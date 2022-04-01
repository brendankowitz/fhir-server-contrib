﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Specification;

namespace Microsoft.Health.Fhir.Extensions.Schema;

public interface IFhirSchemaProvider : IStructureDefinitionSummaryProvider
{
    FhirSpecification Version { get; }

    IReadOnlySet<string> ResourceTypeNames { get; }
}
