using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Hl7.Fhir.Specification;
using Json.More;
using Json.Schema;
using Microsoft.Health.Fhir.Extensions;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Specification.Extensions.Data;
using IValidatableObject = Hl7.Fhir.Validation.IValidatableObject;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace Microsoft.Health.Fhir.Specification.Extensions.Schema;

public class FhirJsonSchemaStructureDefinitionSummaryProvider : IStructureDefinitionSummaryProvider, IFhirSchemaProvider
{
    public FhirSpecification Version { get; }

    public IReadOnlySet<string> ResourceTypeNames { get; }

    private IDictionary<string, IStructureDefinitionSummary> Types = new Dictionary<string, IStructureDefinitionSummary>();

    public FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification fhirSpecification)
    {
        Version = fhirSpecification;

        JsonSchema schema = JsonSchema
            .FromStream(DataLoader.OpenVersionedFileStream(fhirSpecification, "fhir.schema.json")).Result;

        var definitions = schema.Keywords.OfType<DefinitionsKeyword>().Single();

        var resourcesList = definitions.Definitions[SchemaStructureDefinitionSummary.ResourceListName];

        var resourceTypesLookup = resourcesList.Keywords.OfType<OneOfKeyword>().Single().Schemas
            .Select(x => definitions.Definitions.Lookup(x))
            .ToDictionary(
                x => x.Value.Name,
                x => x.Value.Schema);

        var context = new Context
        {
            Definitions = definitions.Definitions,
            ResourceTypesLookup = resourceTypesLookup,
            FullSchema = schema,
            StructureDefinitionSummaries = Types,
        };

        var extensionName = "Extension";
        var extension = definitions.Definitions[extensionName];
        var extensionStructureDefinition = new SchemaStructureDefinitionSummary(extensionName, false, false, extension, 0, context);
        Types[extensionName] = extensionStructureDefinition;
        context.Extension = extensionStructureDefinition;

        foreach (var definition in definitions.Definitions)
        {
            var key = definition.Key.Replace("_", "#");
            if (Types.ContainsKey(key))
            {
                continue;
            }

            Types.Add(key, new SchemaStructureDefinitionSummary(definition.Key, false, resourceTypesLookup.ContainsKey(definition.Key), definition.Value, 0, context));
        }

        ResourceTypeNames = resourceTypesLookup.Keys.ToImmutableHashSet();
    }

    public bool IsKnownType(string type)
    {
        return Types.ContainsKey(type);
    }

    public IStructureDefinitionSummary Provide(string canonical)
    {
        return Types[canonical];
    }

    private class Context
    {
        public IReadOnlyDictionary<string, JsonSchema> Definitions { get; set; }

        public IReadOnlyDictionary<string, JsonSchema> ResourceTypesLookup  { get; set; }

        public JsonSchema FullSchema { get; set; }

        public IDictionary<string, IStructureDefinitionSummary> StructureDefinitionSummaries { get; set; }

        public SchemaStructureDefinitionSummary Extension { get; set; }

        public IStructureDefinitionSummary GetOrCreateSimplePropertyStructureDefinition(string name)
        {
            IStructureDefinitionSummary propertyStructureDefinition;

            if (StructureDefinitionSummaries.ContainsKey(name))
            {
                propertyStructureDefinition = StructureDefinitionSummaries[name];
            }
            else
            {
                propertyStructureDefinition = new SimpleStructureDefinitionSummary(
                    name,
                    false,
                    false,
                    this);

                StructureDefinitionSummaries[name] = propertyStructureDefinition;
            }

            return propertyStructureDefinition;
        }

        public IStructureDefinitionSummary GetOrCreatePropertyStructureDefinition(
            JsonSchema parentDefinition,
            int level,
            (string Name, JsonSchema Schema)? propDefinition,
            bool propIsResource)
        {
            IStructureDefinitionSummary propertyStructureDefinition = null;
            if (propDefinition != null)
            {
                var key = $"{propDefinition.Value.Name.Replace("_", "#")}";
                var typeName = propDefinition.Value.Schema.Keywords.OfType<TypeKeyword>().SingleOrDefault();

                if (StructureDefinitionSummaries.ContainsKey(key))
                {
                    propertyStructureDefinition = StructureDefinitionSummaries[key];
                }
                else if (typeName != null)
                {
                    propertyStructureDefinition = GetOrCreateSimplePropertyStructureDefinition(key);
                }

                if (string.Equals("Extension", key) && propertyStructureDefinition == null)
                {
                    return Extension;
                }

                if (level < 10 && !parentDefinition.Equals(propDefinition?.Schema) && propertyStructureDefinition == null)
                {
                    propertyStructureDefinition = new SchemaStructureDefinitionSummary(
                        propDefinition.Value.Name,
                        false,
                        propIsResource,
                        propDefinition?.Schema,
                        ++level,
                        this);

                    StructureDefinitionSummaries[key] = propertyStructureDefinition;
                }
            }

            return propertyStructureDefinition;
        }
    }

    private class SimpleStructureDefinitionSummary : IStructureDefinitionSummary
    {
        private readonly Context _context;

        public SimpleStructureDefinitionSummary(string typeName, bool isAbstract, bool isResource, Context context)
        {
            _context = context;
            TypeName = typeName;
            IsAbstract = isAbstract;
            IsResource = isResource;
        }

        public string TypeName { get; }

        public bool IsAbstract { get; }

        public bool IsResource { get; }

        public IReadOnlyCollection<IElementDefinitionSummary> GetElements()
        {
            return new[]
            {
                new ElementDefinitionSummary("id", false, false, false, XmlRepresentation.XmlElement, new[] { _context.Extension }, 0, null, false, false),
                new ElementDefinitionSummary("extension", true, false, false, XmlRepresentation.XmlElement, new[] { _context.Extension }, 0, null, false, false)
            };
        }
    }

    private class SchemaStructureDefinitionSummary : IStructureDefinitionSummary, IValidatableObject
    {
        private readonly JsonSchema _definition;
        private readonly Context _context;

        private IReadOnlyCollection<IElementDefinitionSummary> _elements;
        private readonly JsonSchema _fullSchema;
        internal const string ResourceListName = "ResourceList";

        public SchemaStructureDefinitionSummary(
            string typeName,
            bool isAbstract,
            bool isResource,
            JsonSchema definition,
            int level,
            Context context)
        {
            _definition = definition;
            _context = context;
            TypeName = typeName?.Replace("_", "#");
            IsAbstract = isAbstract;
            IsResource = isResource;
            _fullSchema = context.FullSchema;

            ISet<IElementDefinitionSummary> elements = new HashSet<IElementDefinitionSummary>();

            var required = definition?.Keywords.OfType<RequiredKeyword>().SingleOrDefault();
            var propertiesKeyword = definition?.Keywords.OfType<PropertiesKeyword>().SingleOrDefault();

            Dictionary<string, JsonSchema> properties = new Dictionary<string, JsonSchema>();
            if (propertiesKeyword != null)
            {
                foreach (var schema in propertiesKeyword.Properties)
                {
                    properties.Add(schema.Key, schema.Value);
                }
            }

            if (properties.Any())
            {
                foreach (var element in properties
                             .GroupBy(x => x.Key.Trim('_'))
                             .Select(((value, i) => (value, i))))
                {
                    var property = ProcessProperty(definition, required, (element.value.First(), element.i), level, context);

                    elements.Add(property);
                }
            }

            if (!properties.Any(x => string.Equals("extension", x.Key)) && context.Extension != null)
            {
                elements.Add(new ElementDefinitionSummary("extension", true, false, false, XmlRepresentation.XmlElement, new[] { context.Extension }, 0, null, false, false));
            }

            _elements = elements.ToImmutableArray();
        }

        private SchemaElementDefinitionSummary ProcessProperty(
            JsonSchema parentDefinition,
            RequiredKeyword required,
            (KeyValuePair<string, JsonSchema> value, int i) element,
            int level,
            Context context)
        {
            var isRequired = required?.Properties.Contains(element.value.Key) == true;
            var propDefinition = context.Definitions.Lookup(element.value.Value);

            var propIsResource =
                propDefinition != null && (context.ResourceTypesLookup.ContainsKey(propDefinition.Value.Name) ||
                                           string.Equals(ResourceListName, propDefinition.Value.Name));

            var propertyStructureDefinition = context
                .GetOrCreatePropertyStructureDefinition(parentDefinition, level, propDefinition, propIsResource);

            var typeKeyword = element.value.Value.Keywords.OfType<TypeKeyword>().SingleOrDefault()?.Type.ToString().ToLower();

            ITypeSerializationInfo[] typeSerializationInfos;

            if (string.Equals(ResourceListName, propDefinition?.Name))
            {
                typeSerializationInfos = new ITypeSerializationInfo[] { null };
            }
            else if (parentDefinition.Equals(propDefinition?.Schema))
            {
                typeSerializationInfos = new ITypeSerializationInfo[] { this };
            }
            else if (propertyStructureDefinition == null)
            {
                // Simple properties (i.e. const)
                typeSerializationInfos = new ITypeSerializationInfo[] { context.GetOrCreateSimplePropertyStructureDefinition(propDefinition?.Name ?? typeKeyword ?? "string") };
            }
            else
            {
                typeSerializationInfos = new ITypeSerializationInfo[] { propertyStructureDefinition };
            }

            return new SchemaElementDefinitionSummary(
                element.value.Key,
                string.Equals("array", typeKeyword),
                isRequired,
                isRequired,
                (element.value.Key.StartsWith("_value") || element.value.Key.StartsWith("_value")) &&
                (element.value.Key != "value" || (element.value.Key != "_value")),
                propIsResource,
                typeSerializationInfos,
                typeSerializationInfos?.FirstOrDefault()?.GetTypeName() ?? typeKeyword,
                null,
                propDefinition?.Name?.Contains("html") == true ? XmlRepresentation.XHtml : XmlRepresentation.TypeAttr,
                element.i);
        }

        public string TypeName { get; }
        public bool IsAbstract { get; }
        public bool IsResource { get; }

        public IReadOnlyCollection<IElementDefinitionSummary> GetElements()
        {
            return _elements;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            IEnumerable<ValidationResult> Find(ValidationResults validationResults)
            {
                var results = new List<ValidationResult>();

                if (validationResults.HasNestedResults)
                {
                    foreach (var r in validationResults.NestedResults)
                    {
                        results.AddRange(Find(r));
                    }
                }

                if (!string.IsNullOrEmpty(validationResults.Message)) // && validationResults.AbsoluteSchemaLocation?.ToString().Contains(TypeName) == true)
                {
                    results.Add(new ValidationResult(validationResults.Message, new[] { validationResults.InstanceLocation.ToString() }));
                }

                return results;
            }

            if (!(validationContext.ObjectInstance is JsonDocument jsonDocument))
            {
                jsonDocument = validationContext.ObjectInstance.ToJsonDocument();
            }

            var validationOptions = new ValidationOptions();

            validationOptions.DefaultBaseUri = new Uri("http://hl7.org/fhir/json-schema/4.0");
            //validationOptions.SchemaRegistry.Register(validationOptions.DefaultBaseUri, _fullSchema);

            foreach (var subSchema in _fullSchema.Keywords.OfType<DefinitionsKeyword>().Single().Definitions)
            {
                validationOptions.SchemaRegistry.RegisterAnchor(validationOptions.DefaultBaseUri, $"#/definitions/{subSchema.Key}", subSchema.Value);
            }


            validationOptions.OutputFormat = OutputFormat.Verbose;

            ValidationResults results = _definition.Validate(jsonDocument.RootElement, validationOptions);

            return Find(results);
        }
    }

    private class SchemaElementDefinitionSummary : IElementDefinitionSummary
    {
        public SchemaElementDefinitionSummary(string elementName, bool isCollection, bool isRequired, bool inSummary, bool isChoiceElement, bool isResource, ITypeSerializationInfo[] type, string defaultTypeName, string nonDefaultNamespace, XmlRepresentation representation, int order)
        {
            ElementName = elementName;
            IsCollection = isCollection;
            IsRequired = isRequired;
            InSummary = inSummary;
            IsChoiceElement = isChoiceElement;
            IsResource = isResource;
            Type = type;
            DefaultTypeName = defaultTypeName;
            NonDefaultNamespace = nonDefaultNamespace;
            Representation = representation;
            Order = order;
        }

        public string ElementName { get; }

        public bool IsCollection { get; }

        public bool IsRequired { get; }

        public bool InSummary { get; }

        public bool IsChoiceElement { get; }

        public bool IsResource { get; }

        public ITypeSerializationInfo[] Type { get; }

        public string DefaultTypeName { get; }

        public string NonDefaultNamespace { get; }

        public XmlRepresentation Representation { get; }

        public int Order { get; }
    }
}