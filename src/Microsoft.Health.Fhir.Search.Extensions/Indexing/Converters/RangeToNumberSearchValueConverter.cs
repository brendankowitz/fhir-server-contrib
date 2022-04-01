﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="Range"/> to a list of <see cref="NumberSearchValue"/>.
/// </summary>
public class RangeToNumberSearchValueConverter : FhirTypedElementToSearchValueConverter<NumberSearchValue>
{
    public RangeToNumberSearchValueConverter()
        : base("Range")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        decimal? lowValue = (decimal?)value.Scalar("low");
        decimal? highValue = (decimal?)value.Scalar("high");

        if (lowValue.HasValue || highValue.HasValue) yield return new NumberSearchValue(lowValue, highValue);
    }
}
