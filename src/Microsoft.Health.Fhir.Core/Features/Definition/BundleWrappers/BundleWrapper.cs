// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Definition.BundleWrappers;

internal class BundleWrapper
{
    private readonly Lazy<IReadOnlyList<BundleEntryWrapper>> _entries;

    public BundleWrapper(ITypedElement bundle)
    {
        EnsureArg.IsNotNull(bundle, nameof(bundle));
        EnsureArg.Is(KnownResourceTypes.Bundle, bundle.InstanceType, StringComparison.Ordinal, nameof(bundle));

        _entries = new Lazy<IReadOnlyList<BundleEntryWrapper>>(() => bundle.Select("entry").Select(x => new BundleEntryWrapper(x)).ToArray());
    }

    public IReadOnlyList<BundleEntryWrapper> Entries => _entries.Value;
}
