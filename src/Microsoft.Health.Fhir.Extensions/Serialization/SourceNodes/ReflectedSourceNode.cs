using System.ComponentModel;
using System.Text.Json.Serialization;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Extensions.Serialization.SourceNodes;

public class ReflectedSourceNode : BaseSourceNode<IExtensionData>
{
    private readonly Lazy<List<(string Name, Lazy<IEnumerable<ISourceNode>> Node)>> _propertySourceNodes;

    public ReflectedSourceNode(IExtensionData resource, string location, string name = null) : base(resource)
    {
        if (resource is ResourceJsonNode resourceJsonNode)
        {
            Name = name ?? resourceJsonNode.ResourceType;
            ResourceType = resourceJsonNode.ResourceType;
            Location = location ?? resourceJsonNode.ResourceType;
        }
        else
        {
            Name = name;
            Location = location;
        }

        _propertySourceNodes = new Lazy<List<(string Name, Lazy<IEnumerable<ISourceNode>> Node)>>(() =>
        {
            var list = new List<(string Name, Lazy<IEnumerable<ISourceNode>> Node)>();
        
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(Resource))
            {
                // ExtensionData is already handled
                if (prop.Name == nameof(IExtensionData.ExtensionData))
                {
                    continue;
                }
                
                var propName = (prop.Attributes[typeof(JsonPropertyNameAttribute)] as JsonPropertyNameAttribute)?.Name ?? prop.Name;
                
                if (typeof(IExtensionData).IsAssignableFrom(prop.PropertyType))
                {
                    list.Add((propName, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new ReflectedSourceNode((IExtensionData)prop.GetValue(Resource), $"{Location}.{propName}", propName) })));
                }
                else
                {
                    list.Add((propName, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => prop.GetValue(Resource)?.ToString(), propName, $"{Location}.{propName}") })));
                }
            }

            return list;
        });
    }

    public override string Name { get; }
    
    public override string Text { get; }
    public override string Location { get; }
    
    public override string ResourceType { get; }
    
    protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
    {
        return _propertySourceNodes.Value;
    }
}