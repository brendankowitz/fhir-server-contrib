﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Extensions;
using Microsoft.Health.Fhir.Extensions.ValueSets;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="Money"/> to a list of <see cref="QuantitySearchValue"/>.
/// </summary>
public class MoneyToQuantitySearchValueConverter : FhirTypedElementToSearchValueConverter<QuantitySearchValue>
{
    private readonly FhirSpecification _fhirSpecification;

    public MoneyToQuantitySearchValueConverter(FhirSpecification fhirSpecification)
        : base("Money")
    {
        _fhirSpecification = fhirSpecification;
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        decimal? decimalValue = (decimal?)value.Scalar("value");

        if (!decimalValue.HasValue) yield break;

        if (_fhirSpecification == FhirSpecification.Stu3)
        {
            string code = value.Scalar("code")?.ToString();
            string system = value.Scalar("system")?.ToString();

            // The spec specifies that only the code value must be provided.
            if (code == null) yield break;

            yield return new QuantitySearchValue(
                system,
                code,
                decimalValue.GetValueOrDefault());
        }
        else
        {
            string currency = value.Scalar("currency")?.ToString();

            if (currency == null) yield break;

            yield return new QuantitySearchValue(
                CurrencyValueSet.CodeSystemUri, // TODO: Use ICodeSystemResolver to pull this from resourcepath-codesystem-mappings.json once it's added.
                currency,
                decimalValue.GetValueOrDefault());
        }
    }
}
