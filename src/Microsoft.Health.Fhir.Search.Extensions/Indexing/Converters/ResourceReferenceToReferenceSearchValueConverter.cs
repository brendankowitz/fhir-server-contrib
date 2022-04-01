﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;

/// <summary>
/// A converter used to convert from <see cref="ResourceReference"/> to a list of <see cref="ReferenceSearchValue"/>.
/// </summary>
public class ResourceReferenceToReferenceSearchValueConverter : FhirTypedElementToSearchValueConverter<ReferenceSearchValue>
{
    private readonly IReferenceSearchValueParser _referenceSearchValueParser;

    public ResourceReferenceToReferenceSearchValueConverter(IReferenceSearchValueParser referenceSearchValueParser)
        : base("Reference")
    {
        EnsureArg.IsNotNull(referenceSearchValueParser, nameof(referenceSearchValueParser));

        _referenceSearchValueParser = referenceSearchValueParser;
    }

    protected override IEnumerable<ISearchValue> Convert(ITypedElement value)
    {
        string reference = value.Scalar("reference") as string;

        if (reference == null) yield break;

        // Contained resources will not be searchable.
        if (reference.StartsWith("#", StringComparison.Ordinal)
            || reference.StartsWith("urn:", StringComparison.Ordinal))
            yield break;

        yield return _referenceSearchValueParser.Parse(reference);
    }
}
