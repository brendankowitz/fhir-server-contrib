﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Health.Fhir.Extensions;

namespace Microsoft.Health.Fhir.Search.Extensions.Data
{
    public static class DataLoader
    {
        private static readonly string _thisNamespace = typeof(DataLoader).Namespace;
        private static readonly Assembly _thisAssembly = typeof(DataLoader).Assembly;

        public static Stream OpenVersionedFileStream(FhirSpecification fhirVersion, string filename, string @namespace = null, Assembly assembly = null)
        {
            string manifestName = $"{@namespace ?? _thisNamespace}.{fhirVersion}.{filename}";
            return (assembly ?? _thisAssembly).GetManifestResourceStream(manifestName);
        }
    }
}