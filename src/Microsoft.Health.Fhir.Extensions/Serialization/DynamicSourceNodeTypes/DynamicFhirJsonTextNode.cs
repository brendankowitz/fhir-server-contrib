// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;

/// <summary>
///     An JSON implementation of ISourceNode based on System.Text.Json
/// </summary>
public class DynamicFhirJsonTextNode : ISourceNode, IResourceTypeSupplier, IAnnotated, IDisposable
{
    private readonly JsonMergeSettings _jsonMergeSettings = new()
    {
        PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
        MergeNullValueHandling = MergeNullValueHandling.Merge,
        MergeArrayHandling = MergeArrayHandling.Union
    };

    private IList<(string Name, Lazy<IEnumerable<ISourceNode>> Nodes)> _children;
    private JsonDocument _jsonDocument;
    private JsonElement? _jsonElement;
    private Lazy<ILookup<string, JsonProperty>> _jsonElementProperties;
    private Lazy<string> _location;
    private JObject _modifyObject;
    private Lazy<string> _name;

    private string _nodeName;
    private Lazy<string> _resourceType;

    private DynamicFhirJsonTextNode(JsonDocument jsonDocument, string nodeName)
    {
        Initialize(jsonDocument, nodeName);
    }

    private DynamicFhirJsonTextNode(string name, JsonElement? value, JsonElement? content, bool usesShadow,
        int? arrayIndex, string location)
    {
        _name = new Lazy<string>(() => name);
        _location = new Lazy<string>(() => location);
        JsonValue = value;
        _jsonElement = content;
        ArrayIndex = arrayIndex;
        UsesShadow = usesShadow;

        SetupObjectEnumeration();
        GetResourceType(name);
    }

    public bool IsReadOnly => _jsonDocument == null;

    public bool UsesShadow { get; }

    public int? ArrayIndex { get; }

    public JsonElement? JsonValue { get; }

    public IEnumerable<object> Annotations(Type type)
    {
        if (type == typeof(DynamicFhirJsonTextNode) || type == typeof(FhirJsonNode) || type == typeof(ISourceNode) ||
            type == typeof(IResourceTypeSupplier)) return new[] {this};

        return Enumerable.Empty<object>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public string ResourceType => _resourceType.Value;

    public string Name => _name.Value;

    public string Location => _location.Value;

    public string Text
    {
        get
        {
            if (JsonValue?.ValueKind == JsonValueKind.String)
                if (JsonValue?.GetString() != null)
                    return JsonValue?.GetString().Trim();

            if (JsonValue != null)
            {
                var rawText = JsonValue?.GetRawText();
                if (!string.IsNullOrWhiteSpace(rawText))
                    return PrimitiveTypeConverter.ConvertTo<string>(rawText.Trim());
            }

            return null;
        }
    }

    public IEnumerable<ISourceNode> Children(string name = null)
    {
        if (_children == null)
        {
            var childNodes = new List<(string Name, Lazy<IEnumerable<ISourceNode>> Nodes)>();

            if (!(_jsonElement == null ||
                  _jsonElement?.ValueKind == JsonValueKind.Null
                  || _jsonElement?.ValueKind == JsonValueKind.Undefined
                  || _jsonElement?.EnumerateObject().Any() == false))
            {
                // ToList() added explicitly here, we really need our own copy of the list of children
                // Note: this will create a lookup with a grouping that groups the main + shadow property
                // under the same name (which is the name without the _).

                var children = _jsonElementProperties.Value;

                var processed = new HashSet<string>();

                IEnumerable<IGrouping<string, JsonProperty>> scanChildren = children;

                var resourceTypeChild = GetResourceTypePropertyFromObject(_jsonElement.Value, Name).GetValueOrDefault();

                foreach (var child in scanChildren)
                {
                    if (child.First().Value.Equals(resourceTypeChild)) continue;

                    if (processed.Contains(child.Key)) continue;

                    (var main, var shadow) = GivenGetNextElementPair(child);

                    processed.Add(child.Key);

                    var innerChild = child;
                    var nodes = new Lazy<IEnumerable<ISourceNode>>(() =>
                        GivenEnumerateElement(innerChild.Key, main, shadow).ToArray());
                    childNodes.Add((child.Key, nodes));
                }
            }

            _children = childNodes;
        }

        return _children
            .Where(n => n.Name.MatchesPrefix(name))
            .SelectMany(x => x.Nodes.Value);
    }

    private static JsonElement? GetResourceTypePropertyFromObject(JsonElement o, string myName)
    {
        return !o.TryGetProperty(JsonSerializationDetails.RESOURCETYPE_MEMBER_NAME, out var type) ? null
            : type.ValueKind == JsonValueKind.String && myName != "instance" ? type : null;
    }

    public static ISourceNode Parse(string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        return Create(jsonDocument);
    }

    public static ISourceNode Create(JsonDocument document, string nodeName = null)
    {
        return new DynamicFhirJsonTextNode(document, nodeName);
    }

    public void Merge(params object[] replacements)
    {
        if (_modifyObject == null)
        {
            var text = _jsonDocument.RootElement.GetRawText();
            _modifyObject = JObject.Parse(text);
        }

        foreach (var replacement in replacements)
            _modifyObject.Merge(JObject.FromObject(replacement), _jsonMergeSettings);

        Initialize(JsonDocument.Parse(_modifyObject.ToString()), _nodeName);
    }

    private (JsonProperty main, JsonProperty shadow) GivenGetNextElementPair(IGrouping<string, JsonProperty> child)
    {
        JsonProperty main = child.First(), shadow = child.Skip(1).FirstOrDefault();
        return main.Name[0] != '_' ? (main, shadow) : (shadow, main);
    }

    private IEnumerable<DynamicFhirJsonTextNode> GivenEnumerateElement(string name, JsonProperty main,
        JsonProperty shadow)
    {
        // Even if main/shadow has errors (i.e. not both are an array, number of items are not the same
        // we should be getting some kind of minimal useable list from the next two statements and
        // continue parsing.
        var mains = MakeList(main, out var wasArrayMain);
        var shadows = MakeList(shadow, out var wasArrayShadow);
        var isArrayElement = wasArrayMain | wasArrayShadow;

        var length = Math.Max(mains.Count, shadows.Count);

        for (var index = 0; index < length; index++)
        {
            var result = BuildNode(name, At(mains, index), At(shadows, index), isArrayElement, index);
            if (result != null) yield return result;
        }

        static JsonElement? At(IList<JsonElement> list, int i)
        {
            return list.Count > i ? list[i] : null;
        }

        IList<JsonElement> MakeList(JsonProperty prop, out bool wasArray)
        {
            wasArray = false;

            if (prop.Value.ValueKind == JsonValueKind.Null) return Array.Empty<JsonElement>();

            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                wasArray = true;
                return prop.Value.EnumerateArray().Select(x => x).ToList();
            }

            return new[] {prop.Value};
        }
    }

