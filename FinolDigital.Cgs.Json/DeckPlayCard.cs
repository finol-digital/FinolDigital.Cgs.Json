namespace FinolDigital.Cgs.Json
{
    using Newtonsoft.Json;
    using System.ComponentModel;

    [JsonObject(MemberSerialization.OptIn)]
    public class DeckPlayCard
    {
        [JsonProperty]
        [Description(
            "CGS will automatically play the Card that matches cardQuery, when the player loads a Deck in Play Mode matching deckQuery")]
        public string CardQuery { get; private set; }


        [JsonProperty]
        [Description(
            "CGS will automatically play the Card that matches cardQuery, when the player loads a Deck in Play Mode matching deckQuery")]
        public string DeckQuery { get; private set; }


        [JsonProperty]
        [Description("Indicates the played Card's position in inches")]
        public Float2 Position { get; private set; }

        [JsonProperty]
        [Description("Indicates the played Card's rotation in degrees")]
        public int Rotation { get; private set; }

        [JsonConstructor]
        public DeckPlayCard(string cardQuery, string deckQuery, Float2 position, int rotation)
        {
            CardQuery = cardQuery;
            DeckQuery = deckQuery;
            Position = position;
            Rotation = rotation;
        }
    }
}
