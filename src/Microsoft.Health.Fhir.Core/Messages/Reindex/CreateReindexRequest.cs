﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.Reindex;

public class CreateReindexRequest : IRequest<CreateReindexResponse>
{
    public CreateReindexRequest(
        IReadOnlyCollection<string> targetResourceTypes,
        ushort? maximumConcurrency = null,
        uint? maximumResourcesPerQuery = null,
        int? queryDelayIntervalInMilliseconds = null,
        ushort? targetDataStoreUsagePercentage = null)
    {
        EnsureArg.IsNotNull(targetResourceTypes, nameof(targetResourceTypes));

        MaximumConcurrency = maximumConcurrency;
        MaximumResourcesPerQuery = maximumResourcesPerQuery;
        QueryDelayIntervalInMilliseconds = queryDelayIntervalInMilliseconds;
        TargetDataStoreUsagePercentage = targetDataStoreUsagePercentage;
        TargetResourceTypes = targetResourceTypes;
    }

    public IReadOnlyCollection<string> TargetResourceTypes { get; }

    public ushort? MaximumConcurrency { get; }

    public uint? MaximumResourcesPerQuery { get; }

    public int? QueryDelayIntervalInMilliseconds { get; }

    public ushort? TargetDataStoreUsagePercentage { get; }
}
