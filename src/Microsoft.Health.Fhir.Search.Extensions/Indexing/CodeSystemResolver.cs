// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Fhir.Extensions;
using Microsoft.Health.Fhir.Search.Extensions.Data;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Search.Extensions.Indexing
{
    public sealed class CodeSystemResolver : ICodeSystemResolver
    {
        private Dictionary<string, string> _dictionary;

        public CodeSystemResolver(FhirSpecification fhirSpecification)
        {
            using Stream file = DataLoader.OpenVersionedFileStream(fhirSpecification, "resourcepath-codesystem-mappings.json");
            using var reader = new StreamReader(file);
            _dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }

        public string ResolveSystem(string shortPath)
        {
            EnsureArg.IsNotNullOrWhiteSpace(shortPath, nameof(shortPath));

            if (_dictionary == null)
            {
                throw new InvalidOperationException($"{nameof(CodeSystemResolver)} has not been initialized.");
            }

            if (_dictionary.TryGetValue(NormalizePath(shortPath), out var system))
            {
                return system;
            }

            return null;
        }

        private static string NormalizePath(string path) =>
         Regex.Replace(path, "\\[\\w+\\]", string.Empty);
    }
}
