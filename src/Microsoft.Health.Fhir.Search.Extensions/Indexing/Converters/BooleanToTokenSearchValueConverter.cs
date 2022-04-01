﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Extensions.ValueSets;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="FhirBoolean"/> to a list of <see cref="TokenSearchValue"/>.
/// </summary>
public class BooleanToTokenSearchValueConverter : FhirTypedElementToSearchValueConverter<TokenSearchValue>
{
    public BooleanToTokenSearchValueConverter()
        : base("boolean", "System.Boolean")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        object fhirValue = value?.Value;

        if (fhirValue == null) yield break;

        yield return new TokenSearchValue(SpecialValues.System, (bool)fhirValue ? "true" : "false", null);
    }
}
