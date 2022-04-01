// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;

[SuppressMessage("Design", "CA2227", Justification = "POCO style model")]
public class MetaJsonNode : IExtensionData
{
    [JsonPropertyName("versionId")] public string VersionId { get; set; }

    [JsonPropertyName("lastUpdated")] public string LastUpdated { get; set; }

    [JsonExtensionData] public JsonObject ExtensionData { get; set; }
}
