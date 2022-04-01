﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

public interface ITypedElementToSearchValueConverter
{
    IReadOnlyList<string> FhirTypes { get; }

    Type SearchValueType { get; }

    IEnumerable<ISearchValue> ConvertTo(ITypedElement value);
}
