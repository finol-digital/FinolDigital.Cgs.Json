// Starvoxel SVD3 deck codes, as used by https://starvoxel.com/decks

namespace FinolDigital.Cgs.Json.Decks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encodes and decodes Starvoxel SVD3 deck codes. A code is "SVD3-" followed by 17 base32 symbols:
    /// two symbols for each of eight different card ids, in play order, then one check symbol.
    /// </summary>
    public static class StarvoxelDeckEncoder
    {
        public const string Prefix = "SVD3-";
        public const int CardCount = 8;
        public const int MaxCardId = 1023;

        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const int SymbolCount = CardCount * 2 + 1;
        private const string DeckQueryKey = "deck=";

        /// <summary>Returns whether the ids form a valid deck: exactly eight different ids from 0 to 1023.</summary>
        public static bool CanEncode(IReadOnlyList<int>? cardIds)
        {
            return cardIds != null && cardIds.Count == CardCount &&
                   cardIds.All(id => id >= 0 && id <= MaxCardId) &&
                   cardIds.Distinct().Count() == CardCount;
        }

        /// <summary>Encodes eight different card ids, keeping their order.</summary>
        public static string Encode(IReadOnlyList<int> cardIds)
        {
            if (!CanEncode(cardIds))
                throw new ArgumentException(
                    $"A Starvoxel deck needs exactly {CardCount} different card ids from 0 to {MaxCardId}.",
                    nameof(cardIds));

            var values = new List<int>(SymbolCount);
            foreach (var cardId in cardIds)
            {
                values.Add(cardId >> 5);
                values.Add(cardId & 31);
            }

            values.Add(Shift(Checksum(values)));
            return Prefix + new string(values.Select(value => Alphabet[value]).ToArray());
        }

        /// <summary>
        /// Decodes a code, or a link with a deck= parameter. Letters may be in any case; I and L read as 1, and O as 0.
        /// </summary>
        public static bool TryDecode(string? text, out List<int> cardIds)
        {
            cardIds = new List<int>();
            var code = ExtractCode(text);
            if (code.Length != Prefix.Length + SymbolCount ||
                !code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var values = new List<int>(SymbolCount);
            foreach (var symbol in code.Substring(Prefix.Length).ToUpperInvariant())
            {
                var value = Alphabet.IndexOf(Normalize(symbol));
                if (value < 0)
                    return false;
                values.Add(value);
            }

            if (Checksum(values) != 0)
                return false;

            var ids = new List<int>(CardCount);
            for (var i = 0; i < CardCount; i++)
                ids.Add(values[i * 2] * 32 + values[i * 2 + 1]);
            if (ids.Distinct().Count() != CardCount)
                return false;

            cardIds = ids;
            return true;
        }

        private static string ExtractCode(string? text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            var queryIndex = trimmed.IndexOf(DeckQueryKey, StringComparison.OrdinalIgnoreCase);
            if (queryIndex < 0 || (queryIndex > 0 && trimmed[queryIndex - 1] != '?' && trimmed[queryIndex - 1] != '&'))
                return trimmed;

            var value = trimmed.Substring(queryIndex + DeckQueryKey.Length);
            var end = value.IndexOfAny(new[] { '&', '#' });
            if (end >= 0)
                value = value.Substring(0, end);
            try
            {
                return Uri.UnescapeDataString(value.Replace('+', ' ')).Trim();
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        private static char Normalize(char symbol)
        {
            return symbol switch
            {
                'I' => '1',
                'L' => '1',
                'O' => '0',
                _ => symbol
            };
        }

        private static int Checksum(IEnumerable<int> values)
        {
            return values.Aggregate(0, (sum, value) => Shift(sum) ^ value);
        }

        private static int Shift(int value)
        {
            return ((value << 1) ^ ((value & 16) != 0 ? 0x25 : 0)) & 31;
        }
    }
}
