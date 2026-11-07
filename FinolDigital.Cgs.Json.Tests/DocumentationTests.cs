namespace FinolDigital.Cgs.Json.Tests;

public class DocumentationTests
{
    [Fact]
    public void DocumentedExampleUsesExplicitContextWithoutMutatingCard()
    {
        var game = new CardGame("Example")
        {
            Localization = LocalizationJson.ReadManifest(
                "{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"inline\":{\"cardNames\":{\"Flame\":\"Llama\"}}}}}").Value
        };
        var card = new Card(game, "001", "Flame", "SET",
            new Dictionary<string, PropertyDefValuePair>(), false);
        var targets = new LocalizationTargets(game, new[] { card });
        var display = new LocalizationContext(targets, "es-MX");
        var label = display.CardName(card);
        var canonical = card.Name;
        Assert.Equal("Llama",label);
        Assert.Equal("Flame",canonical);
        Assert.Equal("Flame",new LocalizationContext(targets,"ja").CardName(card));
    }
}
