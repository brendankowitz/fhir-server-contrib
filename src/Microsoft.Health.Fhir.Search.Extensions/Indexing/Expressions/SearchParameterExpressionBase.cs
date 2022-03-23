﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Expressions
{
    /// <summary>
    /// Represents an expression over a search parameter.
    /// </summary>
    public abstract class SearchParameterExpressionBase : Expression
    {
        protected SearchParameterExpressionBase(SearchParameterInfo searchParameter)
        {
            Parameter = searchParameter;
            EnsureArg.IsNotNull(searchParameter, nameof(searchParameter));
        }

        public SearchParameterInfo Parameter { get; }
    }
}
