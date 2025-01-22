namespace FinolDigital.Cgs.Json
{
    using Newtonsoft.Json;
    using System.ComponentModel;

    [JsonObject(MemberSerialization.OptIn)]
    public class GamePlayZone
    {
        [JsonProperty]
        [Description("When a Card enters the Game Play Zone, the Card will display this face")]
        [DefaultValue("any")]
        public FacePreference Face { get; private set; } = FacePreference.Any;

        [JsonProperty]
        [Description(
            "If possible, CGS will take the defaultCardAction when a Card is double-clicked in the Game Play Zone. If null, defaults to <gameDefaulCardAction>")]
        public CardAction? DefaultCardAction { get; set; } = null;

        [JsonProperty]
        [Description("Indicates the Game Play Zone's position in inches")]
        public Float2 Position { get; private set; }

        [JsonProperty]
        [Description("Indicates the Game Play Zone's rotation in degrees")]
        public int Rotation { get; private set; }

        [JsonProperty]
        [Description("Indicates the Game Play Zone's width and height in inches")]
        public Float2 Size { get; private set; }

        [JsonProperty]
        [Description("The Game Play Zone type from area, horizontal, or vertical")]
        [DefaultValue("area")]
        public GamePlayZoneType Type { get; private set; } = GamePlayZoneType.Area;

        [JsonConstructor]
        public GamePlayZone(FacePreference face, CardAction? defaultCardAction, Float2 position, int rotation, Float2 size, GamePlayZoneType type)
        {
            Face = face;
            DefaultCardAction = defaultCardAction;
            Position = position;
            Rotation = rotation;
            Size = size;
            Type = type;
        }
    }
}