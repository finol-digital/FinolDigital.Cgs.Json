namespace FinolDigital.Cgs.Json
{
    using System.ComponentModel;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class ExtraDef
    {
        public const string DefaultExtraGroup = "Extras";

        [JsonProperty]
        [Description("A group of extra cards is displayed together with this label in Play Mode")]
        [DefaultValue("Extras")]
        public string Group { get; private set; }

        [JsonProperty]
        [Description("Refers to a *Property:Name* in <cardProperties>")]
        public string Property { get; private set; }

        [JsonProperty]
        [Description(
            "If *Card:Properties[ExtraDef:Property]* equals *ExtraDef:Value*, then that card will be moved from the main deck to this extra deck")]
        public string Value { get; private set; }

        [JsonConstructor]
        public ExtraDef(string group, string property, string value)
        {
            Group = group ?? DefaultExtraGroup;
            Property = property ?? string.Empty;
            Value = value ?? string.Empty;
        }
    }
}
