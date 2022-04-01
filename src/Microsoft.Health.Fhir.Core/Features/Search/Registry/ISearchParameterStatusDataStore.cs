// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Features.Search.Registry;

public interface ISearchParameterStatusDataStore
{
    Task<IReadOnlyCollection<ResourceSearchParameterStatus>> GetSearchParameterStatuses(CancellationToken cancellationToken);

    Task UpsertStatuses(IReadOnlyCollection<ResourceSearchParameterStatus> statuses, CancellationToken cancellationToken);

    void SyncStatuses(IReadOnlyCollection<ResourceSearchParameterStatus> statuses);
}
