// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;

public class JsonNodeSourceNode : ISourceNode, IResourceTypeSupplier, IAnnotated
{
    private const string _resourceType = "resourceType";
    private const char _shadowNodePrefix = '_';
    private readonly JsonNode _contentElement;
    private readonly JsonNode _valueElement;
    private IDictionary<string, Lazy<IEnumerable<ISourceNode>>> _cachedNodes;

    private JsonNodeSourceNode(JsonNode valueElement, JsonNode contentElement, string name, int? arrayIndex, string location)
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
        _contentElement is JsonObject jsonObject ? GetResourceTypePropertyFromObject(jsonObject, Name)?.ToString() : null;

    public string Name { get; }

    public string Text
    {
        get
        {
            if (_valueElement is JsonValue jsonValue && jsonValue.TryGetValue(out string stringValue)) return stringValue?.Trim();

            if (_valueElement is JsonObject ||
                _valueElement is JsonArray)
                return null;

            if (_valueElement != null)
            {
                string rawText = _valueElement.ToString();
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

            if (_contentElement is JsonObject contentObject)
            {
                IEnumerable<(string Key, JsonNode Value)> objectEnumerator = contentObject.Select(x => (x.Key, x.Value));
                foreach ((string, Lazy<IEnumerable<ISourceNode>>) item in ProcessObjectProperties(objectEnumerator, Location)) list.Add(item.Item1, item.Item2);
            }

            _cachedNodes = list;
        }

        if (string.IsNullOrWhiteSpace(name)) return _cachedNodes.SelectMany(x => x.Value.Value);

        return _cachedNodes.TryGetValue(name, out Lazy<IEnumerable<ISourceNode>> nodes) ? nodes.Value : Enumerable.Empty<ISourceNode>();
    }

    public static JsonNodeSourceNode FromRoot(JsonNode rootNode, string name = "")
    {
        string resourceType = GetResourceTypePropertyFromObject(rootNode, name);
        return new JsonNodeSourceNode(null, rootNode, resourceType, null, resourceType);
    }

    internal static List<(string, Lazy<IEnumerable<ISourceNode>>)> ProcessObjectProperties(IEnumerable<(string Name, JsonNode Value)> objectEnumerator, string location)
    {
        var list = new List<(string, Lazy<IEnumerable<ISourceNode>>)>();

        foreach (IGrouping<string, (string Name, JsonNode Value)> item in objectEnumerator
                     .GroupBy(x => x.Name.TrimStart(_shadowNodePrefix))
                     .Where(x => !string.Equals(x.Key, _resourceType, StringComparison.OrdinalIgnoreCase)))
            if (item.Count() == 1)
            {
                (string Name, JsonNode Value) innerItem = item.First();
                (string Name, Lazy<IEnumerable<ISourceNode>>) values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonNodeToSourceNodes(innerItem.Name, location, innerItem.Value).ToList()));
                list.Add(values);
            }
            else if (item.Count() == 2)
            {
                // Occurs when there is a shadow node, for example:
                // birthDate: "2000-..."
                // _birthDate: { extension: ... }
                (string Name, JsonNode Value) innerItem = item.SingleOrDefault(x => !x.Name.StartsWith(_shadowNodePrefix));
                (string Name, JsonNode Value) shadowItem = item.SingleOrDefault(x => x.Name.StartsWith(_shadowNodePrefix));
                (string Name, Lazy<IEnumerable<ISourceNode>>) values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonNodeToSourceNodes(innerItem.Name, location, innerItem.Value, shadowItem.Value).ToList()));
                list.Add(values);
            }
            else
            {
                throw new Exception($"Expected 1 or 2 nodes with name '{item.Key}'");
            }

        return list;
    }

    private static IEnumerable<ISourceNode> JsonNodeToSourceNodes(string name, string location, JsonNode item, JsonNode shadowItem = null)
    {
        (IReadOnlyList<JsonNode> List, bool ArrayProperty) itemList = ExpandArray(item);
        (IReadOnlyList<JsonNode> List, bool ArrayProperty)? shadowItemList = shadowItem != null ? ExpandArray(shadowItem) : (Array.Empty<JsonNode>(), false);

        bool isArray = itemList.ArrayProperty | shadowItemList?.ArrayProperty ?? false;
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

            string arrayText = isArray ? $"[{i}]" : null;
            string itemLocation = $"{location}.{name}{arrayText}";

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
        if (prop == null) return (Array.Empty<JsonNode>(), false);

        if (prop is JsonArray propArray) return (propArray.Select(x => x).ToArray(), true);

        return (new[] { prop }, false);
    }

    private static JsonNode ItemAt(IReadOnlyList<JsonNode> list, int i)
    {
        return list?.Count > i ? list[i] : null;
    }

    private static string GetResourceTypePropertyFromObject(JsonNode o, string name)
    {
        string jsonNode = o[_resourceType]?.AsValue().ToString();

        if (!string.IsNullOrEmpty(jsonNode) && name != "instance") return jsonNode;

        return null;
    }
}
