// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using EnsureThat;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Conformance;

public abstract class ConformanceProviderBase : IConformanceProvider
{
    private readonly ConcurrentDictionary<string, bool> _evaluatedQueries = new(StringComparer.OrdinalIgnoreCase);

    public abstract Task<ResourceElement> GetCapabilityStatementOnStartup(CancellationToken cancellationToken = default);

    public async Task<bool> SatisfiesAsync(IEnumerable<CapabilityQuery> queries, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(queries, nameof(queries));

        ResourceElement capabilityStatement = await GetCapabilityStatementOnStartup(cancellationToken);

        return queries.All(x => _evaluatedQueries.GetOrAdd(x.FhirPathPredicate, _ => capabilityStatement.Instance.Predicate(x.FhirPathPredicate)));
    }

    public abstract Task<ResourceElement> GetMetadata(CancellationToken cancellationToken = default);

    internal void ClearCache()
    {
        _evaluatedQueries.Clear();
    }
}
