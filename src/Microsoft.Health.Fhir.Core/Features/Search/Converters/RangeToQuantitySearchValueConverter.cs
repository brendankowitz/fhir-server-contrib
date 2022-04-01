﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;

namespace Microsoft.Health.Fhir.Core.Features.Search.Converters;

/// <summary>
/// A converter used to convert from <see cref="Range"/> to a list of <see cref="QuantitySearchValue"/>.
/// </summary>
public class RangeToQuantitySearchValueConverter : FhirTypedElementToSearchValueConverter<QuantitySearchValue>
{
    public RangeToQuantitySearchValueConverter()
        : base("Range")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        decimal? highValue = (decimal?)value.Scalar("high.value");
        decimal? lowValue = (decimal?)value.Scalar("low.value");

        string quantityRepresentativeValue = lowValue != null ? "low" : highValue != null ? "high" : null;

        if (quantityRepresentativeValue != null)
        {
            string system = value.Scalar($"{quantityRepresentativeValue}.system") as string;
            string code = value.Scalar($"{quantityRepresentativeValue}.code") as string;

            // FROM https://www.hl7.org/fhir/datatypes.html#Range: "The unit and code/system elements of the low or high elements SHALL match."
            yield return new QuantitySearchValue(system, code, lowValue, highValue);
        }
    }
}
