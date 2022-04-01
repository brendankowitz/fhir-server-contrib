// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Configs;

public class OperationsConfiguration
{
    public ExportJobConfiguration Export { get; set; } = new();

    public ReindexJobConfiguration Reindex { get; set; } = new();

    public ConvertDataConfiguration ConvertData { get; set; } = new();

    public ValidateOperationConfiguration Validate { get; set; } = new();

    public IntegrationDataStoreConfiguration IntegrationDataStore { get; set; } = new();

    public ImportTaskConfiguration Import { get; set; } = new();
}
