namespace FinolDigital.Cgs.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class LocalizationDiagnostic
    {
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
        public bool IsWarning { get; }
        public LocalizationDiagnostic(string code, string path, string message, bool isWarning = false)
        { Code = code; Path = path; Message = message; IsWarning = isWarning; }
    }

    public sealed class LocalizationReadResult<T> where T : class
    {
        public T? Value { get; }
        public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; }
        public bool IsValid => Value != null && !Diagnostics.Any(d => !d.IsWarning);
        internal LocalizationReadResult(T? value, List<LocalizationDiagnostic> diagnostics)
        { Value = value; Diagnostics = diagnostics.AsReadOnly(); }
    }

    /// <summary>Safe boundary for untrusted JSON. Invalid leaves are absent, never coerced to strings.</summary>
    public static class LocalizationJson
    {
        public static LocalizationReadResult<LocalizationCatalog> ReadCatalog(string json) => Read<LocalizationCatalog>(json);
        public static LocalizationReadResult<GameLocalization> ReadManifest(string json)
        {
            var result = Read<GameLocalization>(json);
            if (result.Value == null) return result;
            var diagnostics = result.Diagnostics.ToList();
            ValidateManifest(result.Value, diagnostics);
            return new LocalizationReadResult<GameLocalization>(
                diagnostics.Any(d => d.Code == "manifest") ? null : result.Value, diagnostics);
        }

        internal static void ValidateManifest(GameLocalization manifest, List<LocalizationDiagnostic> diagnostics)
        {
            void Error(string path, string message) => diagnostics.Add(new LocalizationDiagnostic("manifest", path, message));
            if (!LocalizationLocale.IsValid(manifest.DefaultLocale)) Error("/defaultLocale", "Expected a BCP 47 language tag.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest.Locales == null) return;
            foreach (var entry in manifest.Locales)
            {
                var path = Pointer("locales", entry.Key);
                if (!LocalizationLocale.IsValid(entry.Key) || !seen.Add(entry.Key))
                    Error(path, "Invalid or case-insensitively duplicate locale.");
                var source = entry.Value;
                if (source == null || (source.Url == null) == (source.Inline == null))
                    Error(path, "Supply exactly one URL or inline catalog.");
                else if (source.Url != null && !IsResourceReference(source.Url))
                    Error(path + "/url", "Expected a nonempty HTTP(S) or relative resource reference.");
            }
        }

        internal static bool IsResourceReference(string? value)
        {
            if (string.IsNullOrEmpty(value) || value!.Any(c => char.IsWhiteSpace(c) || c > 127) || value.Contains("\\") ||
                value.StartsWith("/") || value.Contains("{") || value.Contains("}")) return false;
            if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)) return false;
            if (uri.IsAbsoluteUri)
                return (uri.Scheme == "http" || uri.Scheme == "https") && uri.IsWellFormedOriginalString() && !string.IsNullOrEmpty(uri.Host);
            // RFC 3986 path-noscheme forbids a colon in the first relative segment.
            // Uri may classify malformed HTTP(S) references as relative nonetheless.
            var colon = value.IndexOf(':');
            var separator = value.IndexOfAny(new[] { '/', '?', '#' });
            if (colon >= 0 && (separator < 0 || colon < separator)) return false;
            // CoreCLR rejects fragments in its relative-only well-formedness check.
            // Validate the same text in an HTTP path context; this is syntax validation
            // only and never supplies a resource origin or starts a request.
            return Uri.IsWellFormedUriString("https://localization.invalid/" + value, UriKind.Absolute);
        }

        private static LocalizationReadResult<T> Read<T>(string json) where T : class
        {
            var diagnostics = new List<LocalizationDiagnostic>();
            try
            {
                var quarantined = new HashSet<string>(StringComparer.Ordinal);
                var manifest = typeof(T) == typeof(GameLocalization);
                if (manifest && !ValidateDuplicates(json, diagnostics, quarantined))
                    return new LocalizationReadResult<T>(null, diagnostics);
                using var text = new StringReader(json);
                using var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None };
                var token = JToken.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = manifest ? DuplicatePropertyNameHandling.Replace : DuplicatePropertyNameHandling.Error
                });
                if (token is JObject root && root["locales"] is JObject locales)
                    foreach (var locale in quarantined)
                        if (locales[locale] is JObject source) source["inline"] = new JObject();
                while (reader.Read())
                    if (reader.TokenType != JsonToken.Comment) throw new JsonReaderException("Unexpected content after localization document.");
                if (typeof(T) == typeof(GameLocalization) && token is JObject manifestObject && manifestObject["locales"] is JObject sources)
                    foreach (var source in sources.Properties())
                        if (source.Value is JObject fields && (fields.Property("url") != null) == (fields.Property("inline") != null))
                            diagnostics.Add(new LocalizationDiagnostic("manifest", Pointer("locales", source.Name), "Supply exactly one URL or inline catalog."));
                var structureValid = true;
                var cleaned = Clean(typeof(T), token, "", diagnostics, ref structureValid);
                return new LocalizationReadResult<T>(structureValid ? cleaned?.ToObject<T>() : null, diagnostics);
            }
            catch (JsonException ex)
            {
                diagnostics.Add(new LocalizationDiagnostic("json", "", ex.Message));
                return new LocalizationReadResult<T>(null, diagnostics);
            }
        }

        // JToken.Load loses duplicate keys in replacement mode. Inspect the token stream
        // first, retaining exact pointer segments so dotted/slashed names cannot change
        // which inline catalog owns an error. Duplicates outside a catalog reject the manifest.
        private static bool ValidateDuplicates(string json, List<LocalizationDiagnostic> diagnostics, HashSet<string> quarantined)
        {
            using var text = new StringReader(json);
            using var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None };
            if (!ReadContent(reader)) throw new JsonReaderException("Empty localization document.");
            var usable = true;
            ScanDuplicates(reader, new List<string>(), diagnostics, quarantined, ref usable);
            if (ReadContent(reader)) throw new JsonReaderException("Unexpected content after localization document.");
            return usable;
        }

        private static bool ReadContent(JsonReader reader)
        {
            while (reader.Read())
                if (reader.TokenType != JsonToken.Comment) return true;
            return false;
        }

        private static void ScanDuplicates(JsonReader reader, List<string> path,
            List<LocalizationDiagnostic> diagnostics, HashSet<string> quarantined, ref bool usable)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                while (ReadContent(reader) && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) throw new JsonReaderException("Expected a JSON member.");
                    var name = (string)reader.Value!;
                    path.Add(name);
                    if (!names.Add(name))
                    {
                        var inline = path.Count > 3 && path[0] == "locales" && path[2] == "inline";
                        diagnostics.Add(new LocalizationDiagnostic("json", Pointer(path.ToArray()),
                            inline ? "Duplicate JSON member; inline catalog unavailable." : "Duplicate JSON member; manifest unavailable."));
                        if (inline) quarantined.Add(path[1]);
                        else usable = false;
                    }
                    if (!ReadContent(reader)) throw new JsonReaderException("Missing JSON member value.");
                    ScanDuplicates(reader, path, diagnostics, quarantined, ref usable);
                    path.RemoveAt(path.Count - 1);
                }
                if (reader.TokenType != JsonToken.EndObject) throw new JsonReaderException("Unclosed JSON object.");
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                var index = 0;
                while (ReadContent(reader) && reader.TokenType != JsonToken.EndArray)
                {
                    path.Add((index++).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ScanDuplicates(reader, path, diagnostics, quarantined, ref usable);
                    path.RemoveAt(path.Count - 1);
                }
                if (reader.TokenType != JsonToken.EndArray) throw new JsonReaderException("Unclosed JSON array.");
            }
            else if (reader.TokenType == JsonToken.StartConstructor)
                throw new JsonReaderException("JSON constructors are not supported.");
        }

        private static JToken? Clean(Type type, JToken token, string path, List<LocalizationDiagnostic> diagnostics, ref bool structureValid)
        {
            if (type == typeof(string))
            {
                if (token.Type == JTokenType.String) return token.DeepClone();
                diagnostics.Add(new LocalizationDiagnostic("leaf", path, "Expected a string; invalid value ignored."));
                return null;
            }
            if (!(token is JObject obj))
            {
                diagnostics.Add(new LocalizationDiagnostic("structure", path, "Expected an object; containing catalog or manifest unavailable."));
                structureValid = false;
                return null;
            }
            var result = new JObject();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LocalizationMap<>))
            {
                foreach (var p in obj.Properties())
                {
                    var clean = Clean(type.GetGenericArguments()[0], p.Value, path + Pointer(p.Name), diagnostics, ref structureValid);
                    if (clean != null) result.Add(p.Name, clean);
                }
            }
            else
            {
                var properties = type.GetProperties().Select(p => (Property: p, Attribute: p.GetCustomAttribute<JsonPropertyAttribute>()))
                    .Where(p => p.Attribute != null).ToDictionary(p => p.Attribute!.PropertyName!, p => p.Property, StringComparer.Ordinal);
                foreach (var p in obj.Properties())
                {
                    if (!properties.TryGetValue(p.Name, out var property))
                    {
                        diagnostics.Add(new LocalizationDiagnostic("unknown-member", path + Pointer(p.Name), "Unknown catalog member ignored."));
                        continue;
                    }
                    // An inline catalog is its own failure boundary, just like an external
                    // catalog. Preserve the source as empty so other locales can still resolve.
                    JToken? clean;
                    if (property.PropertyType == typeof(LocalizationCatalog))
                    {
                        var catalogValid = true;
                        clean = Clean(property.PropertyType, p.Value, path + Pointer(p.Name), diagnostics, ref catalogValid);
                        if (!catalogValid) clean = new JObject();
                    }
                    else
                        clean = Clean(property.PropertyType, p.Value, path + Pointer(p.Name), diagnostics, ref structureValid);
                    if (clean != null) result.Add(p.Name, clean);
                }
            }
            return result;
        }

        internal static string Pointer(params string[] segments) => "/" + string.Join("/", segments.Select(s => s.Replace("~", "~0").Replace("/", "~1")));

        internal static Dictionary<string, string> Leaves(object value)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            void Visit(JToken token, string path)
            {
                if (token is JObject obj)
                    foreach (var p in obj.Properties()) Visit(p.Value, path + Pointer(p.Name));
                else if (token.Type == JTokenType.String) result[path] = (string)token!;
            }
            Visit(JObject.FromObject(value), "");
            return result;
        }
    }

    /// <summary>Malformed optional metadata must not prevent canonical game deserialization.</summary>
    public sealed class GameLocalizationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(GameLocalization);
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            // Copy the complete token stream before validating, preserving duplicate member names.
            // This also leaves the enclosing CardGame reader positioned after the optional metadata.
            using var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            var dateHandling = reader.DateParseHandling;
            try
            {
                reader.DateParseHandling = DateParseHandling.None;
                using var writer = new JsonTextWriter(text);
                writer.WriteToken(reader);
            }
            finally { reader.DateParseHandling = dateHandling; }
            var authoredJson = text.ToString();
            var result = LocalizationJson.ReadManifest(authoredJson);
            var value = result.Value ?? new GameLocalization();
            value.Diagnostics = result.Diagnostics;
            value.IsUsable = result.Value != null;
            value.AuthoredJson = authoredJson;
            value.ReadState = ModelState(value);
            return value;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            var manifest = (GameLocalization)value;
            var current = ModelState(manifest);
            if (manifest.AuthoredJson != null && JToken.DeepEquals(current, manifest.ReadState))
                // Replay the captured token stream, including unknown/invalid data and duplicate names.
                writer.WriteRawValue(manifest.AuthoredJson);
            else
                // Unity's bundled Newtonsoft version exposes the converters overload.
                current.WriteTo(writer, Array.Empty<JsonConverter>());
        }

        // Use the declared JSON contract independently of caller defaults or this converter being
        // registered globally. Runtime diagnostics and retained source data are not model edits.
        private static JToken ModelState(GameLocalization value) => JToken.FromObject(value, new JsonSerializer());
    }
}
