namespace FinolDigital.Cgs.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Inventory from loaded canonical data, never reconstructed from source JSON or ID suffixes.</summary>
    public sealed class LocalizationTargets
    {
        private readonly Dictionary<string, string> _leaves = new Dictionary<string, string>(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, string> Leaves => _leaves;
        internal CardGame Game { get; }
        internal Dictionary<string, Card> Cards { get; } = new Dictionary<string, Card>(StringComparer.Ordinal);
        internal Dictionary<string, PropertyDef> Properties { get; } = new Dictionary<string, PropertyDef>(StringComparer.Ordinal);
        internal Dictionary<string, Dictionary<string, PropertyDefValuePair>> CardProperties { get; } = new Dictionary<string, Dictionary<string, PropertyDefValuePair>>(StringComparer.Ordinal);
        internal Dictionary<string, EnumDef> Enums { get; } = new Dictionary<string, EnumDef>(StringComparer.Ordinal);
        private static string P(params string[] parts) => LocalizationJson.Pointer(parts);
        private void Add(string value, params string[] parts) => _leaves[P(parts)] = value;
        internal bool Contains(string path) => _leaves.ContainsKey(path);
        internal static bool IsText(PropertyType type) => type == PropertyType.String || type == PropertyType.EscapedString || type == PropertyType.StringList;

        public LocalizationTargets(CardGame game, IEnumerable<Card> cards, IEnumerable<Set>? sets = null,
            IEnumerable<string>? authoredDeckNames = null, IReadOnlyDictionary<string, PropertyDef>? resolvedProperties = null,
            IEnumerable<string>? boardIds = null, IEnumerable<string>? cardBackIds = null)
        {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            Add(game.Name, "game", "name");
            Add(game.Copyright, "game", "copyright");
            if (!string.IsNullOrEmpty(game.GamePlayDeckName)) Add(game.GamePlayDeckName, "game", "gamePlayDeckName");
            Add(game.SetNameDefault, "game", "setNameDefault");
            foreach (var property in game.CardProperties) AddDefinition(property, property.Name);
            if (resolvedProperties != null)
                foreach (var property in resolvedProperties) Properties[property.Key] = property.Value;
            foreach (var def in game.Enums) Enums[def.Property] = def;
            foreach (var card in cards)
            {
                Cards[card.Id] = card;
                var properties = card.CloneProperties();
                CardProperties[card.Id] = properties;
                Add(card.Name, "cards", card.Id, "name");
                Add(card.Name, "cardNames", card.Name);
                Add(card.ImageWebUrl, "assets", "cardImages", card.Id);
                foreach (var property in properties)
                {
                    var path = property.Key;
                    var def = property.Value.Def;
                    if (def != null) Properties[path] = def;
                    if (Enums.TryGetValue(path, out var enumDef))
                    {
                        foreach (var token in property.Value.Value.Split(new[] {EnumDef.Delimiter}, StringSplitOptions.RemoveEmptyEntries))
                            if (!enumDef.Values.ContainsKey(token) && !enumDef.Lookups.ContainsKey(token) && !EnumDef.TryParseInt(token, out _))
                                Add(token, "enums", path, token);
                    }
                    else if (def != null && IsText(def.Type) && path != game.CardNameIdentifier)
                        Add(card.GetPropertyValueString(path), "cards", card.Id, "properties", path);
                }
            }
            foreach (var property in Properties)
            {
                Add(property.Value.Display, "properties", property.Key, "display");
                Add(property.Value.DisplayEmpty, "properties", property.Key, "displayEmpty");
                if (property.Value.Type == PropertyType.Boolean && !Enums.ContainsKey(property.Key))
                {
                    Add("true", "properties", property.Key, "booleanValues", "true");
                    Add("false", "properties", property.Key, "booleanValues", "false");
                }
            }
            foreach (var def in Enums)
                foreach (var value in def.Value.Values) Add(value.Value, "enums", def.Key, value.Key);
            foreach (var code in Cards.Values.Select(c => c.SetCode).Distinct(StringComparer.Ordinal))
                if (code != game.SetCodeDefault) Add(code, "sets", code, "name");
            foreach (var set in sets ?? Array.Empty<Set>())
                if (set.Code != game.SetCodeDefault) Add(set.Name, "sets", set.Code, "name");
            foreach (var name in game.DeckUrls.Select(d => d.Name).Concat(game.GameStartDecks.Select(d => d.Name)).Concat(authoredDeckNames ?? Array.Empty<string>()))
                Add(name, "decks", name, "name");
            foreach (var group in game.Extras.Select(e => string.IsNullOrEmpty(e.Group) ? ExtraDef.DefaultExtraGroup : e.Group)) Add(group, "extraGroups", group);
            foreach (var zone in game.GamePlayZones) Add(zone.Name, "zones", zone.Name);
            foreach (var id in game.GameBoardCards.SelectMany(c => c.Boards).Select(b => b.Id).Concat(boardIds ?? Array.Empty<string>()))
            { Add(id, "boards", id, "name"); Add("", "assets", "boards", id); }
            foreach (var id in cardBackIds ?? Array.Empty<string>())
            { Add(id, "cardBacks", id, "name"); Add("", "assets", "cardBacks", id); }
            foreach (var board in game.GameBoardUrls)
            { Add(board.Id, "boards", board.Id, "name"); Add(board.Url?.OriginalString ?? "", "assets", "boards", board.Id); }
            foreach (var back in game.CardBackFaceImageUrls)
            { Add(back.Id, "cardBacks", back.Id, "name"); Add(back.Url?.OriginalString ?? "", "assets", "cardBacks", back.Id); }
            Add(game.BannerImageUrl?.OriginalString ?? "", "assets", "bannerImageUrl");
            Add(game.CardBackImageUrl?.OriginalString ?? "", "assets", "cardBackImageUrl");
            Add(game.PlayMatImageUrl?.OriginalString ?? "", "assets", "playMatImageUrl");
            Add(game.RulesUrl?.OriginalString ?? "", "assets", "rulesUrl");
        }

        private void AddDefinition(PropertyDef def, string path)
        {
            Properties[path] = def;
            foreach (var child in def.Properties) AddDefinition(child, path + PropertyDef.ObjectDelimiter + child.Name);
        }

        public IReadOnlyList<LocalizationDiagnostic> Validate(LocalizationCatalog catalog)
        {
            var diagnostics = new List<LocalizationDiagnostic>();
            var leaves = LocalizationJson.Leaves(catalog);
            foreach (var leaf in leaves)
            {
                if (!Contains(leaf.Key))
                    diagnostics.Add(new LocalizationDiagnostic("target", leaf.Key, "Unknown or ineligible target; ignored. Inventory must include all loaded canonical data."));
                if (leaf.Key == P("sets", Game.SetCodeDefault, "name"))
                    diagnostics.Add(new LocalizationDiagnostic("default-set", leaf.Key, "Use game.setNameDefault; this competing target is ignored."));
                if (leaf.Key.StartsWith("/assets/", StringComparison.Ordinal) && !LocalizationJson.IsResourceReference(leaf.Value))
                    diagnostics.Add(new LocalizationDiagnostic("resource", leaf.Key, "Invalid resource reference; ignored."));
            }
            // Controls bind by canonical path even when visible property labels collide.
            foreach (var group in leaves.Where(p => p.Key.StartsWith("/properties/", StringComparison.Ordinal) && p.Key.EndsWith("/display", StringComparison.Ordinal))
                         .GroupBy(p => p.Value, StringComparer.Ordinal).Where(g => g.Count() > 1))
                foreach (var leaf in group)
                    diagnostics.Add(new LocalizationDiagnostic("duplicate-label", leaf.Key, "Multiple properties share this translated label; retain canonical control keys.", true));
            return diagnostics;
        }

        public LocalizationCoverage Coverage(LocalizationCatalog catalog)
        {
            var translated = LocalizationJson.Leaves(catalog);
            var valid = new HashSet<string>(translated.Keys.Where(Contains), StringComparer.Ordinal);
            foreach (var entry in translated.Where(p => p.Key.StartsWith("/assets/", StringComparison.Ordinal) && !LocalizationJson.IsResourceReference(p.Value))) valid.Remove(entry.Key);
            var missing = new HashSet<string>(_leaves.Keys.Where(k => !valid.Contains(k)), StringComparer.Ordinal);
            foreach (var card in Cards.Values)
                if (valid.Contains(P("cardNames", card.Name))) missing.Remove(P("cards", card.Id, "name"));
            foreach (var group in Cards.Values.GroupBy(card => card.Name, StringComparer.Ordinal))
                if (group.All(card => valid.Contains(P("cards", card.Id, "name"))))
                    missing.Remove(P("cardNames", group.Key));
            return new LocalizationCoverage(_leaves.Count, _leaves.Keys.Where(missing.Contains).ToList(), translated.Keys.Where(k => !Contains(k)).ToList());
        }
    }

    public sealed class LocalizationCoverage
    {
        public int Total { get; }
        public IReadOnlyList<string> Missing { get; }
        public IReadOnlyList<string> Unknown { get; }
        internal LocalizationCoverage(int total, List<string> missing, List<string> unknown)
        { Total = total; Missing = missing.AsReadOnly(); Unknown = unknown.AsReadOnly(); }
    }
}
