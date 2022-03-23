// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Extensions.Serialization
{
    public static class JsonSourceNodeFactory
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            AllowTrailingCommas = false,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
        };

        public static ISourceNode Parse(string json, string name = null)
        {
            ResourceJsonNode resource = JsonSerializer.Deserialize<ResourceJsonNode>(json, _jsonSerializerOptions);
            return new ReflectedSourceNode(resource, name);
        }

        public static async ValueTask<ISourceNode> Parse(Stream jsonReader, string name = null)
        {
            ResourceJsonNode resource = await JsonSerializer.DeserializeAsync<ResourceJsonNode>(jsonReader, _jsonSerializerOptions);
            return new ReflectedSourceNode(resource, name);
        }
        
        public static async ValueTask<T> Parse<T>(Stream jsonReader, string name = null)
        where T : ResourceJsonNode
        {
            T resource = await JsonSerializer.DeserializeAsync<T>(jsonReader, _jsonSerializerOptions);
            return resource;
        }
        
        public static ISourceNode CreateSourceNode(this JsonDocument jsonDocument, string name = null)
        {
            return JsonElementSourceNode.FromRoot(jsonDocument.RootElement, name);
        }

        public static ISourceNode Create(ResourceJsonNode resource)
        {
            return new ReflectedSourceNode(resource, null);
        }
    }
}
