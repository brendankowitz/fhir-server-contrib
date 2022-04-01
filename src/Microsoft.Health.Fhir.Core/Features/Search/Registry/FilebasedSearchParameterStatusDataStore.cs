// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Core;
using Microsoft.Health.Fhir.Core.Data;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Search.Registry;

public class FilebasedSearchParameterStatusDataStore : ISearchParameterStatusDataStore
{
    public delegate ISearchParameterStatusDataStore Resolver();

    private readonly IModelInfoProvider _modelInfoProvider;
    private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
    private ResourceSearchParameterStatus[] _statusResults;

    public FilebasedSearchParameterStatusDataStore(
        ISearchParameterDefinitionManager searchParameterDefinitionManager,
        IModelInfoProvider modelInfoProvider)
    {
        EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));

        _searchParameterDefinitionManager = searchParameterDefinitionManager;
        _modelInfoProvider = modelInfoProvider;
    }

    public Task<IReadOnlyCollection<ResourceSearchParameterStatus>> GetSearchParameterStatuses(CancellationToken cancellationToken)
    {
        if (_statusResults == null)
        {
            using Stream stream = _modelInfoProvider.OpenVersionedFileStream("unsupported-search-parameters.json");
            using TextReader reader = new StreamReader(stream);
            UnsupportedSearchParameters unsupportedParams = JsonConvert.DeserializeObject<UnsupportedSearchParameters>(reader.ReadToEnd());

            // Loads unsupported parameters
            var support = unsupportedParams.Unsupported
                .Select(x => new ResourceSearchParameterStatus
                {
                    Uri = x,
                    Status = SearchParameterStatus.Disabled,
                    LastUpdated = Clock.UtcNow
                })
                .Concat(unsupportedParams.PartialSupport
                    .Select(x => new ResourceSearchParameterStatus
                    {
                        Uri = x,
                        Status = SearchParameterStatus.Enabled,
                        IsPartiallySupported = true,
                        LastUpdated = Clock.UtcNow
                    }))
                .ToDictionary(x => x.Uri);

            // Merge with supported list
            _statusResults = _searchParameterDefinitionManager.AllSearchParameters
                .Where(x => !support.ContainsKey(x.Url))
                .Select(x => new ResourceSearchParameterStatus
                {
                    Uri = x.Url,
                    Status = SearchParameterStatus.Enabled,
                    LastUpdated = Clock.UtcNow
                })
                .Concat(support.Values)
                .ToArray();
        }

        return Task.FromResult<IReadOnlyCollection<ResourceSearchParameterStatus>>(_statusResults);
    }

    public Task UpsertStatuses(IReadOnlyCollection<ResourceSearchParameterStatus> statuses, CancellationToken cancellationToken)
    {
        // File based registry does not persist runtime updates
        return Task.CompletedTask;
    }

    public void SyncStatuses(IReadOnlyCollection<ResourceSearchParameterStatus> statuses)
    {
        // Do nothing. This is only required for SQL.
    }
}
