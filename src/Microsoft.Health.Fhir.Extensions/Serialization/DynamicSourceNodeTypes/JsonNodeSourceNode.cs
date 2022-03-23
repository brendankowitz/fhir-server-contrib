// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes
{
    public class JsonNodeSourceNode : ISourceNode, IResourceTypeSupplier, IAnnotated
    {
        private const string _resourceType = "resourceType";
        private const char _shadowNodePrefix = '_';
        private readonly JsonNode _valueElement;
        private readonly JsonNode _contentElement;
        private readonly string _name;
        private readonly int? _arrayIndex;
        private readonly string _location;
        private IDictionary<string, Lazy<IEnumerable<ISourceNode>>> _cachedNodes;

        private JsonNodeSourceNode(JsonNode valueElement, JsonNode contentElement, string name, int? arrayIndex, string location)
        {
            _valueElement = valueElement;
            _contentElement = contentElement;
            _name = name;
            _arrayIndex = arrayIndex;
            _location = location;
        }

        public string Name => _name;

        public string Text
        {
            get
            {
                if (_valueElement is JsonValue jsonValue && jsonValue.TryGetValue(out string stringValue))
                {
                    return stringValue?.Trim();
                }

                if (_valueElement is JsonObject ||
                    _valueElement is JsonArray)
                {
                    return null;
                }

                if (_valueElement != null)
                {
                    string rawText = _valueElement.ToString();
                    if (!string.IsNullOrWhiteSpace(rawText))
                    {
                        return PrimitiveTypeConverter.ConvertTo<string>(rawText.Trim());
                    }
                }

                return null;
            }
        }

        public string Location => _location;

        public string ResourceType
        {
            get
            {
                // Root or "contained" resources can have their own ResourceType
                return _contentElement is JsonObject jsonObject ?
                    GetResourceTypePropertyFromObject(jsonObject, _name)?.ToString() : null;
            }
        }

        public IEnumerable<object> Annotations(Type type)
        {
            if (type == GetType() || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier))
            {
                return new[] { this };
            }

            return Enumerable.Empty<object>();
        }

        public IEnumerable<ISourceNode> Children(string name = null)
        {
            if (_cachedNodes == null)
            {
                var list = new Dictionary<string, Lazy<IEnumerable<ISourceNode>>>();

                if (_contentElement is JsonObject contentObject)
                {
                    var objectEnumerator = contentObject.Select(x => (x.Key, x.Value));
                    foreach (var item in ProcessObjectProperties(objectEnumerator, _location))
                    {
                        list.Add(item.Item1, item.Item2);
                    }
                }

                _cachedNodes = list;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return _cachedNodes.SelectMany(x => x.Value.Value);
            }

            return _cachedNodes.TryGetValue(name, out var nodes) ? nodes.Value : Enumerable.Empty<ISourceNode>();
        }

        public static JsonNodeSourceNode FromRoot(JsonNode rootNode, string name = "")
        {
            var resourceType = GetResourceTypePropertyFromObject(rootNode, name);
            return new JsonNodeSourceNode(null, rootNode, resourceType, null, resourceType);
        }

        internal static List<(string, Lazy<IEnumerable<ISourceNode>>)> ProcessObjectProperties(IEnumerable<(string Name, JsonNode Value)> objectEnumerator, string location)
        {
            var list = new List<(string, Lazy<IEnumerable<ISourceNode>>)>();

            foreach (IGrouping<string, (string Name, JsonNode Value)> item in objectEnumerator
                .GroupBy(x => x.Name.TrimStart(_shadowNodePrefix))
                .Where(x => !string.Equals(x.Key, _resourceType, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.Count() == 1)
                {
                    var innerItem = item.First();
                    var values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonNodeToSourceNodes(innerItem.Name, location, innerItem.Value).ToList()));
                    list.Add(values);
                }
                else if (item.Count() == 2)
                {
                    // Occurs when there is a shadow node, for example:
                    // birthDate: "2000-..."
                    // _birthDate: { extension: ... }
                    var innerItem = item.SingleOrDefault(x => !x.Name.StartsWith(_shadowNodePrefix));
                    var shadowItem = item.SingleOrDefault(x => x.Name.StartsWith(_shadowNodePrefix));
                    var values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonNodeToSourceNodes(innerItem.Name, location, innerItem.Value, shadowItem.Value).ToList()));
                    list.Add(values);
                }
                else
                {
                    throw new Exception($"Expected 1 or 2 nodes with name '{item.Key}'");
                }
            }

            return list;
        }

        private static IEnumerable<ISourceNode> JsonNodeToSourceNodes(string name, string location, JsonNode item, JsonNode shadowItem = null)
        {
            (IReadOnlyList<JsonNode> List, bool ArrayProperty) itemList = ExpandArray(item);
            (IReadOnlyList<JsonNode> List, bool ArrayProperty)? shadowItemList = shadowItem != null ?
                ExpandArray(shadowItem) : (Array.Empty<JsonNode>(), false);

            var isArray = itemList.ArrayProperty | shadowItemList?.ArrayProperty ?? false;
            for (int i = 0; i < Math.Max(itemList.List.Count, shadowItemList?.List.Count ?? 0); i++)
            {
                JsonNode first = ItemAt(itemList.List, i);
                JsonNode shadow = ItemAt(shadowItemList?.List, i);

                JsonNode content = null;
                JsonNode value = null;

                if (first is JsonObject)
                {
                    content = first;
                    value = shadow;
                }
                else
                {
                    content = shadow;
                    value = first;
                }

                var arrayText = isArray ? $"[{i}]" : null;
                var itemLocation = $"{location}.{name}{arrayText}";

                yield return new JsonNodeSourceNode(
                    value,
                    content,
                    name,
                    itemList.ArrayProperty ? i : (int?)null,
                    itemLocation);
            }
        }

        private static (IReadOnlyList<JsonNode> List, bool ArrayProperty) ExpandArray(JsonNode prop)
        {
            if (prop == null)
            {
                return (Array.Empty<JsonNode>(), false);
            }

            if (prop is JsonArray propArray)
            {
                return (propArray.Select(x => x).ToArray(), true);
            }

            return (new[] { prop }, false);
        }

        private static JsonNode ItemAt(IReadOnlyList<JsonNode> list, int i) => list?.Count > i ? list[i] : null;

        private static string GetResourceTypePropertyFromObject(JsonNode o, string name)
        {
            string jsonNode = o[_resourceType]?.AsValue().ToString();

            if (!string.IsNullOrEmpty(jsonNode) && name != "instance")
            {
                return jsonNode;
            }

            return null;
        }
    }
}