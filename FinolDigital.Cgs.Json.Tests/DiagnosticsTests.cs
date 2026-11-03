using Newtonsoft.Json;

namespace FinolDigital.Cgs.Json.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void InvalidLeavesKeepValidSiblingsWithoutCoercion()
    {
        var result = LocalizationJson.ReadCatalog("""{"cards":{"AbC":{"name":"Nombre","properties":{"Rules":null,"Other":7}}}}""");
        Assert.NotNull(result.Value);
        Assert.False(result.IsValid);
        Assert.Equal("Nombre", result.Value.Cards!["AbC"].Name);
        Assert.Empty(result.Value.Cards["AbC"].Properties!);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Null(LocalizationJson.ReadCatalog("""{"cards":[]} """).Value);
        Assert.Null(LocalizationJson.ReadCatalog("""{"game":{"name":"a","name":"b"}}""").Value);
    }

    [Fact]
    public void DateLikeStringsRemainTextAndDuplicateManifestMembersFallBack()
    {
        Assert.Equal("2026-01-01T00:00:00Z",LocalizationJson.ReadCatalog("{\"game\":{\"name\":\"2026-01-01T00:00:00Z\"}}").Value!.Game!.Name);
        var game=JsonConvert.DeserializeObject<CardGame>("{\"name\":\"Base\",\"localization\":{\"defaultLocale\":\"en\",\"defaultLocale\":\"es\"},\"copyright\":\"Owner\"}")!;
        Assert.False(game.Localization!.IsUsable);
        Assert.Equal("Owner",game.Copyright);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("""{"defaultLocale":"bad_tag"}""")]
    [InlineData("""{"defaultLocale":"en","locales":{"es":{"inline":{}},"ES":{"inline":{}}}}""")]
    [InlineData("""{"defaultLocale":"en","locales":{"es":{}}}""")]
    [InlineData("""{"defaultLocale":"en","locales":{"es":{"url":"file:///etc/passwd"}}}""")]
    public void InvalidManifestDoesNotPreventBaseGameDeserialization(string manifest)
    {
        var game = JsonConvert.DeserializeObject<CardGame>("{\"name\":\"Canonical\",\"localization\":" + manifest + "}")!;
        Assert.Equal("Canonical", game.Name);
        Assert.NotNull(game.Localization);
        Assert.False(game.Localization.IsUsable);
        Assert.NotEmpty(game.Localization.Diagnostics);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es-MX")]
    [InlineData("zh-Hant-TW")]
    [InlineData("en-US-u-ca-gregory")]
    [InlineData("x-custom")]
    [InlineData("i-klingon")]
    [InlineData("de-CH-1901")]
    public void WellFormedTagsAreAccepted(string tag) => Assert.True(LocalizationLocale.IsValid(tag));

    [Theory]
    [InlineData("en_US")]
    [InlineData("en-u")]
    [InlineData("en-abcde-abcde")]
    [InlineData("en-u-ca-u-nu")]
    [InlineData("en\n")]
    [InlineData("")]
    public void MalformedTagsAreRejected(string tag) => Assert.False(LocalizationLocale.IsValid(tag));

    [Fact]
    public void TargetsDiagnoseUnknownAndTypedOverridesWithoutAllocatingEnumFlags()
    {
        var game = new CardGame("Base") { CardProperties = new() {new("ready",PropertyType.Boolean),new("count",PropertyType.Integer)}, Enums = new() {new("element", new() {{"fire", "Fire"}})} };
        game.Enums[0].InitializeLookups();
        var cards = new[] {new Card(game,"AbC","Flame","S",new() {
            ["element"] = new() {Def = new("element",PropertyType.StringEnum),Value="wind"},
            ["count"] = new() {Def = new("count",PropertyType.Integer),Value="5"}},false)};
        var targets = new LocalizationTargets(game,cards);
        var catalog = LocalizationJson.ReadCatalog("""
        {"cards":{"AbC":{"properties":{"count":"cinco"}},"unknown":{"name":"x"}},
         "sets":{"_CGSDEFAULT_":{"name":"conflict"}},"enums":{"element":{"wind":"Viento","unseen":"?"}},
         "properties":{"ready":{"display":"Same"},"count":{"display":"Same"}}}
        """).Value!;
        var diagnostics = targets.Validate(catalog);
        Assert.Contains(diagnostics, d => d.Path == "/cards/AbC/properties/count");
        Assert.Contains(diagnostics, d => d.Code == "default-set");
        Assert.Contains(diagnostics, d => d.Code == "duplicate-label" && d.IsWarning);
        Assert.DoesNotContain(diagnostics, d => d.Path == "/enums/element/wind");
        Assert.Contains(diagnostics, d => d.Path == "/enums/element/unseen");
        Assert.Single(game.Enums[0].Lookups);
        Assert.False(game.Enums[0].Lookups.ContainsKey("wind"));
    }
}