    private static void GivenRaiseFormatError(string message)
    {
        ExceptionNotification.Error(Error.Format("Parser: " + message));
    }

    private DynamicFhirJsonTextNode BuildNode(string name, JsonElement? main, JsonElement? shadow, bool isArrayElement,
        int index)
    {
        JsonElement? value = null;
        JsonElement? contents = null;

        if (main?.ValueKind == JsonValueKind.Null && main?.ValueKind == JsonValueKind.Null)
            return null;
        if (main?.ValueKind == JsonValueKind.Null && shadow == null)
            return null;
        if (main == null && shadow?.ValueKind == JsonValueKind.Null) return null;

        if (main != null)
            switch (main.Value.ValueKind)
            {
                case JsonValueKind.False:
                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                    value = ValidateValue(main.Value, name);
                    break;
                case JsonValueKind.Object:
                    contents = main.Value;
                    break;
            }

        if (shadow != null)
            switch (shadow.Value.ValueKind)
            {
                case JsonValueKind.False:
                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                    value = ValidateValue(shadow.Value, $"_{name}");
                    break;
                case JsonValueKind.Object:
                    if (contents != null)
                        GivenRaiseFormatError(
                            $"The '{name}' and '_{name}' properties cannot both contain complex data.");
                    else
                        contents = shadow.Value;

                    break;
                default:
                    GivenRaiseFormatError(
                        $"The value for property '_{name}' must be an object, not a {shadow.Value.ValueKind}");
                    break;
            }

        // This can only be true, if the logic just before left both value and contents == null because of errors
        // In that case, don't return any result from the build - which will make sure the caller skips
        // this property completely
        if (value == null && contents == null) return null;

        var location = $"{Location}.{name}[{index}]";
        return new DynamicFhirJsonTextNode(
            name,
            value,
            contents,
            shadow != null,
            isArrayElement ? index : null,
            location);

        JsonElement? ValidateValue(JsonElement v, string pName)
        {
            if (v.ValueKind == JsonValueKind.String)
            {
                var str = v.GetString();
                if (string.IsNullOrWhiteSpace(str)) return null;
            }

            return v;
        }
    }

    public string ToRawJson()
    {
        return _jsonDocument?.RootElement.GetRawText() ?? _jsonElement?.GetRawText();
    }

    private void SetupObjectEnumeration()
    {
        _jsonElementProperties = new Lazy<ILookup<string, JsonProperty>>(() => _jsonElement.GetValueOrDefault()
            .EnumerateObject()
            .ToLookup(DeriveMainName));

        string DeriveMainName(JsonProperty prop)
        {
            var name = prop.Name;
            return name[0] == '_' ? name.Substring(1) : name;
        }
    }

    private void GetResourceType(string nodeName)
    {
        _resourceType = new Lazy<string>(() =>
            _jsonElement == null ? null : GetResourceTypePropertyFromObject(_jsonElement.Value, nodeName)?.GetString());
    }

    private void Initialize(JsonDocument jsonDocument, string nodeName)
    {
        _jsonDocument = jsonDocument;
        _jsonElement = _jsonDocument.RootElement;
        SetupObjectEnumeration();
        GetResourceType(nodeName);
        _nodeName = nodeName;
        _name = new Lazy<string>(() => (nodeName ?? (string.IsNullOrEmpty(ResourceType) ? null : ResourceType))
                                       ?? throw Error.InvalidOperation(
                                           "Root object has no type indication (resourceType) and therefore cannot be used to construct an FhirJsonNode. " +
                                           $"Alternatively, specify a {nameof(nodeName)} using the parameter."));

        _location = _name;
        _children = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) _jsonDocument?.Dispose();
    }
}