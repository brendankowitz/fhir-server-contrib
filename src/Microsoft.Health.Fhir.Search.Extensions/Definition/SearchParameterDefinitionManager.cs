// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Extensions.Exceptions;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Search.Extensions.Indexing;
using Microsoft.Health.Fhir.Search.Extensions.Models;

namespace Microsoft.Health.Fhir.Search.Extensions.Definition
{
    /// <summary>
    /// Provides mechanism to access search parameter definition.
    /// </summary>
    public class SearchParameterDefinitionManager : ISearchParameterDefinitionManager
    {
        private readonly IFhirSchemaProvider _modelInfoProvider;
        private ConcurrentDictionary<string, string> _resourceTypeSearchParameterHashMap;
        private readonly ILogger _logger;

        public SearchParameterDefinitionManager(
            IFhirSchemaProvider modelInfoProvider,
            ILogger<SearchParameterDefinitionManager> logger)
        {
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _modelInfoProvider = modelInfoProvider;
            _resourceTypeSearchParameterHashMap = new ConcurrentDictionary<string, string>();
            TypeLookup = new ConcurrentDictionary<string, ConcurrentDictionary<string, SearchParameterInfo>>();
            UrlLookup = new ConcurrentDictionary<Uri, SearchParameterInfo>();
            _logger = logger;
        }

        internal ConcurrentDictionary<Uri, SearchParameterInfo> UrlLookup { get; set; }

        // TypeLookup key is: Resource type, the inner dictionary key is the Search Parameter code.
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, SearchParameterInfo>> TypeLookup { get; }

        public IEnumerable<SearchParameterInfo> AllSearchParameters => UrlLookup.Values;

        public IReadOnlyDictionary<string, string> SearchParameterHashMap
        {
            get { return new ReadOnlyDictionary<string, string>(_resourceTypeSearchParameterHashMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)); }
        }

        public void Start()
        {
            var bundle = SearchParameterDefinitionBuilder.ReadEmbeddedSearchParameters("search-parameters.json", _modelInfoProvider);

            SearchParameterDefinitionBuilder.Build(
                bundle.Entries.Select(e => e.Resource).ToList(),
                UrlLookup,
                TypeLookup,
                _modelInfoProvider);
        }

        public IEnumerable<SearchParameterInfo> GetSearchParameters(string resourceType)
        {
            if (TypeLookup.TryGetValue(resourceType, out ConcurrentDictionary<string, SearchParameterInfo> value))
            {
                return value.Values;
            }

            throw new ResourceNotSupportedException(resourceType);
        }

        public SearchParameterInfo GetSearchParameter(string resourceType, string code)
        {
            if (TypeLookup.TryGetValue(resourceType, out ConcurrentDictionary<string, SearchParameterInfo> lookup) &&
                lookup.TryGetValue(code, out SearchParameterInfo searchParameter))
            {
                return searchParameter;
            }

            throw new SearchParameterNotSupportedException(resourceType, code);
        }

        public bool TryGetSearchParameter(string resourceType, string code, out SearchParameterInfo searchParameter)
        {
            searchParameter = null;

            return TypeLookup.TryGetValue(resourceType, out ConcurrentDictionary<string, SearchParameterInfo> searchParameters) &&
                searchParameters.TryGetValue(code, out searchParameter);
        }

        public SearchParameterInfo GetSearchParameter(Uri definitionUri)
        {
            if (UrlLookup.TryGetValue(definitionUri, out SearchParameterInfo value))
            {
                return value;
            }

            throw new SearchParameterNotSupportedException(definitionUri);
        }

        public bool TryGetSearchParameter(Uri definitionUri, out SearchParameterInfo value)
        {
            return UrlLookup.TryGetValue(definitionUri, out value);
        }

        public string GetSearchParameterHashForResourceType(string resourceType)
        {
            EnsureArg.IsNotNullOrWhiteSpace(resourceType, nameof(resourceType));

            if (_resourceTypeSearchParameterHashMap.TryGetValue(resourceType, out string hash))
            {
                return hash;
            }

            return null;
        }

        public void UpdateSearchParameterHashMap(Dictionary<string, string> updatedSearchParamHashMap)
        {
            EnsureArg.IsNotNull(updatedSearchParamHashMap, nameof(updatedSearchParamHashMap));

            foreach (KeyValuePair<string, string> kvp in updatedSearchParamHashMap)
            {
                _resourceTypeSearchParameterHashMap.AddOrUpdate(
                    kvp.Key,
                    kvp.Value,
                    (resourceType, existingValue) => kvp.Value);
            }
        }

        public void AddNewSearchParameters(IReadOnlyCollection<ITypedElement> searchParameters, bool calculateHash = true)
        {
            SearchParameterDefinitionBuilder.Build(
                searchParameters,
                UrlLookup,
                TypeLookup,
                _modelInfoProvider);

            if (calculateHash)
            {
                CalculateSearchParameterHash();
            }
        }

        private void CalculateSearchParameterHash()
        {
            foreach (string resourceName in TypeLookup.Keys)
            {
                string searchParamHash = TypeLookup[resourceName].Values.CalculateSearchParameterHash();
                _resourceTypeSearchParameterHashMap.AddOrUpdate(
                    resourceName,
                    searchParamHash,
                    (resourceType, existingValue) => searchParamHash);
            }
        }

        public void DeleteSearchParameter(string url, bool calculateHash = true)
        {
            SearchParameterInfo searchParameterInfo = null;

            if (!UrlLookup.TryRemove(new Uri(url), out searchParameterInfo))
            {
                throw new ResourceNotFoundException(string.Format(Resources.CustomSearchParameterNotfound, url));
            }

            // for search parameters with a base resource type we need to delete the search parameter
            // from all derived types as well, so we iterate across all resources
            foreach (var resourceType in TypeLookup.Keys)
            {
                TypeLookup[resourceType].TryRemove(searchParameterInfo.Code, out var removedParam);
            }

            if (calculateHash)
            {
                CalculateSearchParameterHash();
            }
        }
    }
}