﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="Date"/> to a list of <see cref="DateTimeSearchValue"/>.
/// </summary>
public class DateToDateTimeSearchValueConverter : FhirTypedElementToSearchValueConverter<DateTimeSearchValue>
{
    public DateToDateTimeSearchValueConverter()
        : base("date", "dateTime", "System.DateTime", "System.Date")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        string stringValue = value?.Value?.ToString();

        if (string.IsNullOrWhiteSpace(stringValue)) yield break;

        yield return new DateTimeSearchValue(PartialDateTime.Parse(stringValue));
    }
}
