namespace FinolDigital.Cgs.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class LocalizationLocale
    {
        // RFC 5646 section 2.1 grammar, with repeated variants/singletons rejected.
        private static readonly Regex Tag = new Regex(
            @"\A(?:(?:[a-z]{2,3}(?:-[a-z]{3}){0,3}|[a-z]{4}|[a-z]{5,8})(?:-[a-z]{4})?(?:-(?:[a-z]{2}|[0-9]{3}))?(?<variants>(?:-(?:[a-z0-9]{5,8}|[0-9][a-z0-9]{3}))*)(?<extensions>(?:-[0-9a-wy-z](?:-[a-z0-9]{2,8})+)*)(?:-x(?:-[a-z0-9]{1,8})+)?|x(?:-[a-z0-9]{1,8})+)\z",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        private static readonly HashSet<string> Grandfathered = new HashSet<string>(
            ("en-GB-oed i-ami i-bnn i-default i-enochian i-hak i-klingon i-lux i-mingo i-navajo i-pwn i-tao i-tay i-tsu " +
             "sgn-BE-FR sgn-BE-NL sgn-CH-DE art-lojban cel-gaulish no-bok no-nyn zh-guoyu zh-hakka zh-min zh-min-nan zh-xiang").Split(' '),
            StringComparer.OrdinalIgnoreCase);

        public static bool IsValid(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Grandfathered.Contains(tag!)) return true;
            Match match;
            try { match = Tag.Match(tag!); }
            catch (RegexMatchTimeoutException) { return false; }
            if (!match.Success) return false;
            var variants = match.Groups["variants"].Value.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            if (variants.Distinct(StringComparer.OrdinalIgnoreCase).Count() != variants.Length) return false;
            var singletons = match.Groups["extensions"].Value.Split('-').Where(p => p.Length == 1).ToArray();
            return singletons.Distinct(StringComparer.OrdinalIgnoreCase).Count() == singletons.Length;
        }

        public static IReadOnlyList<string> FallbackChain(string? selected, string? defaultLocale)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var initial in new[] {selected, defaultLocale})
            {
                if (!IsValid(initial)) continue;
                var candidate = initial!;
                while (candidate.Length > 0)
                {
                    if (IsValid(candidate) && seen.Add(candidate)) result.Add(candidate);
                    var end = candidate.LastIndexOf('-');
                    if (end < 0) break;
                    candidate = candidate.Substring(0, end);
                    while (candidate.Length > 0 && candidate.Substring(candidate.LastIndexOf('-') + 1).Length == 1)
                    {
                        end = candidate.LastIndexOf('-');
                        candidate = end < 0 ? "" : candidate.Substring(0, end);
                    }
                }
            }
            return result;
        }
    }
}
