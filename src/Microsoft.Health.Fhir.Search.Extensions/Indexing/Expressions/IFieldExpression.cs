﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing.Expressions;

public interface IFieldExpression
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    FieldName FieldName { get; }

    /// <summary>
    /// Gets the optional component index.
    /// </summary>
    int? ComponentIndex { get; }
}
