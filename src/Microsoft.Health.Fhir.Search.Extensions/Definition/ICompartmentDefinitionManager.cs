﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Extensions.ValueSets;

namespace Microsoft.Health.Fhir.Search.Extensions.Definition
{
    public interface ICompartmentDefinitionManager
    {
        bool TryGetSearchParams(string resourceType, CompartmentType compartmentType, out HashSet<string> searchParams);

        bool TryGetResourceTypes(CompartmentType compartmentType, out HashSet<string> resourceTypes);
    }
}
