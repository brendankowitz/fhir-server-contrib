using System.Reflection;
using Microsoft.Health.Fhir.Extensions;

namespace Microsoft.Health.Fhir.Specification.Extensions.Data;

public static class DataLoader
{
    private static readonly string ThisNamespace = typeof(DataLoader).Namespace;
    private static readonly Assembly ThisAssembly = typeof(DataLoader).Assembly;

    public static Stream OpenVersionedFileStream(FhirSpecification fhirVersion, string filename, string @namespace = null, Assembly assembly = null)
    {
        var manifestName = $"{@namespace ?? ThisNamespace}.{fhirVersion}.{filename}";
        return (assembly ?? ThisAssembly).GetManifestResourceStream(manifestName);
    }
}