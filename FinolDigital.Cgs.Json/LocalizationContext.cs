namespace FinolDigital.Cgs.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public sealed class LocalizationCatalogDocument
    {
        public LocalizationCatalog Catalog { get; }
        public LocalizationOrigin? Origin { get; }
        public LocalizationCatalogDocument(LocalizationCatalog catalog, LocalizationOrigin? origin = null)
        { Catalog = catalog; Origin = origin; }
    }

    /// <summary>A presentation snapshot. Recreate after canonical data, locale, manifest or catalogs change.</summary>
    public sealed class LocalizationContext
    {
        private sealed class Binding
        {
            public string Locale = "";
            public Dictionary<string, string> Leaves = new Dictionary<string, string>();
            public LocalizationOrigin? Origin;
        }
        private readonly List<Binding> _bindings = new List<Binding>();
        private readonly LocalizationTargets _targets;
        public IReadOnlyList<string> LocaleChain { get; }
        public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; }
        private static string P(params string[] parts) => LocalizationJson.Pointer(parts);

        public LocalizationContext(LocalizationTargets targets, string? locale,
            IReadOnlyDictionary<string, LocalizationCatalogDocument>? externalCatalogs = null,
            LocalizationOrigin? definitionOrigin = null)
        {
            _targets = targets;
            var diagnostics = new List<LocalizationDiagnostic>();
            Diagnostics = diagnostics.AsReadOnly();
            LocaleChain = Array.Empty<string>();
            var manifest = targets.Game.Localization;
            if (manifest == null) return;
            diagnostics.AddRange(manifest.Diagnostics);
            LocalizationJson.ValidateManifest(manifest, diagnostics);
            if (!manifest.IsUsable || diagnostics.Any(d => d.Code == "manifest")) return;
            LocaleChain = LocalizationLocale.FallbackChain(locale, manifest.DefaultLocale);
            foreach (var candidate in LocaleChain)
            {
                var source = manifest.Locales?.FirstOrDefault(p => p.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase)).Value;
                if (source == null) continue;
                var catalog = source.Inline;
                var origin = definitionOrigin;
                if (source.Url != null)
                {
                    var documents = externalCatalogs?.Where(p => p.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (documents == null || documents.Length == 0) continue;
                    if (documents.Length > 1)
                    {
                        diagnostics.Add(new LocalizationDiagnostic("duplicate-catalog", P(candidate), "Ambiguous supplied catalog locale; ignored."));
                        continue;
                    }
                    catalog = documents[0].Value.Catalog;
                    origin = documents[0].Value.Origin;
                }
                if (catalog == null) continue;
                var parsed = LocalizationJson.ReadCatalog(JsonConvert.SerializeObject(catalog));
                diagnostics.AddRange(parsed.Diagnostics);
                if (parsed.Value == null) continue;
                diagnostics.AddRange(targets.Validate(parsed.Value));
                var leaves = LocalizationJson.Leaves(parsed.Value).Where(p => targets.Contains(p.Key) &&
                    (!p.Key.StartsWith("/assets/", StringComparison.Ordinal) || LocalizationJson.IsResourceReference(p.Value)))
                    .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
                _bindings.Add(new Binding {Locale = candidate, Leaves = leaves, Origin = origin});
            }
        }

        private string Resolve(string canonical, params string[] path)
        {
            var key = P(path);
            foreach (var binding in _bindings)
                if (binding.Leaves.TryGetValue(key, out var translated)) return translated;
            return canonical;
        }

        public string GameName => Resolve(_targets.Game.Name, "game", "name");
        public string Copyright => Resolve(_targets.Game.Copyright, "game", "copyright");
        public string GamePlayDeckName(string resolvedDeckName) => string.IsNullOrEmpty(_targets.Game.GamePlayDeckName)
            ? resolvedDeckName : Resolve(_targets.Game.GamePlayDeckName, "game", "gamePlayDeckName");
        public string CardName(Card card)
        {
            foreach (var binding in _bindings)
            {
                if (binding.Leaves.TryGetValue(P("cards", card.Id, "name"), out var exact)) return exact;
                if (binding.Leaves.TryGetValue(P("cardNames", card.Name), out var shared)) return shared;
            }
            return card.Name;
        }
        public string SetName(Set set) => set.Code == _targets.Game.SetCodeDefault
            ? Resolve(_targets.Game.SetNameDefault, "game", "setNameDefault") : Resolve(set.Name, "sets", set.Code, "name");
        public string DeckName(string name, bool hasAuthoredProvenance) => hasAuthoredProvenance ? Resolve(name, "decks", name, "name") : name;
        public string ZoneName(string name) => Resolve(name, "zones", name);
        public string ExtraGroupName(string name) => Resolve(name, "extraGroups", name);
        public string BoardName(string id) => Resolve(id, "boards", id, "name");
        public string CardBackName(string id) => Resolve(id, "cardBacks", id, "name");
        public string PropertyLabel(string path, string canonicalLabel) => Resolve(canonicalLabel, "properties", path, "display");
        public string EmptyLabel(string path, string canonicalLabel) => Resolve(canonicalLabel, "properties", path, "displayEmpty");
        public string EnumLabel(string path, string token, string canonicalLabel) => Resolve(canonicalLabel, "enums", path, token);

        public string PropertyValue(Card card, string path)
        {
            var legacy = card.GetPropertyValueString(path);
            if (!_targets.CardProperties.TryGetValue(card.Id, out var properties) || !properties.TryGetValue(path, out var property)) return legacy;
            if (string.IsNullOrEmpty(property.Value)) return EmptyLabel(path, legacy);
            if (_targets.Enums.TryGetValue(path, out var enumDef))
                return string.Join(EnumDef.Delimiter, property.Value.Split(new[] {EnumDef.Delimiter}, StringSplitOptions.RemoveEmptyEntries).Select(token =>
                {
                    if (enumDef.Lookups.TryGetValue(token, out var flags) || EnumDef.TryParseInt(token, out flags))
                        return string.Join(EnumDef.Delimiter, enumDef.Values.Where(v => enumDef.Lookups.TryGetValue(v.Key, out var bit) && (bit & flags) != 0)
                            .Select(v => EnumLabel(path, v.Key, v.Value)));
                    return EnumLabel(path, token, enumDef.Values.TryGetValue(token, out var value) ? value : token);
                }));
            if (property.Def?.Type == PropertyType.Boolean)
                return Resolve(legacy, "properties", path, "booleanValues", card.GetPropertyValueBool(path) ? "true" : "false");
            if (property.Def != null && LocalizationTargets.IsText(property.Def.Type))
                return path == _targets.Game.CardNameIdentifier ? CardName(card) : Resolve(legacy, "cards", card.Id, "properties", path);
            return legacy;
        }

        /// <summary>Ordered translation candidates only. The caller tries these, then its existing canonical asset.</summary>
        public IReadOnlyList<LocalizationResourceCandidate> AssetCandidates(params string[] assetPath)
        {
            var key = P(new[] {"assets"}.Concat(assetPath).ToArray());
            var result = new List<LocalizationResourceCandidate>();
            foreach (var binding in _bindings)
                if (binding.Leaves.TryGetValue(key, out var reference) && LocalizationOrigin.Resolve(reference, binding.Origin) is Uri uri)
                    result.Add(new LocalizationResourceCandidate(binding.Locale, reference, uri));
            return result;
        }
    }
}
