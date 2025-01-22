// See https://aka.ms/new-console-template for more information

/*
using FinolDigital.Cgs.Json;
using Json.Schema;
using Json.Schema.Generation;
using System.Text.Json;
using System.Text.Json.Serialization;

var jsonSchema = new JsonSchemaBuilder().FromType<CardGame>().Build();
string jsonSchemaString = JsonSerializer.Serialize(jsonSchema, new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles });
File.WriteAllText(SchemaFilePath, jsonSchemaString);*/

using FinolDigital.Cgs.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;

string SchemaFileName = "cgs.json";
string SchemaId = "https://cardgamesim.finoldigital.com/schema/cgs.json";
string SchemaTitle = "CGS Card Game Specification";
string SchemaDescription = "Card Game Simulator (CGS) Card Game Specification";

var settings = new JsonSchemaGeneratorSettings
{
    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
    SerializerSettings = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    }
};
var jsonSchema = JsonSchema.FromType<CardGame>(settings);
jsonSchema.Id = SchemaId;
jsonSchema.Title = SchemaTitle;
jsonSchema.Description = SchemaDescription;
// HACK: cardImageUrl uses a custom implementation of uri-template to allow for more versatility
jsonSchema.Properties["cardImageUrl"].Format = "uri-template";
File.WriteAllText(SchemaFileName, jsonSchema.ToJson());
