namespace FinolDigital.Cgs.Json
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class CardBackFaceImageUrl
    {
        [JsonProperty]
        [Description("The id of the card back")]
        public string Id { get; private set; }

        [JsonProperty]
        [Description("The url from which to download the card back")]
        public Uri Url { get; private set; }

        [JsonConstructor]
        public CardBackFaceImageUrl(string id, Uri url)
        {
            Id = id ?? string.Empty;
            Url = url;
        }

        public override string ToString()
        {
            return $"{Id} at {Url}";
        }
    }
}
