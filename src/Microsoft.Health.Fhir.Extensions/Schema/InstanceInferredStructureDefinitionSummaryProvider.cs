using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;

namespace Microsoft.Health.Fhir.Extensions.Schema
{
    public class InstanceInferredStructureDefinitionSummaryProvider : IStructureDefinitionSummaryProvider
    {
        private readonly ISourceNode _typedElement;

        private InstanceInferredStructureDefinitionSummaryProvider(ISourceNode typedElement)
        {
            _typedElement = typedElement;
        }

        public static IStructureDefinitionSummaryProvider CreateFrom(ISourceNode typedElement)
        {
            return new InstanceInferredStructureDefinitionSummaryProvider(typedElement);
        }
        
        public IStructureDefinitionSummary Provide(string canonical)
        {
            return new GenericStructureDefinitionSummary(_typedElement);
        }

        private class GenericStructureDefinitionSummary : IStructureDefinitionSummary
        {
            private readonly ISourceNode[] _typedElement;

            public GenericStructureDefinitionSummary(params ISourceNode[] typedElement)
            {
                _typedElement = typedElement;
            }

            public string TypeName => char.IsUpper(_typedElement[0].Name[0]) ? _typedElement[0].Name : null;

            public bool IsAbstract { get; }

            public bool IsResource => !string.IsNullOrEmpty(_typedElement[0].GetResourceTypeIndicator());

            public IReadOnlyCollection<IElementDefinitionSummary> GetElements()
            {
                var children = new List<IElementDefinitionSummary>();

                foreach (var tuple in _typedElement.Children().GroupBy(x => x.Name).Select(((element, i) => (element, i))))
                {
                    children.Add(new GenericElementDefinitionSummary(tuple.element.ToArray(), tuple.i));
                }

                return children;
            }
        }

        private class GenericElementDefinitionSummary : IElementDefinitionSummary
        {
            private readonly ISourceNode[] _typedElement;

            public GenericElementDefinitionSummary(ISourceNode[] typedElement, int order)
            {
                _typedElement = typedElement;
                Order = order;
            }

            public string ElementName => _typedElement[0].Name;

            public bool IsCollection => _typedElement[0].Location.Contains("[");
            
            public bool IsRequired { get; }
            
            public bool InSummary { get; }
            
            public bool IsChoiceElement { get; }

            public bool IsResource { get; } = false;

            public ITypeSerializationInfo[] Type  => new ITypeSerializationInfo[] { new GenericStructureDefinitionSummary(_typedElement) };

            public string DefaultTypeName { get; }
            
            public string NonDefaultNamespace { get; }

            public XmlRepresentation Representation => XmlRepresentation.TypeAttr;

            public int Order { get; }
        }
    }

}