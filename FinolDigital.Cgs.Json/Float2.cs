namespace FinolDigital.Cgs.Json
{
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class Float2
    {
        [JsonProperty] public float X { get; private set; }

        [JsonProperty] public float Y { get; private set; }

        [JsonConstructor]
        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
