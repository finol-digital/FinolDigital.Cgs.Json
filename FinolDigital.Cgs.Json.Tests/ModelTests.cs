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

    [Theory]
    [InlineData("42", false)]
    [InlineData("""{"defaultLocale":"bad_tag","locales":{"es":{"inline":{"game":{"name":"Juego"}}}}}""", false)]
    [InlineData("""{"defaultLocale":"en","future":{"version":2},"locales":{"es":{"inline":{"game":{"name":"Juego","future":"2026-01-01T00:00:00Z"}}}}}""", true)]
    [InlineData("""{"defaultLocale":"en","locales":{"es":{"inline":{"game":{"name":42,"copyright":"Owner"}}}}}""", true)]
    public void UnchangedManifestPreservesAuthoredMetadata(string manifest, bool usable)
    {
        var game = JsonConvert.DeserializeObject<CardGame>("{\"name\":\"Base\",\"localization\":" + manifest + "}")!;
        Assert.Equal(usable, game.Localization!.IsUsable);
        Assert.NotEmpty(game.Localization.Diagnostics);
        game.Name = "Edited base name";
        var result = JObject.Parse(JsonConvert.SerializeObject(game, Settings));
        Assert.True(JToken.DeepEquals(JToken.Parse(manifest), result["localization"]));
        Assert.Equal("Edited base name", result["name"]);
    }

    [Fact]
    public void DuplicateMembersSurviveWriteAndRemainUnusableOnReload()
    {
        const string manifest = """{"defaultLocale":"en","defaultLocale":"es"}""";
        var game = JsonConvert.DeserializeObject<CardGame>("{\"name\":\"Base\",\"localization\":" + manifest + "}")!;
        var written = JsonConvert.SerializeObject(game, Settings);
        Assert.Contains(manifest, written);
        Assert.False(JsonConvert.DeserializeObject<CardGame>(written)!.Localization!.IsUsable);
    }

    [Fact]
    public void EditingNestedLocalizationWritesTheEditedModel()
    {
        var game = JsonConvert.DeserializeObject<CardGame>("""
        {"name":"Base","localization":{"defaultLocale":"en","future":true,
          "locales":{"es":{"inline":{"cardNames":{"Flame":"Llama"}}}}}}
        """)!;
        game.Localization!.Locales!["es"].Inline!.CardNames!["Flame"] = "Fuego";
        var written = JObject.Parse(JsonConvert.SerializeObject(game, Settings));
        Assert.Equal("Fuego", written["localization"]!["locales"]!["es"]!["inline"]!["cardNames"]!["Flame"]);
        Assert.Null(written["localization"]!["future"]);
    }

    [Fact]
    public void InvalidManifestCanBeRepairedReplacedOrRemoved()
    {
        var game = JsonConvert.DeserializeObject<CardGame>("""{"name":"Base","localization":42}""")!;
        game.Localization!.DefaultLocale = "es";
        var written = JsonConvert.SerializeObject(game, Settings);
        Assert.True(JsonConvert.DeserializeObject<CardGame>(written)!.Localization!.IsUsable);
        game.Localization = new GameLocalization { DefaultLocale = "ja" };
        Assert.Equal("ja", JObject.Parse(JsonConvert.SerializeObject(game, Settings))["localization"]!["defaultLocale"]);
        game.Localization = null;
        Assert.Null(JObject.Parse(JsonConvert.SerializeObject(game, Settings))["localization"]);
    }
}
