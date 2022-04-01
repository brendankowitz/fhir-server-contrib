// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;

[SuppressMessage("Design", "CA2227", Justification = "POCO style model")]
public class ResourceJsonNode : IExtensionData, IResourceNode
{
    [JsonPropertyName("meta")] public MetaJsonNode Meta { get; set; } = new();

    [JsonExtensionData] public JsonObject ExtensionData { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("resourceType")] public string ResourceType { get; set; }
}
