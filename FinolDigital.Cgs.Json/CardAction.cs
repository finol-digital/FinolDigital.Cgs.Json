namespace FinolDigital.Cgs.Json
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CardAction
    {
        [EnumMember(Value = "move")] Move,
        [EnumMember(Value = "rotate")] Rotate,
        [EnumMember(Value = "tap")] Tap,
        [EnumMember(Value = "flip")] Flip,
        [EnumMember(Value = "discard")] Discard
    }
}
