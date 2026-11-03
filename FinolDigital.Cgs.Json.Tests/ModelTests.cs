using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace FinolDigital.Cgs.Json.Tests;

public class ModelTests
{
    internal static readonly JsonSerializerSettings Settings = new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

    [Theory]
    [InlineData("Example")]
    [InlineData("Suma o Muere")]
    [InlineData("北斗の拳")]
    public void LegacyDefinitionsDoNotAcquireLocalization(string name)
    {
        var game = JsonConvert.DeserializeObject<CardGame>(new JObject { ["name"] = name }.ToString())!;
        Assert.Equal(name, game.Name);
        Assert.Null(game.Localization);
        Assert.Null(JObject.Parse(JsonConvert.SerializeObject(game, Settings))["localization"]);
    }

    [Fact]
    public void AllCanonicalDictionaryKeysRoundTripUnderCamelCaseResolver()
    {
        const string json = """
        {"name":"Base","localization":{"defaultLocale":"ja","locales":{"es-MX":{"inline":{
          "cardNames":{"Flame":"Llama"},"cards":{"AbC.SET":{"name":"","properties":{"Stats.Text":"Texto"}}},
          "properties":{"IsReady":{"booleanValues":{"true":"Sí","false":"No"}}},
          "enums":{"Element":{"Fire":"Fuego"}},"assets":{"cardImages":{"AbC.SET":"images/Card.png"}}
        }}}}}
        """;
        var game = JsonConvert.DeserializeObject<CardGame>(json)!;
        var result = JObject.Parse(JsonConvert.SerializeObject(game, Settings));
        Assert.True(JToken.DeepEquals(JObject.Parse(json)["localization"], result["localization"]));
        Assert.Equal("Base", game.Name);
        Assert.Equal("", game.Localization!.Locales!["es-MX"].Inline!.Cards!["AbC.SET"].Name);
        Assert.False(game.Localization.Locales["es-MX"].Inline!.Cards!.ContainsKey("abc.set"));
    }
}
