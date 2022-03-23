namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes;

public interface IResourceNode
{
    string Id { get; set; }
        
    string ResourceType { get; set; }
}