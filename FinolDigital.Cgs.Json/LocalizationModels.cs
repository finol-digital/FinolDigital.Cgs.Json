namespace FinolDigital.Cgs.Json
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>Canonical catalog keys must never follow a serializer's property casing policy.</summary>
    [JsonDictionary(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public sealed class LocalizationMap<T> : Dictionary<string, T>
    {
        public LocalizationMap() : base(StringComparer.Ordinal) { }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class GameLocalization
    {
        [JsonIgnore] public bool IsUsable { get; internal set; } = true;
        [JsonIgnore] public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; internal set; } = Array.Empty<LocalizationDiagnostic>();
        [JsonProperty("defaultLocale")] public string DefaultLocale { get; set; } = "";
        [JsonProperty("locales", NullValueHandling = NullValueHandling.Ignore)]
        public LocalizationMap<LocalizationSource>? Locales { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizationSource
    {
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)] public string? Url { get; set; }
        [JsonProperty("inline", NullValueHandling = NullValueHandling.Ignore)] public LocalizationCatalog? Inline { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizationCatalog
    {
        [JsonProperty("game", NullValueHandling = NullValueHandling.Ignore)] public LocalizedGame? Game { get; set; }
        [JsonProperty("cards", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedCard>? Cards { get; set; }
        [JsonProperty("cardNames", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? CardNames { get; set; }
        [JsonProperty("sets", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedName>? Sets { get; set; }
        [JsonProperty("decks", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedName>? Decks { get; set; }
        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedProperty>? Properties { get; set; }
        [JsonProperty("enums", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizationMap<string>>? Enums { get; set; }
        [JsonProperty("extraGroups", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? ExtraGroups { get; set; }
        [JsonProperty("zones", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? Zones { get; set; }
        [JsonProperty("boards", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedName>? Boards { get; set; }
        [JsonProperty("cardBacks", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<LocalizedName>? CardBacks { get; set; }
        [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)] public LocalizedAssets? Assets { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizedGame
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)] public string? Name { get; set; }
        [JsonProperty("copyright", NullValueHandling = NullValueHandling.Ignore)] public string? Copyright { get; set; }
        [JsonProperty("gamePlayDeckName", NullValueHandling = NullValueHandling.Ignore)] public string? GamePlayDeckName { get; set; }
        [JsonProperty("setNameDefault", NullValueHandling = NullValueHandling.Ignore)] public string? SetNameDefault { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LocalizedName
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)] public string? Name { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizedCard
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)] public string? Name { get; set; }
        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? Properties { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizedProperty
    {
        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)] public string? Display { get; set; }
        [JsonProperty("displayEmpty", NullValueHandling = NullValueHandling.Ignore)] public string? DisplayEmpty { get; set; }
        [JsonProperty("booleanValues", NullValueHandling = NullValueHandling.Ignore)] public LocalizedBoolean? BooleanValues { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizedBoolean
    {
        [JsonProperty("true", NullValueHandling = NullValueHandling.Ignore)] public string? True { get; set; }
        [JsonProperty("false", NullValueHandling = NullValueHandling.Ignore)] public string? False { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class LocalizedAssets
    {
        [JsonProperty("cardImages", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? CardImages { get; set; }
        [JsonProperty("cardBacks", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? CardBacks { get; set; }
        [JsonProperty("boards", NullValueHandling = NullValueHandling.Ignore)] public LocalizationMap<string>? Boards { get; set; }
        [JsonProperty("bannerImageUrl", NullValueHandling = NullValueHandling.Ignore)] public string? BannerImageUrl { get; set; }
        [JsonProperty("cardBackImageUrl", NullValueHandling = NullValueHandling.Ignore)] public string? CardBackImageUrl { get; set; }
        [JsonProperty("playMatImageUrl", NullValueHandling = NullValueHandling.Ignore)] public string? PlayMatImageUrl { get; set; }
        [JsonProperty("rulesUrl", NullValueHandling = NullValueHandling.Ignore)] public string? RulesUrl { get; set; }
    }
}
