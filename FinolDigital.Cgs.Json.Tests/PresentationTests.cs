using Newtonsoft.Json;

namespace FinolDigital.Cgs.Json.Tests;

public class PresentationTests
{
    internal static (CardGame Game, Card Card, LocalizationTargets Targets) Create(string manifest)
    {
        var game = JsonConvert.DeserializeObject<CardGame>("{\"name\":\"Base\",\"localization\":" + manifest + "}")!;
        game.SetNameDefault = "All sets";
        game.CardNameIdentifier = "title";
        game.CardImageUrl = "https://example.invalid/{cardName}/{cardId}/{rules}.png";
        game.Enums.Add(new("element", new() {{"0x1","Fire"},{"0x2","Water"}}));
        game.Enums[0].InitializeLookups();
        var card = new Card(game,"001.SET","Flame","SET",new() {
            ["title"] = new() { Def = new("title",PropertyType.String),Value="Flame" },
            ["rules"] = new() { Def = new("rules",PropertyType.EscapedString),Value="Draw one." },
            ["stats.text"] = new() { Def = new("text",PropertyType.String),Value="Nested" },
            ["abilities[0].text"] = new() { Def = new("text",PropertyType.String),Value="First" },
            ["abilities.text"] = new() { Def = new("text",PropertyType.StringList),Value="First | Second" },
            ["element"] = new() { Def = new("element",PropertyType.StringEnum),Value="0x3 | wind" },
            ["empty"] = new() { Def = new("empty",PropertyType.String,displayEmpty:"None"),Value="" },
            ["ready"] = new() { Def = new("ready",PropertyType.Boolean),Value="yes" },
            ["notReady"] = new() { Def = new("notReady",PropertyType.Boolean),Value="no" },
            ["emptyBool"] = new() { Def = new("emptyBool",PropertyType.Boolean,displayEmpty:"N/A"),Value="" },
            ["count"] = new() { Def = new("count",PropertyType.Integer),Value="7" }
        },true);
        game.DeckUrls.Add(new("Starter","",new Uri("https://example.invalid/starter")));
        game.GamePlayZones.Add(JsonConvert.DeserializeObject<GamePlayZone>("{\"name\":\"Hand\"}")!);
        game.Extras.Add(new("Side", "count", "7"));
        game.GameBoardUrls.Add(new("Board",new Uri("https://example.invalid/board.png")));
        game.CardBackFaceImageUrls.Add(new("Back",new Uri("https://example.invalid/back.png")));
        return (game,card,new LocalizationTargets(game,new[] {card},new[] {new Set("SET","Set")}));
    }

    [Fact]
    public void PerLeafFallbackPreservesEmptyStringsAndDoesNotInferSiblings()
    {
        var (game,card,targets) = Create("""
        {"defaultLocale":"ja","locales":{
          "es-MX":{"inline":{"cards":{"001.SET":{"name":"Flama"}},"game":{"name":""}}},
          "es":{"inline":{"cards":{"001.SET":{"properties":{"rules":"Roba una."}}}}},
          "ja":{"inline":{"game":{"name":"ゲーム"},"sets":{"SET":{"name":"組"}}}},
          "pt-BR":{"inline":{"game":{"name":"Jogo"}}}}}
        """);
        var context = new LocalizationContext(targets,"ES-mx");
        Assert.Equal("",context.GameName);
        Assert.Equal("Flama",context.CardName(card));
        Assert.Equal("Roba una.",context.PropertyValue(card,"rules"));
        Assert.Equal("組",context.SetName(new("SET","Set")));
        Assert.Equal("ゲーム",new LocalizationContext(targets,"pt").GameName);
        Assert.Equal("ゲーム",new LocalizationContext(targets,"invalid_locale").GameName);
        Assert.Equal(new[] {"en-US-u-ca-gregory","en-US-u-ca","en-US","en","ja"},LocalizationLocale.FallbackChain("en-US-u-ca-gregory","ja"));
        Assert.Equal(new[] {"es-MX","es"},LocalizationLocale.FallbackChain("es-MX","ES"));
        Assert.Equal("Base",game.Name);
    }

