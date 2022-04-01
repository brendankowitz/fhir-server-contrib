// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;

internal class JsonElementSourceNode : ISourceNode, IResourceTypeSupplier, IAnnotated
{
    private const string _resourceType = "resourceType";
    private const char _shadowNodePrefix = '_';
    private readonly JsonElement? _contentElement;
    private readonly JsonElement? _valueElement;
    private IDictionary<string, Lazy<IEnumerable<ISourceNode>>> _cachedNodes;

    private JsonElementSourceNode(JsonElement? valueElement, JsonElement? contentElement, string name, int? arrayIndex, string location)
    {
        _valueElement = valueElement;
        _contentElement = contentElement;
        Name = name;
        Location = location;
    }

    public IEnumerable<object> Annotations(Type type)
    {
        if (type == GetType() || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier)) return new[] { this };

        return Enumerable.Empty<object>();
    }

    public string ResourceType =>
        // Root or "contained" resources can have their own ResourceType
        _contentElement?.ValueKind == JsonValueKind.Object ? GetResourceTypePropertyFromObject(_contentElement.Value, Name)?.GetString() : null;

    public string Name { get; }

    public string Text
    {
        get
        {
            if (_valueElement?.ValueKind == JsonValueKind.String)
            {
                string stringValue = _valueElement?.GetString();
                if (stringValue != null) return stringValue.Trim();
            }

            if (_valueElement?.ValueKind == JsonValueKind.Object || _valueElement?.ValueKind == JsonValueKind.Array || _valueElement?.ValueKind == JsonValueKind.Undefined) return null;

            if (_valueElement != null)
            {
                string rawText = _valueElement.Value.GetRawText();
                if (!string.IsNullOrWhiteSpace(rawText)) return PrimitiveTypeConverter.ConvertTo<string>(rawText.Trim());
            }

            return null;
        }
    }

    public string Location { get; }

    public IEnumerable<ISourceNode> Children(string name = null)
    {
        if (_cachedNodes == null)
        {
            var list = new Dictionary<string, Lazy<IEnumerable<ISourceNode>>>();

            if (!(_contentElement == null ||
                  _contentElement.Value.ValueKind != JsonValueKind.Object
                  || _contentElement.Value.EnumerateObject().Any() == false))
            {
                IEnumerable<(string Name, JsonElement Value)> objectEnumerator = _contentElement.GetValueOrDefault().EnumerateObject().Select(x => (x.Name, x.Value));
                foreach ((string, Lazy<IEnumerable<ISourceNode>>) item in ProcessObjectProperties(objectEnumerator, Location)) list.Add(item.Item1, item.Item2);
            }

            _cachedNodes = list;
        }

        if (string.IsNullOrWhiteSpace(name)) return _cachedNodes.SelectMany(x => x.Value.Value);

        return _cachedNodes.TryGetValue(name, out Lazy<IEnumerable<ISourceNode>> nodes) ? nodes.Value : Enumerable.Empty<ISourceNode>();
    }

    internal static JsonElementSourceNode FromRoot(JsonElement rootNode, string name = "")
    {
        string resourceType = GetResourceTypePropertyFromObject(rootNode, name)?.GetString();
        return new JsonElementSourceNode(null, rootNode, resourceType, null, resourceType);
    }

    internal static List<(string, Lazy<IEnumerable<ISourceNode>>)> ProcessObjectProperties(IEnumerable<(string Name, JsonElement Value)> objectEnumerator, string location)
    {
        var list = new List<(string, Lazy<IEnumerable<ISourceNode>>)>();

        foreach (IGrouping<string, (string Name, JsonElement Value)> item in objectEnumerator
                     .GroupBy(x => x.Name.TrimStart(_shadowNodePrefix))
                     .Where(x => !string.Equals(x.Key, _resourceType, StringComparison.OrdinalIgnoreCase)))
            if (item.Count() == 1)
            {
                (string Name, JsonElement Value) innerItem = item.First();
                (string Name, Lazy<IEnumerable<ISourceNode>>) values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonElementToSourceNodes(innerItem.Name, location, innerItem.Value).ToList()));
                list.Add(values);
            }
            else if (item.Count() == 2)
            {
                // Occurs when there is a shadow node, for example:
                // birthDate: "2000-..."
                // _birthDate: { extension: ... }
                (string Name, JsonElement Value) innerItem = item.SingleOrDefault(x => !x.Name.StartsWith(_shadowNodePrefix));
                (string Name, JsonElement Value) shadowItem = item.SingleOrDefault(x => x.Name.StartsWith(_shadowNodePrefix));
                (string Name, Lazy<IEnumerable<ISourceNode>>) values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonElementToSourceNodes(innerItem.Name, location, innerItem.Value, shadowItem.Value).ToList()));
                list.Add(values);
            }
            else
            {
                throw new Exception($"Expected 1 or 2 nodes with name '{item.Key}'");
            }

        return list;
    }

    private static IEnumerable<ISourceNode> JsonElementToSourceNodes(string name, string location, JsonElement item, JsonElement? shadowItem = null)
    {
        (IReadOnlyList<JsonElement> List, bool ArrayProperty) itemList = ExpandArray(item);
        (IReadOnlyList<JsonElement> List, bool ArrayProperty)? shadowItemList = shadowItem != null ? ExpandArray(shadowItem.Value) : (Array.Empty<JsonElement>(), false);

        bool isArray = shadowItemList.Value.ArrayProperty;
        for (int i = 0; i < Math.Max(itemList.List.Count, shadowItemList.Value.List.Count); i++)
        {
            JsonElement? first = ItemAt(itemList.List, i);
            JsonElement? shadow = ItemAt(shadowItemList.Value.List, i);

            JsonElement? content = null;
            JsonElement? value = null;

            if (first?.ValueKind == JsonValueKind.Object)
            {
                content = first;
                value = shadow;
            }
            else
            {
                content = shadow;
                value = first;
            }

            string arrayText = isArray ? $"[{i}]" : null;
            string itemLocation = $"{location}.{name}{arrayText}";

            yield return new JsonElementSourceNode(
                value,
                content,
                name,
                itemList.ArrayProperty ? i : (int?)null,
                itemLocation);
        }

        (IReadOnlyList<JsonElement> List, bool ArrayProperty) ExpandArray(JsonElement prop)
        {
            if (prop.ValueKind == JsonValueKind.Null) return (Array.Empty<JsonElement>(), false);

            if (prop.ValueKind == JsonValueKind.Array) return (prop.EnumerateArray().Select(x => x).ToArray(), true);

            return (new[] { prop }, false);
        }

        JsonElement? ItemAt(IReadOnlyList<JsonElement> list, int i)
        {
            return list?.Count > i ? (JsonElement?)list[i] : null;
        }
    }

    private static JsonElement? GetResourceTypePropertyFromObject(JsonElement o, string name)
    {
        return !o.TryGetProperty(_resourceType, out JsonElement type) ? null
            : type.ValueKind == JsonValueKind.String && name != "instance" ? (JsonElement?)type : null;
    }
}
