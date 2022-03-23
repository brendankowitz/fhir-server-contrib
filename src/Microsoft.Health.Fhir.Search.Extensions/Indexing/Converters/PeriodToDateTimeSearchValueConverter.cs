// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters
{
    /// <summary>
    /// A converter used to convert from <see cref="Period"/> to a list of <see cref="DateTimeSearchValue"/>.
    /// </summary>
    public class PeriodToDateTimeSearchValueConverter : FhirTypedElementToSearchValueConverter<DateTimeSearchValue>
    {
        public PeriodToDateTimeSearchValueConverter()
            : base("Period")
        {
        }

        protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
        {
            var startString = value.Scalar("start")?.ToString();
            var endString = value.Scalar("end")?.ToString();

            PartialDateTime start = string.IsNullOrWhiteSpace(startString) ?
                PartialDateTime.MinValue :
                PartialDateTime.Parse(startString);

            PartialDateTime end = string.IsNullOrWhiteSpace(endString) ?
                PartialDateTime.MaxValue :
                PartialDateTime.Parse(endString);

            yield return new DateTimeSearchValue(start, end);
        }
    }
}
