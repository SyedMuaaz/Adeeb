using System;
using System.Collections.Generic;
using Adeeb.Models;

namespace Adeeb.Services
{
    public static class ContentSearchService
    {
        public static List<ContentMetadata> Find(IReadOnlyList<ContentMetadata> items, string query)
        {
            string key = CoverInfoParser.Normalize(query);
            var matches = new List<ContentMetadata>();
            if (key.Length == 0) return matches;
            foreach (var item in items)
                if (item.SearchTitle.IndexOf(key, StringComparison.Ordinal) >= 0 ||
                    item.SearchAuthor.IndexOf(key, StringComparison.Ordinal) >= 0) matches.Add(item);
            return matches;
        }
    }
}
