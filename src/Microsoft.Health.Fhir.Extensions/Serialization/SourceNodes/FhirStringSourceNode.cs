// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes
{
    internal class FhirStringSourceNode : ISourceNode, IResourceTypeSupplier, IAnnotated
    {
        private readonly Func<string> _text;
        private readonly string _name;
        private readonly string _location;

        internal FhirStringSourceNode(Func<string> text, string name, string location)
        {
            _text = text;
            _name = name;
            _location = location;
        }

        public string Name => _name;

        public string Text => _text();

        public string Location => _location;

        public string ResourceType => null;

        public IEnumerable<object> Annotations(Type type)
        {
            if (type == typeof(ReflectedSourceNode) || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier))
            {
                return new[] { this };
            }

            return Enumerable.Empty<object>();
        }

        public IEnumerable<ISourceNode> Children(string name = null)
        {
            return Enumerable.Empty<ISourceNode>();
        }
    }
}