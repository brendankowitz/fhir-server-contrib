﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Health.Fhir.Extensions.Exceptions;
using Microsoft.Health.Fhir.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing;

/// <summary>
/// Thrown when search operation is not supported.
/// </summary>
public class SearchOperationNotSupportedException : FhirException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchOperationNotSupportedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SearchOperationNotSupportedException(string message)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(message), $"{nameof(message)} should not be null or whitespace.");

        Issues.Add(new OperationOutcomeIssue(
            OperationOutcomeConstants.IssueSeverity.Error,
            OperationOutcomeConstants.IssueType.NotSupported,
            message));
    }
}
