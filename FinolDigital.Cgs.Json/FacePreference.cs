namespace FinolDigital.Cgs.Json
{
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json;
    using System.Runtime.Serialization;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FacePreference
    {
        [EnumMember(Value = "any")] Any,
        [EnumMember(Value = "down")] Down,
        [EnumMember(Value = "up")] Up
    }
}