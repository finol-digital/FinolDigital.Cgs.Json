namespace FinolDigital.Cgs.Json.Tests;

public class InventoryTests
{
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
