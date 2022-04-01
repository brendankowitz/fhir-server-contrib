﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

public interface IRangedComparable
{
    int CompareTo(ISupportSortSearchValue other, ComparisonRange range);
}
