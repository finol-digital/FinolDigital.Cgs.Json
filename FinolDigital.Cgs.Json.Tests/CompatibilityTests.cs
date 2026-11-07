using FinolDigital.Cgs.Json.Decks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FinolDigital.Cgs.Json.Tests;

internal static class TestFiles
{
    public static string Read(string relative)
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null && !File.Exists(Path.Combine(dir.FullName,"FinolDigital.Cgs.Json.sln"))) dir=dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName,relative));
    }
}

public class CompatibilityTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("ja")]
    public void AuthoredLanguageFixturesKeepCanonicalDataWithOrWithoutMetadata(string language)
    {
        var prefix=$"FinolDigital.Cgs.Json.Tests/Fixtures/{language}/";
        var legacy=JsonConvert.DeserializeObject<CardGame>(TestFiles.Read(prefix+"cgs.json"))!;
        var localized=JsonConvert.DeserializeObject<CardGame>(TestFiles.Read(prefix+"localized.json"))!;
        var data=(JObject)JArray.Parse(TestFiles.Read(prefix+"AllCards.json"))[0];
        Card Load(CardGame game) => new(game,(string)data[game.CardIdIdentifier]!, (string)data[game.CardNameIdentifier]!,"BASE",
            new() { ["rules"]=new() {Def=game.CardProperties[0],Value=(string)data["rules"]!}},false);
        var baseCard=Load(legacy);
        var card=Load(localized);
        Assert.Equal(JsonConvert.SerializeObject(baseCard),JsonConvert.SerializeObject(card));
        var gameBefore=JsonConvert.SerializeObject(localized,ModelTests.Settings);
        foreach(var locale in new[] {"en","es","ja","fr","unknown"})
        {
            var originalDisplay=new LocalizationContext(new LocalizationTargets(legacy,new[] {baseCard}),locale);
            Assert.Empty(originalDisplay.LocaleChain);
            Assert.Equal(legacy.Name,originalDisplay.GameName);
            Assert.Equal(baseCard.Name,originalDisplay.CardName(baseCard));
            Assert.Empty(originalDisplay.AssetCandidates("cardImages",baseCard.Id));
            var display=new LocalizationContext(new LocalizationTargets(localized,new[] {card}),locale);
            Assert.Equal(locale=="fr"?"Translated":localized.Name,display.GameName);
            Assert.Equal(baseCard.GetPropertyValueString("rules"),display.PropertyValue(card,"rules"));
            Assert.Equal(baseCard.ImageWebUrl,card.ImageWebUrl);
            Assert.Equal(gameBefore,JsonConvert.SerializeObject(localized,ModelTests.Settings));
        }
        Assert.Null(JObject.Parse(JsonConvert.SerializeObject(legacy,ModelTests.Settings))["localization"]);
    }

    private sealed class TestDeck : Deck
    {
        public TestDeck(CardGame game, DeckFileType type, IReadOnlyCollection<Card> cards) : base(game,"Starter",type,cards) { }
    }

    [Theory]
    [InlineData(DeckFileType.Dec)]
    [InlineData(DeckFileType.Hsd)]
    [InlineData(DeckFileType.Lor)]
    [InlineData(DeckFileType.Txt)]
    [InlineData(DeckFileType.Ydk)]
    public void EveryDeckExportAndCanonicalQueryIsIdenticalAcrossLocales(DeckFileType type)
    {
        var game=new CardGame("Base") { DeckFileAltId="dbId", CardNameIsUnique=false, DeckFileTxtId=DeckFileTxtId.Id,
            CardImageUrl="https://example.invalid/{cardName}/{cardId}/{stats.text}.png" };
        game.Extras.Add(new("Side","extra","yes"));
        Card Create(string id,string name,string dbId,bool extra,bool reprint) => new(game,id,name,"SET",new() {
            ["dbId"]=new() {Def=new("dbId",PropertyType.Integer),Value=dbId},
            ["extra"]=new() {Def=new("extra",PropertyType.Boolean),Value=extra?"yes":"no"},
            ["stats.text"]=new() {Def=new("text",PropertyType.String),Value="canonical"}
        },reprint);
        var first=Create("01DE001","Flame","1",false,false);
        var reprint=Create("01DE002","Flame","2",false,true);
        var extra=Create("01FR001","Ash","3",true,false);
        var cards=new[] {first,first,reprint,extra};
        var deck=new TestDeck(game,type,cards);
        var baseline=deck.ToString();
        var image=first.ImageWebUrl;
        var baselineSetId=game.Id;
        game.Localization=LocalizationJson.ReadManifest("""
        {"defaultLocale":"en","locales":{"es":{"inline":{
          "game":{"name":"Juego"},"cardNames":{"Flame":"Ash","Ash":"Ceniza"},
          "decks":{"Starter":{"name":"Inicio"}},"extraGroups":{"Side":"Extra"},
          "cards":{"01DE001":{"properties":{"stats.text":"traducido"}}}
        }}}}
        """).Value;
        var spanishContext=new LocalizationContext(new LocalizationTargets(game,cards,authoredDeckNames:new[] {"Starter"}),"es");
        Assert.Equal("Ash",spanishContext.CardName(first));
        foreach(var locale in new[] {"es","ja","en","es-MX"})
        {
            var context=new LocalizationContext(new LocalizationTargets(game,cards,authoredDeckNames:new[] {"Starter"}),locale);
            foreach(var card in cards) { _=context.CardName(card); _=context.PropertyValue(card,"stats.text"); }
            Assert.Equal(baseline,deck.ToString());
            Assert.Equal(image,first.ImageWebUrl);
            Assert.Equal(baselineSetId,game.Id);
            Assert.Equal(new[] {extra},deck.GetExtraCards());
            Assert.True(extra.GetPropertyValueBool("extra"));
            Assert.Equal(2,reprint.GetPropertyValueInt("dbId"));
            Assert.True(reprint.IsReprint);
            Assert.False(first.IsReprint);
            // The translated Flame -> Ash collision cannot change canonical name matching.
            Assert.Same(extra,cards.Single(c=>c.Name=="Ash"));
            Assert.Equal("canonical",first.GetPropertyValueString("stats.text"));
        }
        if(type==DeckFileType.Lor)
        {
            var parsed=LoRDeckEncoder.GetDeckFromCode(deck.ToLor().Trim());
            Assert.Equal(2,parsed.Single(c=>c.CardCode==first.Id).Count);
            Assert.Equal(1,parsed.Single(c=>c.CardCode==extra.Id).Count);
        }
        if(type==DeckFileType.Txt)
        {
            Assert.Contains("[01DE002] Flame",baseline);
            game.DeckFileTxtId=DeckFileTxtId.Set;
            Assert.Contains("Flame (SET)",deck.ToTxt());
        }
    }

    [Fact]
    public void MaintainedExampleResolvesActualCardProperties()
    {
        var game=JsonConvert.DeserializeObject<CardGame>(TestFiles.Read("examples/localization/cgs.json"))!;
        foreach(var def in game.Enums) def.InitializeLookups();
        var data=JArray.Parse(TestFiles.Read("examples/localization/AllCards.json"));
        var cards=data.Cast<JObject>().Select(c=>new Card(game,(string)c["id"]!,(string)c["name"]!,(string)c["set"]!,
            game.CardProperties.ToDictionary(p=>p.Name,p=>new PropertyDefValuePair {Def=p,Value=(string?)c[p.Name]??""}),false)).ToArray();
        var targets=new LocalizationTargets(game,cards,new[] {new Set("BASE","Base"),new Set("SECOND","Second")});
        var external=LocalizationJson.ReadCatalog(TestFiles.Read("examples/localization/locales/es.json"));
        Assert.True(external.IsValid);
        Assert.Empty(targets.Validate(external.Value!));
        var context=new LocalizationContext(targets,"es",new Dictionary<string,LocalizationCatalogDocument> { ["es"]=new(external.Value!) });
        Assert.Equal("Llama",context.CardName(cards[0]));
        Assert.Equal("Llama",context.CardName(cards[2]));
        Assert.Equal("Sin reglas",context.PropertyValue(cards[1],"rules"));
        Assert.Equal("Sí",context.PropertyValue(cards[0],"ready"));
        Assert.Equal("Sí",context.PropertyValue(cards[2],"ready"));
        Assert.Equal("No",context.PropertyValue(cards[1],"ready"));
    }
}
