﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="Markdown"/> to a list of <see cref="StringSearchValue"/>.
/// </summary>
public class MarkdownToStringSearchValueConverter : FhirTypedElementToSearchValueConverter<StringSearchValue>
{
    public MarkdownToStringSearchValueConverter()
        : base("markdown")
    {
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        if (value?.Value == null) yield break;

        yield return new StringSearchValue(value.Value.ToString());
    }
}
