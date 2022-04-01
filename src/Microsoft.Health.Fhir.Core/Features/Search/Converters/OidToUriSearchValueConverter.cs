﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;

namespace Microsoft.Health.Fhir.Core.Features.Search.Converters;

/// <summary>
/// A converter used to convert from <see cref="Oid"/> to a list of <see cref="UriSearchValue"/>.
/// </summary>
public class OidToUriSearchValueConverter : FhirTypedElementToSearchValueConverter<UriSearchValue>
{
    public OidToUriSearchValueConverter()
        : base("oid")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        if (value.Value == null) yield break;

        yield return new UriSearchValue(value.Value.ToString(), false);
    }
}
