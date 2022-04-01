﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Extensions.ValueSets;
using Microsoft.Health.Fhir.Search.Extensions.Definition;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing;

/// <summary>
/// Provides a mechanism to create search indices.
/// </summary>
public class TypedElementSearchIndexer : ISearchIndexer
{
    private readonly ITypedElementToSearchValueConverterManager _fhirElementTypeConverterManager;
    private readonly ILogger<TypedElementSearchIndexer> _logger;
    private readonly IReferenceToElementResolver _referenceToElementResolver;
    private readonly ISupportedSearchParameterDefinitionManager _searchParameterDefinitionManager;
    private readonly ConcurrentDictionary<string, List<string>> _targetTypesLookup = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedElementSearchIndexer"/> class.
    /// </summary>
    /// <param name="searchParameterDefinitionManager">The search parameter definition manager.</param>
    /// <param name="fhirElementTypeConverterManager">The FHIR element type converter manager.</param>
    /// <param name="referenceToElementResolver">Used for parsing reference strings</param>
    /// <param name="logger">The logger.</param>
    public TypedElementSearchIndexer(
        ISupportedSearchParameterDefinitionManager searchParameterDefinitionManager,
        ITypedElementToSearchValueConverterManager fhirElementTypeConverterManager,
        IReferenceToElementResolver referenceToElementResolver,
        ILogger<TypedElementSearchIndexer> logger)
    {
        EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));
        EnsureArg.IsNotNull(fhirElementTypeConverterManager, nameof(fhirElementTypeConverterManager));
        EnsureArg.IsNotNull(referenceToElementResolver, nameof(referenceToElementResolver));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _searchParameterDefinitionManager = searchParameterDefinitionManager;
        _fhirElementTypeConverterManager = fhirElementTypeConverterManager;
        _referenceToElementResolver = referenceToElementResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<SearchIndexEntry> Extract(ITypedElement resource)
    {
        EnsureArg.IsNotNull(resource, nameof(resource));

        var entries = new List<SearchIndexEntry>();

        var context = new FhirEvaluationContext
        {
            ElementResolver = _referenceToElementResolver.Resolve,
            // This allow to resolve %resource FhirPath to provided value.
            Resource = resource
        };

        IEnumerable<SearchParameterInfo> searchParameters = _searchParameterDefinitionManager.GetSearchParameters(resource.InstanceType);

        foreach (SearchParameterInfo searchParameter in searchParameters)
        {
            if (searchParameter.Code == SearchParameterNames.ResourceType)
                // We don't index the resource type value. We just use the property on the root document.

                continue;

            if (searchParameter.Type == SearchParamType.Composite)
                entries.AddRange(ProcessCompositeSearchParameter(searchParameter, resource, context));
            else
                entries.AddRange(ProcessNonCompositeSearchParameter(searchParameter, resource, context));
        }

        return entries;
    }

    private IEnumerable<SearchIndexEntry> ProcessCompositeSearchParameter(SearchParameterInfo searchParameter, ITypedElement resource, EvaluationContext context)
    {
        Debug.Assert(searchParameter?.Type == SearchParamType.Composite, "The search parameter must be composite.");

        SearchParameterInfo compositeSearchParameterInfo = searchParameter;

        IEnumerable<ITypedElement> rootObjects = resource.Select(searchParameter.Expression, context);

        foreach (ITypedElement rootObject in rootObjects)
        {
            int numberOfComponents = searchParameter.Component.Count;
            bool skip = false;

            var componentValues = new IReadOnlyList<ISearchValue>[numberOfComponents];

            // For each object extracted from the expression, we will need to evaluate each component.
            for (int i = 0; i < numberOfComponents; i++)
            {
                SearchParameterComponentInfo component = searchParameter.Component[i];

                // First find the type of the component.
                SearchParameterInfo componentSearchParameterDefinition = searchParameter.Component[i].ResolvedSearchParameter;

                IReadOnlyList<ISearchValue> extractedComponentValues = ExtractSearchValues(
                    componentSearchParameterDefinition.Url.ToString(),
                    componentSearchParameterDefinition.Type,
                    componentSearchParameterDefinition.TargetResourceTypes,
                    rootObject,
                    component.Expression,
                    context);

                // Filter out any search value that's not valid as a composite component.
                extractedComponentValues = extractedComponentValues
                    .Where(sv => sv.IsValidAsCompositeComponent)
                    .ToArray();

                if (!extractedComponentValues.Any())
                {
                    // One of the components didn't have any value and therefore it will not be indexed.
                    skip = true;
                    break;
                }

                componentValues[i] = extractedComponentValues;
            }

            if (skip) continue;

            yield return new SearchIndexEntry(compositeSearchParameterInfo, new CompositeSearchValue(componentValues));
        }
    }

    private IEnumerable<SearchIndexEntry> ProcessNonCompositeSearchParameter(SearchParameterInfo searchParameter, ITypedElement resource, EvaluationContext context)
    {
        EnsureArg.IsNotNull(searchParameter, nameof(searchParameter));
        Debug.Assert(searchParameter.Type != SearchParamType.Composite, "The search parameter must be non-composite.");

        SearchParameterInfo searchParameterInfo = searchParameter;

        foreach (ISearchValue searchValue in ExtractSearchValues(
                     searchParameter.Url.ToString(),
                     searchParameter.Type,
                     searchParameter.TargetResourceTypes,
                     resource,
                     searchParameter.Expression,
                     context))
            yield return new SearchIndexEntry(searchParameterInfo, searchValue);
    }

    private IReadOnlyList<ISearchValue> ExtractSearchValues(
        string searchParameterDefinitionUrl,
        SearchParamType? searchParameterType,
        IReadOnlyList<string> allowedReferenceResourceTypes,
        ITypedElement element,
        string fhirPathExpression,
        EvaluationContext context)
    {
        Debug.Assert(searchParameterType != SearchParamType.Composite, "The search parameter must be non-composite.");

        var results = new List<ISearchValue>();

        // For simple value type, we can parse the expression directly.
        IEnumerable<ITypedElement> extractedValues = Enumerable.Empty<ITypedElement>();

        try
        {
            extractedValues = element.Select(fhirPathExpression, context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to extract the values using '{FhirPathExpression}' against '{ElementType}'.",
                fhirPathExpression,
                element.GetType());
        }

        Debug.Assert(extractedValues != null, "The extracted values should not be null.");

        // If there is target set, then filter the extracted values to only those types.
        if (searchParameterType == SearchParamType.Reference &&
            allowedReferenceResourceTypes?.Count > 0)
        {
            List<string> targetResourceTypes = _targetTypesLookup.GetOrAdd(searchParameterDefinitionUrl, _ =>
            {
                return allowedReferenceResourceTypes.Select(t => t.ToString()).ToList();
            });

            // TODO: The expression for reference search parameters in STU3 has issues.
            // The reference search parameter could be pointing to an element that can be multiple types. For example,
            // the Appointment.participant.actor can be type of Patient, Practitioner, Related Person, Location, and so on.
            // Some search parameter could refer to this property but restrict to certain types. For example,
            // Appointment's location search parameter is returned only when Appointment.participant.actor is Location element.
            // The STU3 expressions don't have this restriction so everything is being returned. This is addressed in R4 release (see
            // http://community.fhir.org/t/expression-seems-incorrect-for-reference-search-parameter-thats-only-applicable-to-certain-types/916/2).
            // Therefore, for now, we will need to compare the reference value itself (which can be internal or external references), and restrict
            // the values ourselves.
            extractedValues = extractedValues.Where(ev =>
            {
                if (ev.InstanceType.Equals("ResourceReference", StringComparison.OrdinalIgnoreCase)) return ev.Scalar("reference") is string rr && targetResourceTypes.Any(trt => rr.Contains(trt, StringComparison.Ordinal));

                return true;
            });
        }

        foreach (ITypedElement extractedValue in extractedValues)
        {
            if (!_fhirElementTypeConverterManager.TryGetConverter(extractedValue.InstanceType, GetSearchValueTypeForSearchParamType(searchParameterType), out ITypedElementToSearchValueConverter converter))
            {
                _logger.LogWarning(
                    "The FHIR element '{ElementType}' is not supported.",
                    extractedValue.InstanceType);

                continue;
            }

            IEnumerable<ISearchValue> searchValues = converter.ConvertTo(extractedValue);

            if (searchValues != null)
            {
                if (searchParameterType == SearchParamType.Reference && allowedReferenceResourceTypes?.Count == 1)
                {
                    // For references, if the type is not specified in the reference string, we can set the type on the search value because
                    // in this case it can only be of one type.
                    string singleAllowedResourceType = allowedReferenceResourceTypes[0];
                    foreach (ISearchValue searchValue in searchValues)
                        if (searchValue is ReferenceSearchValue rsr && string.IsNullOrEmpty(rsr.ResourceType))
                            results.Add(new ReferenceSearchValue(rsr.Kind, rsr.BaseUri, singleAllowedResourceType, rsr.ResourceId));
                        else
                            results.Add(searchValue);
                }
                else
                {
                    results.AddRange(searchValues);
                }
            }
        }

        return results;
    }

    internal static Type GetSearchValueTypeForSearchParamType(SearchParamType? searchParamType)
    {
        switch (searchParamType)
        {
            case SearchParamType.Number:
                return typeof(NumberSearchValue);
            case SearchParamType.Date:
                return typeof(DateTimeSearchValue);
            case SearchParamType.String:
                return typeof(StringSearchValue);
            case SearchParamType.Token:
                return typeof(TokenSearchValue);
            case SearchParamType.Reference:
                return typeof(ReferenceSearchValue);
            case SearchParamType.Composite:
                return typeof(CompositeSearchValue);
            case SearchParamType.Quantity:
                return typeof(QuantitySearchValue);
            case SearchParamType.Uri:
                return typeof(UriSearchValue);
            case SearchParamType.Special:
                return typeof(StringSearchValue);
            default:
                throw new ArgumentOutOfRangeException(nameof(searchParamType), searchParamType, null);
        }
    }
}
