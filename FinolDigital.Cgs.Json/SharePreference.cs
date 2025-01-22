namespace FinolDigital.Cgs.Json
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SharePreference
    {
        [EnumMember(Value = "ask")] Ask,
        [EnumMember(Value = "share")] Share,
        [EnumMember(Value = "individual")] Individual
    }
}
