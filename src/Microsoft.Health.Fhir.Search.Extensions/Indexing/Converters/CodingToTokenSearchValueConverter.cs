﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="Coding"/> to a list of <see cref="TokenSearchValue"/>.
/// </summary>
public class CodingToTokenSearchValueConverter : FhirTypedElementToSearchValueConverter<TokenSearchValue>
{
    public CodingToTokenSearchValueConverter()
        : base("Coding")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        var searchValue = value.ToTokenSearchValue();

        if (searchValue != null) yield return searchValue;
    }
}
