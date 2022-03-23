using Json.Schema;

namespace Microsoft.Health.Fhir.Specification.Extensions.Schema;

internal static class JsonSchemaExtensions
{
    public static string ToName(this Uri reference)
    {
        return reference.ToString().Split('/').Last();
    }
    
    public static (string Name, JsonSchema Schema)? Lookup(this IReadOnlyDictionary<string, JsonSchema> schema, JsonSchema reference)
    {
        var singleOrDefault = reference?.Keywords?.OfType<RefKeyword>().SingleOrDefault();

        if (singleOrDefault == null)
        {
            singleOrDefault = reference?.Keywords?.OfType<ItemsKeyword>().SingleOrDefault()?.SingleSchema?.Keywords
                ?.OfType<RefKeyword>().SingleOrDefault();
        }

        if (singleOrDefault == null)
        {
            return null;
        }
        
        var name = singleOrDefault.Reference.ToName();
        return (name, schema[name]);
    }
}