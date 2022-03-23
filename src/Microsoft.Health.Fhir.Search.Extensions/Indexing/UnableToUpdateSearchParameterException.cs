﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Extensions.Exceptions;
using Microsoft.Health.Fhir.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing
{
    /// <summary>
    /// The exception that is thrown when if unable to update search parameter information from the data store
    /// </summary>
    public class UnableToUpdateSearchParameterException : FhirException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToUpdateSearchParameterException"/> class.
        /// </summary>
        /// <param name="definitionUri">The search parameter definition URL.</param>
        public UnableToUpdateSearchParameterException(Uri definitionUri)
        {
            EnsureArg.IsNotNull(definitionUri, nameof(definitionUri));

            AddIssue(string.Format(Resources.UnableToUpdateSearchParameter, definitionUri.ToString()));
        }

        private void AddIssue(string diagnostics)
        {
            Issues.Add(new OperationOutcomeIssue(
                OperationOutcomeConstants.IssueSeverity.Error,
                OperationOutcomeConstants.IssueType.NotSupported,
                diagnostics));
        }
    }
}
