using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Adeeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Adeeb.Services
{
    public sealed class ContentParseResult
    {
        public readonly List<ContentMetadata> Items = new List<ContentMetadata>();
        public int Records, Skipped, Incomplete;
        public string Error;
    }

    public static class CoverInfoParser
    {
        private static readonly string[] DateFormats = { "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "dd-MM-yyyy" };

        public static IEnumerator Parse(string json, ContentParseResult result, int batchSize = 50)
        {
            using (var source = new StringReader(json ?? ""))
            using (var reader = new JsonTextReader(source) { DateParseHandling = DateParseHandling.None, MaxDepth = 64 })
            {
                bool started;
                try { started = reader.Read() && reader.TokenType == JsonToken.StartObject; }
                catch (JsonException) { started = false; }
                if (!started) { result.Error = "The library is missing or is not a JSON object."; yield break; }
                while (true)
                {
                    string id = null;
                    JToken record = null;
                    bool ended = false;
                    try
                    {
                        if (!reader.Read()) throw new JsonReaderException();
                        if (reader.TokenType == JsonToken.EndObject) ended = true;
                        else
                        {
                            if (reader.TokenType != JsonToken.PropertyName) throw new JsonReaderException();
                            id = (string)reader.Value;
                            if (!reader.Read()) throw new JsonReaderException();
                            record = JToken.ReadFrom(reader);
                        }
                    }
                    catch (JsonException) { result.Error = "The server returned malformed library JSON."; yield break; }
                    if (ended) break;
                    result.Records++;
                    var item = ParseRecord(id, record);
                    if (item == null) result.Skipped++;
                    else { result.Items.Add(item); if (item.Warning != null) result.Incomplete++; }
                    if (result.Records % Math.Max(1, batchSize) == 0) yield return null;
                }
            }
            result.Items.Sort((a, b) => { int c = string.CompareOrdinal(a.SearchTitle, b.SearchTitle); return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id); });
        }

        public static ContentMetadata ParseRecord(string id, JToken record)
        {
            if (!(record is JObject obj) || !(obj["Story"] is JObject story)) return null;
            // Prefer the matching ID-based key. Only use a suffix fallback when unambiguous.
            JToken cover = story[id + "CoverInfo"];
            if (cover == null)
            {
                var candidates = story.Properties().Where(p => p.Name.EndsWith("CoverInfo", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (candidates.Length != 1) return null;
                cover = candidates[0].Value;
            }
            if (cover.Type != JTokenType.String) return null;
            return ParseValue(id, (string)cover);
        }

        public static ContentMetadata ParseValue(string id, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var fields = value.Split(new[] { '_', '|' }, StringSplitOptions.None);
            string title = Clean(fields[0]);
            string author = fields.Length > 1 ? Clean(fields[1]) : "";
            DateTime? date = null;
            if (fields.Length > 2 && TryDate(fields[2], out var parsed)) date = parsed;
            else
            {
                for (int i = 3; i < fields.Length; i++) if (TryDate(fields[i], out _)) return null;
            }
            if (title.Length == 0 && author.Length == 0) return null;
            string warning = title.Length == 0 || author.Length == 0 || date == null ? "Some metadata is unavailable." : null;
            return new ContentMetadata(id, title, author, date, warning, Normalize(title), Normalize(author));
        }

        private static bool TryDate(string text, out DateTime date) => DateTime.TryParseExact(
            Clean(text), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        public static string Clean(string value)
        {
            var b = new StringBuilder(); bool space = false;
            foreach (char c in value ?? "")
            {
                if (char.IsWhiteSpace(c)) { space = b.Length > 0; continue; }
                if (space) b.Append(' ');
                b.Append(c); space = false;
            }
            return b.ToString();
        }

        public static string Normalize(string value)
        {
            var b = new StringBuilder();
            foreach (char c in Clean(value).Normalize(NormalizationForm.FormKC))
            {
                if (c == '\u0640' || (c >= '\u064B' && c <= '\u065F') || c == '\u0670' ||
                    (c >= '\u06D6' && c <= '\u06ED' && CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)) continue;
                b.Append(char.ToLowerInvariant(c));
            }
            return b.ToString();
        }
    }
}
