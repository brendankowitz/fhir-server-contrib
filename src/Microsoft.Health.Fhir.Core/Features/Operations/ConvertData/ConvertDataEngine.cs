﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using DotLiquid;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features.Operations.ConvertData.Models;
using Microsoft.Health.Fhir.Core.Messages.ConvertData;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;

namespace Microsoft.Health.Fhir.Core.Features.Operations.ConvertData;

public class ConvertDataEngine : IConvertDataEngine
{
    private readonly ConvertDataConfiguration _convertDataConfiguration;
    private readonly IConvertDataTemplateProvider _convertDataTemplateProvider;

    private readonly Dictionary<DataType, IFhirConverter> _converterMap = new();
    private readonly ILogger<ConvertDataEngine> _logger;

    public ConvertDataEngine(
        IConvertDataTemplateProvider convertDataTemplateProvider,
        IOptions<ConvertDataConfiguration> convertDataConfiguration,
        ILogger<ConvertDataEngine> logger)
    {
        EnsureArg.IsNotNull(convertDataTemplateProvider, nameof(convertDataTemplateProvider));
        EnsureArg.IsNotNull(convertDataConfiguration, nameof(convertDataConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _convertDataTemplateProvider = convertDataTemplateProvider;
        _convertDataConfiguration = convertDataConfiguration.Value;
        _logger = logger;

        InitializeConvertProcessors();
    }

    public async Task<ConvertDataResponse> Process(ConvertDataRequest convertRequest, CancellationToken cancellationToken)
    {
        List<Dictionary<string, Template>> templateCollection = await _convertDataTemplateProvider.GetTemplateCollectionAsync(convertRequest, cancellationToken);

        ITemplateProvider templateProvider = new TemplateProvider(templateCollection);
        if (templateProvider == null)
        {
            // This case should never happen.
            _logger.LogInformation("Invalid input data type for conversion.");
            throw new RequestNotValidException("Invalid input data type for conversion.");
        }

        string result = GetConvertDataResult(convertRequest, templateProvider, cancellationToken);

        return new ConvertDataResponse(result);
    }

    private string GetConvertDataResult(ConvertDataRequest convertRequest, ITemplateProvider templateProvider, CancellationToken cancellationToken)
    {
        IFhirConverter converter = _converterMap.GetValueOrDefault(convertRequest.InputDataType);
        if (converter == null)
        {
            // This case should never happen.
            _logger.LogInformation("Invalid input data type for conversion.");
            throw new RequestNotValidException("Invalid input data type for conversion.");
        }

        try
        {
            return converter.Convert(convertRequest.InputData, convertRequest.RootTemplate, templateProvider, cancellationToken);
        }
        catch (FhirConverterException convertException)
        {
            if (convertException.FhirConverterErrorCode == FhirConverterErrorCode.TimeoutError)
            {
                _logger.LogError(convertException.InnerException, "Convert data operation timed out.");
                throw new ConvertDataTimeoutException(Core.Resources.ConvertDataOperationTimeout, convertException.InnerException);
            }

            _logger.LogInformation(convertException, "Convert data failed.");
            throw new ConvertDataFailedException(string.Format(Core.Resources.ConvertDataFailed, convertException.Message), convertException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: convert data process failed.");
            throw new ConvertDataUnhandledException(string.Format(Core.Resources.ConvertDataFailed, ex.Message), ex);
        }
    }

    /// <summary>
    /// In order to terminate long running templates, we add timeout setting to DotLiquid rendering context,
    /// which throws a Timeout Exception when render process elapsed longer than timeout threshold.
    /// Reference: https://github.com/dotliquid/dotliquid/blob/master/src/DotLiquid/Context.cs
    /// </summary>
    private void InitializeConvertProcessors()
    {
        var processorSetting = new ProcessorSettings
        {
            TimeOut = (int)_convertDataConfiguration.OperationTimeout.TotalMilliseconds
        };

        _converterMap.Add(DataType.Hl7v2, new Hl7v2Processor(processorSetting));
        _converterMap.Add(DataType.Ccda, new CcdaProcessor(processorSetting));
        _converterMap.Add(DataType.Json, new JsonProcessor(processorSetting));
    }
}
