// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes
{
    public interface IExtensionData
    {
        JsonObject ExtensionData { get; }
    }
}
