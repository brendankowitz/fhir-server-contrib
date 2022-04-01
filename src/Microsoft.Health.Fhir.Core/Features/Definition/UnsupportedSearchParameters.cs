﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Features.Definition;

internal class UnsupportedSearchParameters
{
    public HashSet<Uri> Unsupported { get; set; } = new();

    public HashSet<Uri> PartialSupport { get; set; } = new();
}
