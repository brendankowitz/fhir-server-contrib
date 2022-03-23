// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes
{
    public class LazyFhirJsonNode : ISourceNode, IResourceTypeSupplier, IAnnotated, IExceptionSource
    {
        private readonly JsonMergeSettings _jsonMergeSettings = new JsonMergeSettings
        {
            PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
            MergeArrayHandling = MergeArrayHandling.Union,
        };

        private readonly FhirJsonParsingSettings _settings;
        private readonly JValue _jsonValue;
        private readonly JObject _jsonObject;
        private readonly int? _arrayIndex;
        private readonly bool _usesShadow;

        private Lazy<string> _text;
        private readonly Lazy<JProperty> _resourceTypeProperty;
        private IList<(string Name, Lazy<IEnumerable<ISourceNode>> Nodes)> _children;

        internal LazyFhirJsonNode(JObject root, string nodeName, FhirJsonParsingSettings settings = null)
        {
            _jsonObject = root ?? throw Error.ArgumentNull(nameof(root));
            _resourceTypeProperty = new Lazy<JProperty>(() => GetResourceTypePropertyFromObject(_jsonObject, nodeName));

            Name = (nodeName ?? (string.IsNullOrEmpty(ResourceType) ? null : ResourceType))
                      ?? throw Error.InvalidOperation("Root object has no type indication (resourceType) and therefore cannot be used to construct an FhirJsonNode. " +
                        $"Alternatively, specify a {nameof(nodeName)} using the parameter.");

            Location = Name;

            _jsonValue = null;
            _arrayIndex = null;
            _usesShadow = false;
            _settings = settings?.Clone() ?? new FhirJsonParsingSettings();
            SetupText();
        }

        private LazyFhirJsonNode(LazyFhirJsonNode parent, string name, JValue value, JObject content, bool usesShadow, int? arrayIndex, string location)
        {
            Name = name;
            _jsonValue = value;
            _jsonObject = content;
            _arrayIndex = arrayIndex;
            Location = location;
            _usesShadow = usesShadow;
            _settings = parent._settings;
            ExceptionHandler = parent.ExceptionHandler;
            _resourceTypeProperty = new Lazy<JProperty>(() => GetResourceTypePropertyFromObject(_jsonObject, name));
            SetupText();
        }

        public string Name { get; }

        public string Location { get; }

        public ExceptionNotificationHandler ExceptionHandler { get; set; }

        public JToken PositionNode => _jsonValue ?? (JToken)_jsonObject;

        public string ResourceType => (_resourceTypeProperty.Value?.Value as JValue)?.Value as string;

        public string Text => _text.Value;

        public static ISourceNode Parse(string json)
        {
            var jsonDocument = JObject.Parse(json);
            return Create(jsonDocument);
        }

        public static ISourceNode Create(JObject document, string nodeName = null)
        {
            return new LazyFhirJsonNode(document, nodeName);
        }

        public void Merge(params object[] replacements)
        {
            foreach (object replacement in replacements)
            {
                _jsonObject.Merge(JObject.FromObject(replacement), _jsonMergeSettings);
            }

            _children = null;
            SetupText();
        }

        public string ToRawJson()
        {
            return _jsonObject.ToString();
        }

        public IEnumerable<ISourceNode> Children(string name = null)
        {
            if (_jsonObject == null || _jsonObject.HasValues == false)
            {
                return Enumerable.Empty<ISourceNode>();
            }

            if (_children == null)
            {
                var childNodes = new List<(string Name, Lazy<IEnumerable<ISourceNode>> Nodes)>();

                // ToList() added explicitly here, we really need our own copy of the list of children
                // Note: this will create a lookup with a grouping that groups the main + shadow property
                // under the same name (which is the name without the _).
                var children = _jsonObject.Children<JProperty>().ToLookup(jp => DeriveMainName(jp));

                var processed = new HashSet<string>();

                var resourceTypeChild = _resourceTypeProperty.Value;

                foreach (var child in children)
                {
                    if (child.First() == resourceTypeChild)
                    {
                        continue;
                    }

                    if (processed.Contains(child.Key))
                    {
                        continue;
                    }

                    (JProperty main, JProperty shadow) = GetNextElementPair(child);

                    if (child.Key == "fhir_comments")
                    {
                        continue;      // ignore pre-DSTU2 Json comments
                    }

                    processed.Add(child.Key);

                    var innerChild = child;
                    var nodes = new Lazy<IEnumerable<ISourceNode>>(() => EnumerateElement(child.Key, main, shadow));
                    childNodes.Add((child.Key, nodes));
                }

                _children = childNodes;
            }

            return _children
                .Where(n => n.Name.MatchesPrefix(name))
                .SelectMany(x => x.Nodes.Value);

            static string DeriveMainName(JProperty prop)
            {
                var n = prop.Name;
                return n[0] == '_' ? n.Substring(1) : n;
            }
        }

        private (JProperty main, JProperty shadow) GetNextElementPair(IGrouping<string, JProperty> child)
        {
            JProperty main = child.First(), shadow = child.Skip(1).FirstOrDefault();

            return main.Name[0] != '_' ? (main, shadow) : (shadow, main);
        }

        private IEnumerable<LazyFhirJsonNode> EnumerateElement(string name, JProperty main, JProperty shadow)
        {
            // Even if main/shadow has errors (i.e. not both are an array, number of items are not the same
            // we should be getting some kind of minimal useable list from the next two statements and
            // continue parsing.
            var mains = MakeList(main, out var wasArrayMain);
            var shadows = MakeList(shadow, out var wasArrayShadow);
            bool isArrayElement = wasArrayMain | wasArrayShadow;

            int length = Math.Max(mains.Count, shadows.Count);

            for (var index = 0; index < length; index++)
            {
                var result = Build(name, At(mains, index), At(shadows, index), isArrayElement, index);
                if (result != null)
                {
                    yield return result;
                }
            }

            JToken At(IList<JToken> list, int i) => list.Count > i ? list[i] : null;

            IList<JToken> MakeList(JProperty prop, out bool wasArray)
            {
                wasArray = false;

                if (prop == null)
                {
                    return Array.Empty<JToken>();
                }
                else if (prop.Value is JArray array)
                {
                    wasArray = true;
                    return array;
                }
                else
                {
                    return new[] { prop.Value };
                }
            }
        }

        private LazyFhirJsonNode Build(string name, JToken main, JToken shadow, bool isArrayElement, int index)
        {
            JValue value = null;
            JObject contents = null;

            if (main?.Type == JTokenType.Null && shadow?.Type == JTokenType.Null)
            {
                return null;
            }
            else if (main?.Type == JTokenType.Null && shadow == null)
            {
                return null;
            }
            else if (main == null && shadow?.Type == JTokenType.Null)
            {
                return null;
            }

            if (main != null)
            {
                switch (main)
                {
                    case JValue val:
                        value = ValidateValue(val, name);
                        break;
                    case JObject obj:
                        contents = ValidateObject(obj, name);
                        break;
                    default:
                        break;
                }
            }

            if (shadow != null)
            {
                switch (shadow)
                {
                    case JValue val when val.Type == JTokenType.Null:
                        ValidateValue(val, $"_{name}");   // just report error, has no real value to return
                        break;
                    case JObject obj:
                        if (contents != null)
                        {
                            RaiseFormatError($"The '{name}' and '_{name}' properties cannot both contain complex data.", shadow);
                        }
                        else
                        {
                            contents = ValidateObject(obj, $"_{name}");
                        }

                        break;
                    default:
                        RaiseFormatError($"The value for property '_{name}' must be an object, not a {shadow.Type}", shadow);
                        break;
                }
            }

            // This can only be true, if the logic just before left both value and contents == null because of errors
            // In that case, don't return any result from the build - which will make sure the caller skips
            // this property completely
            if (value == null && contents == null)
            {
                return null;
            }

            var location = $"{Location}.{name}[{index}]";

            return new LazyFhirJsonNode(this, name, value, contents, shadow != null, isArrayElement ? index : (int?)null, location);

            JValue ValidateValue(JValue v, string pName)
            {
                if (v.Value is string s && string.IsNullOrWhiteSpace(s))
                {
                    return null;
                }

                if (v.Type == JTokenType.Null)
                {
                    return null;
                }
                else
                {
                    return v;
                }
            }

            JObject ValidateObject(JObject o, string pName)
            {
                if (o.Count == 0)
                {
                    return null;
                }
                else
                {
                    return o;
                }
            }
        }

        private void RaiseFormatError(string message, JToken node)
        {
            var (lineNumber, linePosition) = GetPosition(node);
            ExceptionHandler.NotifyOrThrow(this, ExceptionNotification.Error(Error.Format("Parser: " + message, lineNumber, linePosition)));
        }

        private static (int lineNumber, int linePosition) GetPosition(JToken node) => node is IJsonLineInfo jli ? (jli.LineNumber, jli.LinePosition) : (-1, -1);

        public IEnumerable<object> Annotations(Type type)
        {
            if (type == typeof(FhirJsonNode) || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier))
            {
                return new[] { this };
            }
            else
            {
                return Enumerable.Empty<object>();
            }
        }

        private static JProperty GetResourceTypePropertyFromObject(JObject o, string name = null)
        {
            JProperty jProperty = o?.Property("resourceType", StringComparison.OrdinalIgnoreCase);

            if (jProperty != null)
            {
                if (jProperty.Value.Type != JTokenType.String || !(name != "instance"))
                {
                    return null;
                }

                return jProperty;
            }

            return null;
        }

        private void SetupText()
        {
            _text = new Lazy<string>(() =>
            {
                if (_jsonValue != null)
                {
                    if (_jsonValue.Value != null)
                    {
                        // Make sure the representation of this Json-typed value is turned
                        // into a string representation compatible with the XML serialization
                        return _jsonValue.Value is string str ? str.Trim() : PrimitiveTypeConverter.ConvertTo<string>(_jsonValue.Value);
                    }
                }

                return null;
            });
        }
    }
}
