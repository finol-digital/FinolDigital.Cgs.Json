namespace FinolDigital.Cgs.Json
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>Explicit document origin. Never infer one from AutoUpdateUrl or a game ID.</summary>
    public sealed class LocalizationOrigin
    {
        public Uri? RemoteUrl { get; }
        private readonly string? _gameDirectory;
        private readonly string? _documentPath;
        private readonly Func<string, string?>? _physicalPath;
        private LocalizationOrigin(Uri? remote, string? root, string? document, Func<string, string?>? physicalPath)
        { RemoteUrl = remote; _gameDirectory = root; _documentPath = document; _physicalPath = physicalPath; }

        public static LocalizationOrigin Remote(Uri finalResponseUrl)
        {
            if (!finalResponseUrl.IsAbsoluteUri || (finalResponseUrl.Scheme != "http" && finalResponseUrl.Scheme != "https"))
                throw new ArgumentException("Expected the final absolute HTTP(S) document URL.", nameof(finalResponseUrl));
            return new LocalizationOrigin(finalResponseUrl, null, null, null);
        }

        /// <param name="physicalPath">Consumer-provided filesystem resolver: resolve every ancestor symlink,
        /// return the absolute physical path of an existing file/directory, or null if unavailable.
        /// A consumer may instead keep a stable trusted-root alias and reject every link
        /// below that root. Both strategies must prevent package paths escaping the root.
        /// This callback is mandatory because netstandard2.1 has no portable symlink resolution API.</param>
        public static LocalizationOrigin Bundled(string gameDirectory, string documentPath, Func<string, string?> physicalPath)
            => new LocalizationOrigin(null, Path.GetFullPath(gameDirectory), Path.GetFullPath(documentPath), physicalPath ?? throw new ArgumentNullException(nameof(physicalPath)));

        public static Uri? Resolve(string reference, LocalizationOrigin? origin)
        {
            if (!LocalizationJson.IsResourceReference(reference)) return null;
            var parsed = new Uri(reference, UriKind.RelativeOrAbsolute);
            if (parsed.IsAbsoluteUri) return parsed;
            if (origin?.RemoteUrl != null) return new Uri(origin.RemoteUrl, parsed);
            if (origin?._physicalPath == null || origin._gameDirectory == null || origin._documentPath == null) return null;
            try
            {
                var root = origin._physicalPath(origin._gameDirectory);
                var document = origin._physicalPath(origin._documentPath);
                if (root == null || document == null || !Inside(root, document)) return null;
                var separator = reference.IndexOfAny(new[] {'?', '#'});
                var path = Uri.UnescapeDataString(separator < 0 ? reference : reference.Substring(0, separator));
                // Reject decoded rooted paths and platform-dependent separators too.
                if (path.StartsWith("/") || path.Contains("\\") || path.Contains(":") || Path.IsPathRooted(path)) return null;
                var target = path.Length == 0 ? document : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(document)!, path));
                if (!Inside(root, target)) return null;
                var physical = origin._physicalPath(target);
                if (physical == null || !Inside(root, physical)) return null;
                // A physical filename containing "%2e" must not be decoded a second time
                // when it becomes a file URI; preserve literal reserved characters first.
                var uriPath = physical.Replace(Path.DirectorySeparatorChar, '/');
                var escapedPath = string.Join("/", uriPath.Split('/').Select(Uri.EscapeDataString));
                string absolute;
                if (Path.DirectorySeparatorChar == '\\' && uriPath.StartsWith("//", StringComparison.Ordinal))
                    absolute = "file:" + escapedPath; // UNC host/share
                else if (Path.DirectorySeparatorChar == '\\')
                    absolute = "file:///" + uriPath.Substring(0, 2) + escapedPath.Substring(4); // drive colon
                else
                    absolute = "file://" + escapedPath;
                var fileUri = new Uri(absolute + (separator < 0 ? "" : reference.Substring(separator)));
                var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!string.Equals(Path.GetFullPath(fileUri.LocalPath), Path.GetFullPath(physical), comparison)) return null;
                return fileUri;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            { return null; }
        }

        private static bool Inside(string root, string path)
        {
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path);
            return path.StartsWith(root, Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
    }

    public sealed class LocalizationResourceCandidate
    {
        public string Locale { get; }
        public string Reference { get; }
        public Uri Uri { get; }
        internal LocalizationResourceCandidate(string locale, string reference, Uri uri)
        { Locale = locale; Reference = reference; Uri = uri; }
    }
}
