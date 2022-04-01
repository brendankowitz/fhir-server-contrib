// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Health.Fhir.Extensions.Models;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models.Converters;

public class ResourceConverter : JsonConverter<ResourceJsonNode>
{
    public override ResourceJsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

        var node = JsonNode.Parse(ref reader);
        if (node == null) throw new JsonException();

        if (node["resourceType"]?.ToString() == KnownResourceTypes.SearchParameter) return node.Deserialize<SearchParameterJsonNode>();

        return node.Deserialize<ResourceJsonNode>();
    }

    public override void Write(Utf8JsonWriter writer, ResourceJsonNode value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
