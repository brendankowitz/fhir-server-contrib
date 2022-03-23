using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Extensions.Serialization;
using Microsoft.Health.Fhir.Extensions.Serialization.DynamicSourceNodeTypes;
using Microsoft.Health.Fhir.Specification.Extensions.Schema;
using Xunit;
using IValidatableObject = Hl7.Fhir.Validation.IValidatableObject;

namespace Microsoft.Health.Fhir.Extensions.Tests
{
    public class FhirJsonTextNodeTests
    {
        private readonly string _patientJson = @"{
  ""resourceType"" : ""Patient"",
  ""name"" : [{
    ""id"" : ""f2"",
    ""use"" : ""official"" ,
    ""given"" : [ ""Karen"", ""May"" ],
    ""_given"" : [ null, {""id"" : ""middle""} ],
    ""family"" :  ""Van"",
    ""_family"" : {""id"" : ""a2""}
   }],
  ""text"" : {
    ""status"" : ""generated"" ,
    ""div"" : ""<div xmlns=\""http://www.w3.org/1999/xhtml\""><p>...</p></div>""
  }
}";

        [Fact]
        public void ReadShadowProperty()
        {
            var sourceNode = JsonSourceNodeFactory.Parse(_patientJson);
            var meta = InstanceInferredStructureDefinitionSummaryProvider.CreateFrom(sourceNode);

            var node = sourceNode.ToTypedElement(meta);

            var familyName = node.Scalar("Patient.name.family");
            var familyId = node.Scalar("Patient.name.family.id");
            Assert.Equal("Van", familyName);
            Assert.Equal("a2", familyId);

            var middle = node.Scalar("Patient.name.given[1]");
            var middleId = node.Scalar("Patient.name.given[1].id");
            Assert.Equal("May", middle);
            Assert.Equal("middle", middleId);

            var firstName = node.Scalar("Patient.name.given[0]");
            var firstNameId = node.Scalar("Patient.name.given[0].id");
            Assert.Equal("Karen", firstName);
            Assert.Null(firstNameId);
        }

        [Fact]
        public void SourceNode()
        {
            var sourceNode = JsonSourceNodeFactory.Parse(_patientJson);
            var meta = new FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification.R4);

            var node = sourceNode.ToTypedElement(meta);
            var familyType = node.Select("Patient.name.family").Single();

            var definitions = familyType.ChildDefinitions(meta);

        }

        [Fact]
        public void WithJsonDoc()
        {
            var jsonDoc = JsonDocument.Parse(_patientJson);
            var sourceNode = jsonDoc.CreateSourceNode();

            var meta = new FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification.R4);

            var node = sourceNode.ToTypedElement(meta);
            var familyType = node.Select("Patient.name.family").Single();

            var definitions = familyType.ChildDefinitions(meta);
        }

        [Fact]
        public void ValidateObject()
        {
            var jsonDoc = JsonDocument.Parse(_patientJson);

            var meta = new FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification.R4);
            var node = jsonDoc.CreateSourceNode();

            var summary = meta.Provide(node.GetResourceTypeIndicator()) as IValidatableObject;

            var results = summary.Validate(new ValidationContext(jsonDoc)).ToArray();


            var sourceNode = JsonDocument.Parse("{ \"resourceType\": \"Boo\" }");

            results = summary.Validate(new ValidationContext(sourceNode)).ToArray();
        }

        [Fact]
        public void WithSchema()
        {
            var sourceNode = JsonSourceNodeFactory.Parse(_patientJson);
            var meta = new FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification.R4);

            var node = sourceNode.ToTypedElement(meta);

            var familyName = node.Scalar("Patient.name.family");
            var familyId = node.Scalar("Patient.name.family.id");
            Assert.Equal("Van", familyName);
            Assert.Equal("a2", familyId);

            var middle = node.Scalar("Patient.name.given[1]");
            var middleId = node.Scalar("Patient.name.given[1].id");
            Assert.Equal("May", middle);
            Assert.Equal("middle", middleId);

            var firstName = node.Scalar("Patient.name.given[0]");
            var firstNameId = node.Scalar("Patient.name.given[0].id");
            Assert.Equal("Karen", firstName);
            Assert.Null(firstNameId);
        }


        [Fact]
        public void WithJsonNode()
        {
            var sourceNode = JsonNodeSourceNode.FromRoot(JsonNode.Parse(_patientJson));
            var meta = new FhirJsonSchemaStructureDefinitionSummaryProvider(FhirSpecification.R4);

            var node = sourceNode.ToTypedElement(meta);

            var familyName = node.Scalar("Patient.name.family");
            var familyId = node.Scalar("Patient.name.family.id");
            Assert.Equal("Van", familyName);
            Assert.Equal("a2", familyId);

            var familyNodes = node.Select("Patient.name.family").Single();
            if (familyNodes is ScopedNode sn && sn.Current is JsonNodeSourceNode jsonFamilyNode)
            {

            }
        }
    }
}