﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters
{
    /// <summary>
    /// A converter used to convert from <see cref="Instant"/> to a list of <see cref="DateTimeSearchValue"/>.
    /// </summary>
    public class InstantToDateTimeSearchValueConverter : FhirTypedElementToSearchValueConverter<DateTimeSearchValue>
    {
        public InstantToDateTimeSearchValueConverter()
            : base("instant")
        {
        }

        protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
        {
            string stringValue = value.Value?.ToString();

            if (stringValue == null)
            {
                yield break;
            }

            var val = PrimitiveTypeConverter.ConvertTo<DateTimeOffset>(stringValue);

            yield return new DateTimeSearchValue(val);
        }
    }
}
