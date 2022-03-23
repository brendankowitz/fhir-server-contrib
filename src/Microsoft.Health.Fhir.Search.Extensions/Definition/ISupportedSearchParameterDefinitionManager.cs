// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Definition
{
    /// <summary>
    /// Provides mechanism to access search parameter definition.
    /// </summary>
    public interface ISupportedSearchParameterDefinitionManager : ISearchParameterDefinitionManager
    {
        /// <summary>
        /// Gets list of search parameters that are supported but not yet searchable or sortable.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the matching search parameters.</returns>
        IEnumerable<SearchParameterInfo> GetSearchParametersRequiringReindexing();
    }
}
