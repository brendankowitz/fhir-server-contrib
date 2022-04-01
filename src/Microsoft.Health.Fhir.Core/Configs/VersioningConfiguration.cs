﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Configs;

public class VersioningConfiguration
{
    public string Default { get; set; } = ResourceVersionPolicy.Versioned;

    public Dictionary<string, string> ResourceTypeOverrides { get; } = new();
}
