using Hl7.Fhir.Specification;

namespace Microsoft.Health.Fhir.Extensions.Schema;

public interface IFhirSchemaProvider : IStructureDefinitionSummaryProvider
{
    FhirSpecification Version { get; }
    
    IReadOnlySet<string> ResourceTypeNames { get; }
}