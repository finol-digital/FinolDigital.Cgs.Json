namespace FinolDigital.Cgs.Json
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardUrl
    {
        [JsonProperty]
        [Description("The id of the board")]
        public string Id { get; private set; }

        [JsonProperty]
        [Description("The url from which to download the board image")]
        public Uri Url { get; private set; }

        [JsonConstructor]
        public GameBoardUrl(string id, Uri url)
        {
            Id = id ?? string.Empty;
            Url = url;
        }
    }
}
