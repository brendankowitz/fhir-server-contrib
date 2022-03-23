// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Extensions.Serialization
{
    public interface IJsonSerializable
    {
        string SerializeToJson(bool writeIndented = false);

        Task SerializeToJson(Stream stream, bool writeIndented = false);
    }
}
