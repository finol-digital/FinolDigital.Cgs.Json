using FinolDigital.Cgs.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

var outputDirectory = args.Length == 0 ? "." : args[0];
Directory.CreateDirectory(outputDirectory);
var settings = new NewtonsoftJsonSchemaGeneratorSettings
{
    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
    SerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    }
};

JObject Generate(Type type, string file, string title)
{
    var schema = new JsonSchemaGenerator(settings).Generate(type);
    schema.Id = "https://cardgamesim.finoldigital.com/schema/" + file;
    schema.Title = title;
    schema.Description = title;
    return JObject.Parse(schema.ToJson());
}

// The model generator handles the existing contract; these constraints supplement the
// localization DTOs without changing nullability or URI formats of legacy fields.
void ConstrainLocalization(JObject root)
{
    var definitions = (JObject?)root["definitions"];
    var nodes = definitions?.Properties().Where(p =>
        p.Name.StartsWith("Localized", StringComparison.Ordinal) ||
        p.Name.StartsWith("Localization", StringComparison.Ordinal) || p.Name == "GameLocalization")
        .Select(p => (JObject)p.Value).ToList() ?? new List<JObject>();
    if ((string?)root["title"] == "CGS Game Localization Catalog") nodes.Add(root);
    foreach (var node in nodes)
    {
        // Optional members may be absent, but supplied null translation leaves/maps are invalid.
        foreach (var type in node.DescendantsAndSelf().OfType<JObject>().ToList())
        {
            if (type["type"] is JArray types)
            {
                var nonNull = types.Where(t => (string?)t != "null").ToArray();
                type["type"] = nonNull.Length == 1 ? nonNull[0].DeepClone() : new JArray(nonNull);
            }
            if (type["oneOf"] is JArray variants && variants.Count == 2 &&
                variants.OfType<JObject>().Any(v => (string?)v["type"] == "null"))
            {
                var nonNull = (JObject)variants.OfType<JObject>().Single(v => (string?)v["type"] != "null").DeepClone();
                type.Remove("oneOf");
                foreach (var p in nonNull.Properties()) type[p.Name] = p.Value.DeepClone();
            }
        }
    }
    if (definitions?["GameLocalization"] is JObject manifest)
        manifest["required"] = new JArray("defaultLocale");
    // URI references use ASCII (percent-encode international paths), HTTP(S) or
    // relative paths only. Filesystem roots and other schemes are never resources.
    const string resourcePattern = @"^(?:[Hh][Tt][Tt][Pp][Ss]?://(?=[^/?#])|(?!(?:[^/?#]*:|/)))(?!.*[\\{}])[!-~]+(?![\s\S])";
    if (definitions?["LocalizationSource"] is JObject source)
    {
        source["oneOf"] = new JArray(
            new JObject { ["required"] = new JArray("url") },
            new JObject { ["required"] = new JArray("inline") });
        var url = (JObject)source["properties"]!["url"]!;
        url["format"] = "uri-reference";
        url["minLength"] = 1;
        url["pattern"] = resourcePattern;
    }
    if (definitions?["LocalizedAssets"] is JObject assets)
    {
        foreach (var prop in ((JObject)assets["properties"]!).Properties())
        {
            var value = (JObject)prop.Value;
            if (value["$ref"] != null)
            {
                value.Remove("$ref");
                value["type"] = "object";
                var item = new JObject { ["type"] = "string" };
                value["additionalProperties"] = item;
                value = item;
            }
            value["format"] = "uri-reference";
            value["minLength"] = 1;
            value["pattern"] = resourcePattern;
        }
    }
}

var game = Generate(typeof(CardGame), "cgs.json", "CGS Card Game Specification");
game["description"] = "Card Game Simulator (CGS) Card Game Specification";
game["properties"]!["cardImageUrl"]!["format"] = "uri-template";
ConstrainLocalization(game);
File.WriteAllText(Path.Combine(outputDirectory, "cgs.json"), game.ToString(Formatting.Indented) + Environment.NewLine);
var catalog = Generate(typeof(LocalizationCatalog), "GameLocalization.json", "CGS Game Localization Catalog");
ConstrainLocalization(catalog);
File.WriteAllText(Path.Combine(outputDirectory, "GameLocalization.json"), catalog.ToString(Formatting.Indented) + Environment.NewLine);
