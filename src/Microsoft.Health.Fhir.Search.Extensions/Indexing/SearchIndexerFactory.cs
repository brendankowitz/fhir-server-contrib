﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Search.Extensions.Definition;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing;

public static class SearchIndexerFactory
{
    public static async Task<ISearchIndexer> CreateInstance(IFhirSchemaProvider fhirSchemaProvider, ILoggerFactory loggerProvider)
    {
        var definitionManager = new SearchParameterDefinitionManager(fhirSchemaProvider, loggerProvider.CreateLogger<SearchParameterDefinitionManager>());

        var referenceParser = new ReferenceSearchValueParser(fhirSchemaProvider);
        var elementResolver = new LightweightReferenceToElementResolver(referenceParser);
        var codesystems = new CodeSystemResolver(fhirSchemaProvider.Version);

        ITypedElementToSearchValueConverter[] converters = typeof(TypedElementSearchIndexer)
            .Assembly
            .ExportedTypes
            .Where(x => typeof(ITypedElementToSearchValueConverter).IsAssignableFrom(x) && !x.IsAbstract && !x.IsGenericType)
            .Select(x => (ITypedElementToSearchValueConverter)CreateTypeWithArguments(x, fhirSchemaProvider, referenceParser, elementResolver, codesystems, fhirSchemaProvider.Version))
            .ToArray();

        await definitionManager.Start();

        return new TypedElementSearchIndexer(
            new SupportedSearchParameterDefinitionManager(definitionManager),
            new FhirTypedElementToSearchValueConverterManager(converters),
            elementResolver,
            loggerProvider.CreateLogger<TypedElementSearchIndexer>());
    }

    private static object CreateTypeWithArguments(Type type, params object[] argOverrides)
    {
        EnsureArg.IsNotNull(type, nameof(type));

        if (argOverrides.Any(x => x == null)) throw new ArgumentNullException(nameof(argOverrides), "Values for argument overrides should not be null");

        ConstructorInfo constructor = type.GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();

        if (constructor == null) throw new ArgumentException($"{type} has no usable constructors", nameof(type));

        var arguments = new List<object>();
        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            object overridden = argOverrides.FirstOrDefault(x => parameter.ParameterType.IsAssignableFrom(x.GetType()));
            if (overridden != null)
            {
                arguments.Add(overridden);
            }
            else
            {
                if (parameter.ParameterType.IsClass && !parameter.ParameterType.GetConstructors().Any()) throw new ArgumentException($"{parameter.ParameterType} has no usable constructors. Used to create {type}", nameof(type));

                if (parameter.ParameterType.IsClass && parameter.ParameterType.GetConstructors().Min(x => x.GetParameters().Length) > 0)
                    arguments.Add(CreateTypeWithArguments(parameter.ParameterType, argOverrides));
                else
                    throw new ArgumentNullException(nameof(argOverrides), $"Unable to find a value for {parameter.ParameterType}");
            }
        }

        return Activator.CreateInstance(type, arguments.ToArray());
    }
}
