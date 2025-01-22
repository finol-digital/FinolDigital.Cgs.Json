namespace FinolDigital.Cgs.Json
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardCard
    {
        [JsonProperty]
        [Description(
            "When a deck is loaded in Play Mode, any card with *Card:Id* = *Card* will cause *Boards* to be put into the play area.")]
        public string Card { get; private set; }

        [JsonProperty] public List<GameBoard> Boards { get; private set; }

        [JsonConstructor]
        public GameBoardCard(string card, List<GameBoard> boards)
        {
            Card = card ?? string.Empty;
            Boards = boards ?? new List<GameBoard>();
        }
    }
}
