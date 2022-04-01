// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Conformance;

public interface IConformanceProvider
{
    /// <summary>
    /// Gets capability statement built during server start up.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ResourceElement> GetCapabilityStatementOnStartup(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets capability statement based on state of server.
    /// </summary>
    /// <remarks>
    /// SearchParameters and Profile support can be different from statement provided via <see cref="GetCapabilityStatementOnStartup"/>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ResourceElement> GetMetadata(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run <paramref name="queries"/> against capability statement calculated during startup.
    /// </summary>
    /// <param name="queries">Query to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> SatisfiesAsync(IEnumerable<CapabilityQuery> queries, CancellationToken cancellationToken = default);
}
