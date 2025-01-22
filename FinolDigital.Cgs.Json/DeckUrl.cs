namespace FinolDigital.Cgs.Json
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class DeckUrl
    {
        [JsonProperty]
        [Description("The name of the deck")]
        public string Name { get; private set; }

        [JsonProperty]
        [Description(
            "An optional path that can be used to override the url. See @cgs.json#/properties/allDecksUrlTxtRoot")]
        public string Txt { get; private set; }

        [JsonProperty]
        [Description("The url from which to download the deck")]
        public Uri Url { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [Description("Optionally set to false to ignore this deck url")]
        [DefaultValue("true")]
        public bool IsAvailable { get; private set; } = true;

        [JsonConstructor]
        public DeckUrl(string name, string txt, Uri url, bool isAvailable = true)
        {
            Name = name ?? string.Empty;
            Txt = txt ?? string.Empty;
            Url = url;
            if (!isAvailable)
                IsAvailable = false;
        }

        public override string ToString()
        {
            return $"{Name} at {Url}/{Txt} is {IsAvailable}";
        }
    }
}
