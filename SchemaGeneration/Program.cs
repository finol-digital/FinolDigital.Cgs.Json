// See https://aka.ms/new-console-template for more information

using FinolDigital.Cgs.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

string SchemaFileName = "cgs.json";
string SchemaId = "https://cardgamesim.finoldigital.com/schema/cgs.json";
string SchemaTitle = "CGS Card Game Specification";
string SchemaDescription = "Card Game Simulator (CGS) Card Game Specification";

NewtonsoftJsonSchemaGeneratorSettings settings = new()
{
    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
    SerializerSettings = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    }
};
var generator = new JsonSchemaGenerator(settings);
var schema = generator.Generate(typeof(CardGame));
schema.Id = SchemaId;
schema.Title = SchemaTitle;
schema.Description = SchemaDescription;
// HACK: cardImageUrl uses a custom implementation of uri-template to allow for more versatility
schema.Properties["cardImageUrl"].Format = "uri-template";
File.WriteAllText(SchemaFileName, schema.ToJson());

/*
using Newtonsoft.Json.Schema.Generation;

JSchemaGenerator generator = new();
//generator.GenerationProviders.Add(new FormatSchemaProvider());
JSchema schema = generator.Generate(typeof(CardGame));
File.WriteAllText(SchemaFileName, schema.ToString());

using Json.Schema;
using Json.Schema.Generation;
using System.Text.Json;
using System.Text.Json.Serialization;

var jsonSchema = new JsonSchemaBuilder().FromType<CardGame>().Build();
string jsonSchemaString = JsonSerializer.Serialize(jsonSchema, new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles });
File.WriteAllText(SchemaFilePath, jsonSchemaString);
*/