    [Fact]
    public void ValuesUseCanonicalTypesFlagsAndFinalDisplayStrings()
    {
        var (game,card,targets) = Create("""
        {"defaultLocale":"en","locales":{"es":{"inline":{
          "cardNames":{"Flame":"Llama"},
          "cards":{"001.SET":{"properties":{"rules":"\\n literal","stats.text":"Anidado","abilities.text":"Conjunto","abilities[0].text":"Primero","count":"siete","element":"INVALID","ready":"INVALID","title":"WRONG"}}},
          "properties":{"empty":{"displayEmpty":"Vacío"},"ready":{"booleanValues":{"true":"Sí"}},"notReady":{"booleanValues":{"true":"Sí"}},"emptyBool":{"displayEmpty":"Ausente","booleanValues":{"false":"No"}}},
          "enums":{"element":{"0x1":"Fuego","wind":"Viento","0x8":"Unknown"}}
        }}}}
        """);
        var before = JsonConvert.SerializeObject(card);
        var image = card.ImageWebUrl;
        var flags = card.GetPropertyValueEnum("element");
        var context = new LocalizationContext(targets,"es");
        Assert.Equal("\\n literal",context.PropertyValue(card,"rules"));
        Assert.Equal("Anidado",context.PropertyValue(card,"stats.text"));
        Assert.Equal("Conjunto",context.PropertyValue(card,"abilities.text"));
        Assert.Equal("Primero",context.PropertyValue(card,"abilities[0].text"));
        Assert.Equal("Vacío",context.PropertyValue(card,"empty"));
        Assert.Equal("Sí",context.PropertyValue(card,"ready"));
        Assert.Equal("no",context.PropertyValue(card,"notReady"));
        Assert.Equal("Ausente",context.PropertyValue(card,"emptyBool"));
        Assert.Equal("7",context.PropertyValue(card,"count"));
        Assert.Equal("Llama",context.PropertyValue(card,"title"));
        Assert.Equal("Fuego | Water | Viento",context.PropertyValue(card,"element"));
        Assert.Equal(before,JsonConvert.SerializeObject(card));
        Assert.Equal(image,card.ImageWebUrl);
        Assert.Equal(flags,card.GetPropertyValueEnum("element"));
        Assert.Equal(2,game.Enums[0].Lookups.Count);
    }

    [Fact]
    public void NamedTargetsAndDefaultSetHaveDeterministicPrecedence()
    {
        var (game,_,targets) = Create("""
        {"defaultLocale":"en","locales":{
          "es-MX":{"inline":{"sets":{"_CGSDEFAULT_":{"name":"WRONG"}}}},
          "es":{"inline":{"game":{"setNameDefault":"Todas","gamePlayDeckName":"IGNORED"},"decks":{"Starter":{"name":"Inicio"}},"zones":{"Hand":"Mano"},"extraGroups":{"Side":"Extra"},"boards":{"Board":{"name":"Tablero"}},"cardBacks":{"Back":{"name":"Dorso"}}}}
        }}
        """);
        var context=new LocalizationContext(targets,"es-MX");
        Assert.Equal("Todas",context.SetName(new(game.SetCodeDefault,"Old")));
        Assert.Equal("Inicio",context.DeckName("Starter",true));
        Assert.Equal("Starter",context.DeckName("Starter",false));
        Assert.Equal("Custom",context.DeckName("Custom",true));
        Assert.Equal("Inicio",context.GamePlayDeckName("Inicio"));
        Assert.Equal("Mano",context.ZoneName("Hand"));
        Assert.Equal("Extra",context.ExtraGroupName("Side"));
        Assert.Equal("Tablero",context.BoardName("Board"));
        Assert.Equal("Dorso",context.CardBackName("Back"));
        Assert.Contains(context.Diagnostics,d=>d.Code=="default-set");
    }

    [Fact]
    public void ReprintsResolveWithinEachLocaleWithoutSuffixGuessing()
    {
        var (game,card,_) = Create("""
        {"defaultLocale":"en","locales":{
        "es":{"inline":{"cardNames":{"Flame":"Llama"},"cards":{"001.SET":{"name":"Especial"}}}},
        "en":{"inline":{"cards":{"001":{"name":"English exact"}}}}}}
        """);
        var other=new Card(game,"001","Flame","OTHER",card.CloneProperties(),false);
        var back=new Card(game,"001_b","Ash","OTHER",new(),false,true);
        foreach(var cards in new[] {new[] {card,other,back},new[] {back,other,card}})
        {
            var context=new LocalizationContext(new LocalizationTargets(game,cards),"es-MX");
            Assert.Equal("Especial",context.CardName(card));
            Assert.Equal("Llama",context.CardName(other));
            Assert.Equal("Ash",context.CardName(back));
        }
    }
}
