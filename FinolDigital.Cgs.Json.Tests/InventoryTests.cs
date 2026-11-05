namespace FinolDigital.Cgs.Json.Tests;

public class InventoryTests
{
    [Fact]
    public void ExactTranslationsOfEveryPrintingCoverTheSharedNameTarget()
    {
        var game = new CardGame("Base");
        var cards = new[] {
            new Card(game,"1","Flame","S",new(),false),
            new Card(game,"2","Flame","T",new(),false),
            new Card(game,"3","Ash","S",new(),false)
        };
        var targets = new LocalizationTargets(game, cards);
        var partial = LocalizationJson.ReadCatalog("""{"cards":{"1":{"name":"Llama"}}}""").Value!;
        Assert.Contains("/cardNames/Flame", targets.Coverage(partial).Missing);
        var complete = LocalizationJson.ReadCatalog("""{"cards":{"1":{"name":"Llama"},"2":{"name":""},"3":{"name":"Ceniza"}}}""").Value!;
        var missing = targets.Coverage(complete).Missing;
        Assert.DoesNotContain(missing, path => path.StartsWith("/cardNames/",StringComparison.Ordinal));
        Assert.DoesNotContain(missing, path => path.StartsWith("/cards/",StringComparison.Ordinal) && path.EndsWith("/name",StringComparison.Ordinal));
    }

    [Fact]
    public void ResolvedInventoryIncludesActualFaceAndNestedPathsAndReportsCoverage()
    {
        var (game,card,_)=PresentationTests.Create("""{"defaultLocale":"en"}""");
        var face=new Card(game,"Custom-ID.backId","Ash","SET",new() {
            ["blocks[3].text"]=new() {Def=new("text",PropertyType.String),Value="Face text"}
        },false,true);
        var targets=new LocalizationTargets(game,new[] {card,face},new[] {new Set("Set/With~Punctuation","Name")},new[] {"From AllDecks"}, boardIds:new[] {"LocalBoard"},cardBackIds:new[] {"LocalBack"});
        Assert.Contains("/cards/Custom-ID.backId/name",targets.Leaves.Keys);
        Assert.Contains("/cards/Custom-ID.backId/properties/blocks[3].text",targets.Leaves.Keys);
        Assert.Contains("/cards/001.SET/properties/abilities.text",targets.Leaves.Keys);
        Assert.Contains("/cards/001.SET/properties/abilities[0].text",targets.Leaves.Keys);
        Assert.Contains("/boards/LocalBoard/name",targets.Leaves.Keys);
        Assert.Contains("/cardBacks/LocalBack/name",targets.Leaves.Keys);
        Assert.Contains("/cardNames/Ash",targets.Leaves.Keys);
        Assert.Contains("/sets/Set~1With~0Punctuation/name",targets.Leaves.Keys);
        Assert.Contains("/decks/From AllDecks/name",targets.Leaves.Keys);
        var catalog=LocalizationJson.ReadCatalog("""{"cardNames":{"Flame":"Llama"},"cards":{"unknown":{"name":"?"}}}""").Value!;
        var coverage=targets.Coverage(catalog);
        Assert.DoesNotContain("/cards/001.SET/name",coverage.Missing);
        Assert.Contains("/cards/Custom-ID.backId/name",coverage.Missing);
        Assert.Contains("/cards/unknown/name",coverage.Unknown);
        Assert.Equal(targets.Leaves.Count,coverage.Total);
    }
}
