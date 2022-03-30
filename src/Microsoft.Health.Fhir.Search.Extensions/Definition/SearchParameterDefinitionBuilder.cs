﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Extensions;
using Microsoft.Health.Fhir.Extensions.Exceptions;
using Microsoft.Health.Fhir.Extensions.Models;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Extensions.Serialization;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;
using Microsoft.Health.Fhir.Extensions.ValueSets;
using Microsoft.Health.Fhir.Search.Extensions.Data;
using Microsoft.Health.Fhir.Search.Extensions.Definition.BundleNavigators;
using Microsoft.Health.Fhir.Search.Extensions.Indexing;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Definition
{
    internal static class SearchParameterDefinitionBuilder
    {
        private static readonly ISet<Uri> _knownBrokenR5 = new HashSet<Uri>
        {
            new Uri("http://hl7.org/fhir/SearchParameter/EvidenceVariable-topic"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/ImagingStudy-reason"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/Medication-form"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/MedicationKnowledge-packaging-cost"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/MedicationKnowledge-packaging-cost-concept"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/Subscription-payload"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/Subscription-type"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/Subscription-payload"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/Subscription-url"), // expression is null or empty.
            new Uri("http://hl7.org/fhir/SearchParameter/TestScript-scope-artifact-phase"), // referencing non existing search param.
            new Uri("http://hl7.org/fhir/SearchParameter/TestScript-scope-artifact-conformance"), // referencing non existing search param.
        };

        internal static void Build(
            IReadOnlyCollection<ITypedElement> searchParameters,
            ConcurrentDictionary<Uri, SearchParameterInfo> uriDictionary,
            ConcurrentDictionary<string, ConcurrentDictionary<string, SearchParameterInfo>> resourceTypeDictionary,
            IFhirSchemaProvider modelInfoProvider)
        {
            EnsureArg.IsNotNull(searchParameters, nameof(searchParameters));
            EnsureArg.IsNotNull(uriDictionary, nameof(uriDictionary));
            EnsureArg.IsNotNull(resourceTypeDictionary, nameof(resourceTypeDictionary));
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));

            ILookup<string, SearchParameterInfo> searchParametersLookup = ValidateAndGetFlattenedList(
                searchParameters,
                uriDictionary,
                modelInfoProvider).ToLookup(
                    entry => entry.ResourceType,
                    entry => entry.SearchParameter);

            // Build the inheritance. For example, the _id search parameter is on Resource
            // and should be available to all resources that inherit Resource.
            foreach (string resourceType in modelInfoProvider.ResourceTypeNames)
            {
                // Recursively build the search parameter definitions. For example,
                // Appointment inherits from DomainResource, which inherits from Resource
                // and therefore Appointment should include all search parameters DomainResource and Resource supports.
                BuildSearchParameterDefinition(searchParametersLookup, resourceType, resourceTypeDictionary, modelInfoProvider);
            }
        }

        private static bool ShouldExcludeEntry(string resourceType, string searchParameterName, IFhirSchemaProvider modelInfoProvider)
        {
            return (resourceType == KnownResourceTypes.DomainResource && searchParameterName == "_text") ||
                   (resourceType == KnownResourceTypes.Resource && searchParameterName == "_content") ||
                   (resourceType == KnownResourceTypes.Resource && searchParameterName == "_query") ||
                   (resourceType == KnownResourceTypes.Resource && searchParameterName == "_list") ||
                   ShouldExcludeEntryStu3(resourceType, searchParameterName, modelInfoProvider);
        }

        private static bool ShouldExcludeEntryStu3(string resourceType, string searchParameterName, IFhirSchemaProvider modelInfoProvider)
        {
            return modelInfoProvider.Version == FhirSpecification.Stu3 &&
                   resourceType == "DataElement" && (searchParameterName == "objectClass" || searchParameterName == "objectClassProperty");
        }

        internal static BundleNavigator ReadEmbeddedSearchParameters(
            string embeddedResourceName,
            IFhirSchemaProvider modelInfoProvider,
            string embeddedResourceNamespace = null,
            Assembly assembly = null)
        {
            using Stream stream = DataLoader.OpenVersionedFileStream(modelInfoProvider.Version, embeddedResourceName, embeddedResourceNamespace, assembly);

            var sourceNode = JsonSourceNodeFactory.Parse(stream).Result;

            return new BundleNavigator(sourceNode.ToTypedElement(modelInfoProvider));
        }

        private static SearchParameterInfo GetOrCreateSearchParameterInfo(SearchParameterNavigator searchParameter, IDictionary<Uri, SearchParameterInfo> uriDictionary)
        {
            // Return SearchParameterInfo that has already been created for this Uri
            if (uriDictionary.TryGetValue(new Uri(searchParameter.Url, UriKind.RelativeOrAbsolute), out var spi))
            {
                return spi;
            }

            return new SearchParameterInfo(searchParameter);
        }

        private static List<(string ResourceType, SearchParameterInfo SearchParameter)> ValidateAndGetFlattenedList(
            IReadOnlyCollection<ITypedElement> searchParamCollection,
            IDictionary<Uri, SearchParameterInfo> uriDictionary,
            IFhirSchemaProvider modelInfoProvider)
        {
            var issues = new List<OperationOutcomeIssue>();
            var searchParameters = searchParamCollection.Select((x, entryIndex) =>
            {
                try
                {
                    return new SearchParameterNavigator(x);
                }
                catch (ArgumentException)
                {
                    AddIssue(Resources.SearchParameterDefinitionInvalidResource, entryIndex);
                    return null;
                }
            }).ToList();

            // Do the first pass to make sure all resources are SearchParameter.
            for (int entryIndex = 0; entryIndex < searchParameters.Count; entryIndex++)
            {
                SearchParameterNavigator searchParameter = searchParameters[entryIndex];

                if (searchParameter == null)
                {
                    continue;
                }

                try
                {
                    SearchParameterInfo searchParameterInfo = GetOrCreateSearchParameterInfo(searchParameter, uriDictionary);
                    uriDictionary.Add(new Uri(searchParameter.Url), searchParameterInfo);
                }
                catch (FormatException)
                {
                    AddIssue(Resources.SearchParameterDefinitionInvalidDefinitionUri, entryIndex);
                    continue;
                }
                catch (ArgumentException)
                {
                    AddIssue(Resources.SearchParameterDefinitionDuplicatedEntry, searchParameter.Url);
                    continue;
                }
            }

            EnsureNoIssues();

            var validatedSearchParameters = new List<(string ResourceType, SearchParameterInfo SearchParameter)>
            {
                // _type is currently missing from the search params definition bundle, so we inject it in here.
                (KnownResourceTypes.Resource, new SearchParameterInfo(SearchParameterNames.ResourceType, SearchParameterNames.ResourceType, SearchParamType.Token, SearchParameterNames.ResourceTypeUri, null, "Resource.type().name", null)),
            };

            // Do the second pass to make sure the definition is valid.
            foreach (var searchParameter in searchParameters)
            {
                if (searchParameter == null)
                {
                    continue;
                }

                // If this is a composite search parameter, then make sure components are defined.
                if (string.Equals(searchParameter.Type, SearchParamType.Composite.GetLiteral(), StringComparison.OrdinalIgnoreCase))
                {
                    if (modelInfoProvider.Version == FhirSpecification.R5 && _knownBrokenR5.Contains(new Uri(searchParameter.Url)))
                    {
                        continue;
                    }

                    var composites = searchParameter.Component;
                    if (composites.Count == 0)
                    {
                        AddIssue(Resources.SearchParameterDefinitionInvalidComponent, searchParameter.Url);
                        continue;
                    }

                    SearchParameterInfo compositeSearchParameter = GetOrCreateSearchParameterInfo(searchParameter, uriDictionary);

                    for (int componentIndex = 0; componentIndex < composites.Count; componentIndex++)
                    {
                        ITypedElement component = composites[componentIndex];
                        var definitionUrl = GetComponentDefinition(component);

                        if (definitionUrl == null ||
                            !uriDictionary.TryGetValue(new Uri(definitionUrl), out SearchParameterInfo componentSearchParameter))
                        {
                            AddIssue(
                                Resources.SearchParameterDefinitionInvalidComponentReference,
                                searchParameter.Url,
                                componentIndex);
                            continue;
                        }

                        if (componentSearchParameter.Type == SearchParamType.Composite)
                        {
                            AddIssue(
                                Resources.SearchParameterDefinitionComponentReferenceCannotBeComposite,
                                searchParameter.Url,
                                componentIndex);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(component.Scalar("expression")?.ToString()))
                        {
                            AddIssue(
                                Resources.SearchParameterDefinitionInvalidComponentExpression,
                                searchParameter.Url,
                                componentIndex);
                            continue;
                        }

                        compositeSearchParameter.Component[componentIndex].ResolvedSearchParameter = componentSearchParameter;
                    }
                }

                // Make sure the base is defined.
                IReadOnlyList<string> bases = searchParameter.Base;
                if (bases.Count == 0)
                {
                    AddIssue(Resources.SearchParameterDefinitionBaseNotDefined, searchParameter.Url);
                    continue;
                }

                for (int baseElementIndex = 0; baseElementIndex < bases.Count; baseElementIndex++)
                {
                    var code = bases[baseElementIndex];

                    string baseResourceType = code;

                    // Make sure the expression is not empty unless they are known to have empty expression.
                    // These are special search parameters that searches across all properties and needs to be handled specially.
                    if (ShouldExcludeEntry(baseResourceType, searchParameter.Name, modelInfoProvider)
                    || (modelInfoProvider.Version == FhirSpecification.R5 && _knownBrokenR5.Contains(new Uri(searchParameter.Url))))
                    {
                        continue;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(searchParameter.Expression))
                        {
                            AddIssue(Resources.SearchParameterDefinitionInvalidExpression, searchParameter.Url);
                            continue;
                        }
                    }

                    validatedSearchParameters.Add((baseResourceType, GetOrCreateSearchParameterInfo(searchParameter, uriDictionary)));
                }
            }

            EnsureNoIssues();

            return validatedSearchParameters;

            void AddIssue(string format, params object[] args)
            {
                issues.Add(new OperationOutcomeIssue(
                    OperationOutcomeConstants.IssueSeverity.Fatal,
                    OperationOutcomeConstants.IssueType.Invalid,
                    string.Format(CultureInfo.InvariantCulture, format, args)));
            }

            void EnsureNoIssues()
            {
                if (issues.Count != 0)
                {
                    throw new InvalidDefinitionException(
                        Resources.SearchParameterDefinitionContainsInvalidEntry,
                        issues.ToArray());
                }
            }
        }

        private static HashSet<SearchParameterInfo> BuildSearchParameterDefinition(
            ILookup<string, SearchParameterInfo> searchParametersLookup,
            string resourceType,
            ConcurrentDictionary<string, ConcurrentDictionary<string, SearchParameterInfo>> resourceTypeDictionary,
            IFhirSchemaProvider modelInfoProvider)
        {
            HashSet<SearchParameterInfo> results;
            if (resourceTypeDictionary.TryGetValue(resourceType, out ConcurrentDictionary<string, SearchParameterInfo> cachedSearchParameters))
            {
                results = new HashSet<SearchParameterInfo>(cachedSearchParameters.Values);
            }
            else
            {
                results = new HashSet<SearchParameterInfo>();
            }

            string baseType = null;

            if (!string.Equals(resourceType, KnownResourceTypes.Resource) && !string.Equals(resourceType, KnownResourceTypes.Base))
            {
                baseType = KnownResourceTypes.Resource;
            }
            else if (!string.Equals(resourceType, KnownResourceTypes.Base))
            {
                baseType = KnownResourceTypes.Base;
            }

            if (baseType != null && !string.Equals(KnownResourceTypes.Base, baseType, StringComparison.OrdinalIgnoreCase))
            {
                HashSet<SearchParameterInfo> baseResults = BuildSearchParameterDefinition(searchParametersLookup, baseType, resourceTypeDictionary, modelInfoProvider);
                results.UnionWith(baseResults);
            }

            results.UnionWith(searchParametersLookup[resourceType]);

            var searchParameterDictionary = new ConcurrentDictionary<string, SearchParameterInfo>(
                results.ToDictionary(
                r => r.Code,
                r => r,
                StringComparer.Ordinal));

            if (!resourceTypeDictionary.TryAdd(resourceType, searchParameterDictionary))
            {
                resourceTypeDictionary[resourceType] = searchParameterDictionary;
            }

            return results;
        }

        private static string GetComponentDefinition(ITypedElement component)
        {
            // In Stu3 the Url is under 'definition.reference'
            return component.Scalar("definition.reference")?.ToString() ??
                   component.Scalar("definition")?.ToString();
        }
    }
}