using FinolDigital.Cgs.Json.Decks;
using Newtonsoft.Json;

namespace FinolDigital.Cgs.Json.Tests;

public class StarvoxelDeckTests
{
    // The Starvoxel starter deck, as shared on https://starvoxel.com/decks?deck=SVD3-000107090306020BW
    private const string StarterCode = "SVD3-000107090306020BW";
    private static readonly int[] StarterIds = { 0, 1, 7, 9, 3, 6, 2, 11 };

    private sealed class TestDeck : Deck
    {
        public TestDeck(CardGame game, IReadOnlyCollection<Card> cards) : base(game, "Starter", DeckFileType.Svd, cards) { }
    }

    private static CardGame NewGame() => new("Starvoxel") { DeckFileAltId = "deckId" };

    private static Card NewCard(CardGame game, string slug, string deckId) => new(game, slug, slug, "SET", new()
    {
        ["deckId"] = new() { Def = new("deckId", PropertyType.Integer), Value = deckId }
    }, false);

    [Fact]
    public void StarterCodeDecodesToItsCardsInOrder()
    {
        Assert.True(StarvoxelDeckEncoder.TryDecode(StarterCode, out var ids));
        Assert.Equal(StarterIds, ids);
        Assert.Equal(StarterCode, StarvoxelDeckEncoder.Encode(ids));
    }

    [Theory]
    [InlineData(new[] { 1023, 0, 32, 31, 512, 100, 7, 999 })]
    [InlineData(new[] { 39, 38, 37, 36, 35, 34, 33, 32 })]
    public void EveryValidDeckRoundTrips(int[] ids)
    {
        var code = StarvoxelDeckEncoder.Encode(ids);
        Assert.Matches("^SVD3-[0-9A-HJKMNP-TV-Z]{17}$", code);
        Assert.True(StarvoxelDeckEncoder.TryDecode(code, out var decoded));
        Assert.Equal(ids, decoded);
    }

    [Theory]
    [InlineData("  svd3-000107090306020bw  ")]
    [InlineData("SVD3-OOO1O7O9O3O6O2OBW")]
    [InlineData("SVD3-000I07090306020BW")]
    [InlineData("SVD3-000L07090306020BW")]
    [InlineData("https://starvoxel.com/decks?deck=SVD3-000107090306020BW")]
    [InlineData("https://starvoxel.com/decks?lang=en&deck=SVD3-000107090306020BW&utm_source=deck_share#builder")]
    [InlineData("https://starvoxel.com/decks?deck=SVD3%2D000107090306020BW")]
    public void DecodingAcceptsWhatStarvoxelAccepts(string text)
    {
        Assert.True(StarvoxelDeckEncoder.TryDecode(text, out var ids));
        Assert.Equal(StarterIds, ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SVD3-000107090306020B")] // Too short
    [InlineData("SVD3-000107090306020BWW")] // Too long
    [InlineData("SVD2-000107090306020BW")] // Other version
    [InlineData("SVD3-000107090306020BX")] // Wrong check symbol
    [InlineData("SVD3-010007090306020BW")] // Swapped symbols
    [InlineData("SVD3-000107090306020UW")] // U is not in the alphabet
    [InlineData("SVD3-00000000000000000")] // Same card eight times, with a valid check symbol
    [InlineData("https://starvoxel.com/redeck=SVD3-000107090306020BW")]
    public void DecodingRejectsInvalidCodes(string? text)
    {
        Assert.False(StarvoxelDeckEncoder.TryDecode(text, out var ids));
        Assert.Empty(ids);
    }

    [Theory]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6 })]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6, 6 })]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6, 1024 })]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6, -1 })]
    public void InvalidDecksCannotBeEncoded(int[] ids)
    {
        Assert.False(StarvoxelDeckEncoder.CanEncode(ids));
        Assert.Throws<ArgumentException>(() => StarvoxelDeckEncoder.Encode(ids));
    }

    [Fact]
    public void CompleteDeckExportsOnlyTheCode()
    {
        var game = NewGame();
        var cards = StarterIds.Select(id => NewCard(game, "card" + id, id.ToString())).ToList();
        var deck = new TestDeck(game, cards);
        Assert.Equal(StarterCode + Environment.NewLine, deck.ToString());
        Assert.Equal(StarterCode, deck.ToSvd().Trim());
    }

    [Fact]
    public void UnfinishedDeckKeepsItsCardsInsteadOfACode()
    {
        var game = NewGame();
        var first = NewCard(game, "pebble_panda", "0");
        var noDeckId = NewCard(game, "unreleased", "");
        var deck = new TestDeck(game, new[] { first, first, noDeckId });
        var lines = deck.ToSvd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(new[] { Deck.IncompleteSvdComment, "0", "0", "unreleased" }, lines);
        Assert.False(StarvoxelDeckEncoder.TryDecode(deck.ToSvd(), out _));
    }

    [Fact]
    public void SvdDeckFileTypeSerializesAsSvd()
    {
        var game = JsonConvert.DeserializeObject<CardGame>("""{"name":"Starvoxel","deckFileType":"svd"}""")!;
        Assert.Equal(DeckFileType.Svd, game.DeckFileType);
        Assert.Contains("\"svd\"", JsonConvert.SerializeObject(DeckFileType.Svd));
    }
}
