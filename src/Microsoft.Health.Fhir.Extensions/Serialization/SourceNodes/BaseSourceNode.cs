// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes;

public abstract class BaseSourceNode<T> : ISourceNode, IResourceTypeSupplier, IAnnotated
    where T : IExtensionData
{
    private IList<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> _cachedNodes;

    protected BaseSourceNode(T resource)
    {
        Resource = resource;
    }

    public T Resource { get; }

    public IEnumerable<object> Annotations(Type type)
    {
        if (type == GetType() || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier)) return new[] { this };

        return Enumerable.Empty<object>();
    }

    public abstract string ResourceType { get; }

    public abstract string Name { get; }

    public abstract string Text { get; }

    public abstract string Location { get; }

    public IEnumerable<ISourceNode> Children(string name = null)
    {
        if (_cachedNodes == null) _cachedNodes = PropertySourceNodes().Concat(ExtensionSourceNodes()).ToList();

        if (string.IsNullOrWhiteSpace(name)) return _cachedNodes.SelectMany(x => x.Node.Value);

        return _cachedNodes
            .Where(x => string.Equals(name, x.Name, StringComparison.Ordinal))
            .SelectMany(x => x.Node.Value);
    }

    private IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> ExtensionSourceNodes()
    {
        JsonObject properties = Resource.ExtensionData ?? new JsonObject();
        return JsonNodeSourceNode.ProcessObjectProperties(properties.Select(x => (x.Key, x.Value)), Location);
    }

    protected abstract IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes();
}
