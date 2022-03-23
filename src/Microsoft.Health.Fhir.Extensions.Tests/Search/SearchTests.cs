using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Fhir.Extensions.Schema;
using Microsoft.Health.Fhir.Extensions.Serialization;
using Microsoft.Health.Fhir.Search.Extensions.Data;
using Microsoft.Health.Fhir.Search.Extensions.Definition;
using Microsoft.Health.Fhir.Search.Extensions.Indexing;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.Converters;
using Microsoft.Health.Fhir.Search.Extensions.Indexing.SearchValues;
using Microsoft.Health.Fhir.Specification.Extensions.Data;
using Microsoft.Health.Fhir.Specification.Extensions.Schema;
using Xunit;
using DataLoader = Microsoft.Health.Fhir.Search.Extensions.Data.DataLoader;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Extensions.Tests.Search;

public class SearchTests
{
    private (ISearchIndexer Indexer, IFhirSchemaProvider FhirSchemaProvider) SetupSearchIndexer(FhirSpecification fhirSpecification)
    {
        var schema = new FhirJsonSchemaStructureDefinitionSummaryProvider(fhirSpecification);

        return (SearchIndexerFactory.CreateInstance(schema, new NullLoggerFactory()), schema);
    }

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
    public void Indexer2()
    {
        using var stream = DataLoader.OpenVersionedFileStream(FhirSpecification.R4, "search-parameters.json");
        using var reader = new StreamReader(stream);
        var official = new OfficialFhirSchemaProvider();
        var json = reader.ReadToEnd();

        var bundle = FhirJsonNode.Parse(json)
            .ToTypedElement(ModelInfo.ModelInspector);

        var items = bundle.Select("Bundle.entry[37].resource.component[0].definition.reference").ToArray();

        var fhirSpecification = FhirSpecification.R4;
        var schema2 = new FhirJsonSchemaStructureDefinitionSummaryProvider(fhirSpecification);

        var bundle2 = JsonSourceNodeFactory.Parse(json).ToTypedElement(schema2);
        var bundle3 = FhirJsonNode.Parse(json).ToTypedElement(schema2);

        var items2 = bundle2.Select("Bundle.entry[37].resource.component[0].definition").ToArray();
    }

    [Fact]
    public void Indexer()
    {
        var context = SetupSearchIndexer(FhirSpecification.R4);

        var patient = JsonSourceNodeFactory.Parse(_patientJson).ToTypedElement(context.FhirSchemaProvider);

        var indexes = context.Indexer.Extract(patient);
    }

    [Fact]
    public void IndexerR4B()
    {
        var contextR4 = SetupSearchIndexer(FhirSpecification.R4);
        var contextR4B = SetupSearchIndexer(FhirSpecification.R4B);

        var sourceNode = JsonSourceNodeFactory.Parse(_patientJson);
        var patientR4 = sourceNode.ToTypedElement(contextR4.FhirSchemaProvider);
        var patientR4B = sourceNode.ToTypedElement(contextR4B.FhirSchemaProvider);

        var indexesR4 = contextR4.Indexer.Extract(patientR4);
        var indexesR4B = contextR4B.Indexer.Extract(patientR4B);
    }

    public class OfficialFhirSchemaProvider : IFhirSchemaProvider
    {
        private readonly ModelInspector _models;

        public OfficialFhirSchemaProvider()
        {
            _models = new ModelInspector(FhirRelease.R4);
            _models.Import(typeof(Patient).Assembly);
        }

        public IStructureDefinitionSummary Provide(string canonical)
        {
            return _models.Provide(canonical);
        }

        public FhirSpecification Version { get; } = FhirSpecification.R4;

        public IReadOnlySet<string> ResourceTypeNames { get; } = ModelInfo.SupportedResources.ToHashSet();
    }
}