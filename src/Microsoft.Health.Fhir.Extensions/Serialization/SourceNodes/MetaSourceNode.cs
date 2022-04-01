// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes;

// ReSharper disable once UnusedType.Global
internal class MetaSourceNode : BaseSourceNode<MetaJsonNode>
{
    private const string _versionIdProperty = "versionId";
    private const string _lastUpdatedProperty = "lastUpdated";
    private readonly string _location;

    public MetaSourceNode(MetaJsonNode resource, string name, string location)
        : base(resource)
    {
        Name = name;
        _location = location;
    }

    public override string Name { get; }

    public override string Text => null;

    public override string Location => _location;

    public override string ResourceType => null;

    protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
    {
        yield return (_versionIdProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.VersionId, _versionIdProperty, $"{_location}.{_versionIdProperty}") }));
        yield return (_lastUpdatedProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.LastUpdated, _lastUpdatedProperty, $"{_location}.{_lastUpdatedProperty}") }));
    }
}
