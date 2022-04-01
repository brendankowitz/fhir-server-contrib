// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Fhir.Core.Features.Conformance.Models;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Should be consistent with base type.")]
internal class DefaultOptionHashSet<T> : HashSet<T>, IDefaultOption
{
    public DefaultOptionHashSet(T defaultOption)
    {
        DefaultOption = defaultOption;
    }

    public DefaultOptionHashSet(T defaultOption, IEqualityComparer<T> comparer)
        : base(comparer)
    {
        DefaultOption = defaultOption;
    }

    public T DefaultOption { get; set; }

    object IDefaultOption.DefaultOption => DefaultOption;
}
