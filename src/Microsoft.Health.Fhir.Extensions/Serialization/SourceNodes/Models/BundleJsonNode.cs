// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models.Converters;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models
{
    [SuppressMessage("Design", "CA2227", Justification = "POCO style model")]
    [SuppressMessage("Design", "CA1819", Justification = "POCO style model")]
    public class BundleJsonNode : ResourceJsonNode
    {
        public BundleJsonNode()
        {
            ResourceType = "Bundle";
        }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("link")]
        public IList<BundleLinkJsonNode> Link { get; set; }

        [JsonPropertyName("entry")]
        public IList<BundleComponentJsonNode> Entry { get; set; }
    }
}
