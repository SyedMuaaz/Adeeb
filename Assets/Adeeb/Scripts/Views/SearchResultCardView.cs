using System.Globalization;
using Adeeb.Models;
using UnityEngine.UIElements;

namespace Adeeb.Views
{
    public sealed class SearchResultCardView : VisualElement
    {
        private readonly Label title = new Label();
        private readonly Label author = new Label();
        private readonly Label date = new Label();
        public SearchResultCardView()
        {
            AddToClassList("result-card");
            title.AddToClassList("result-title");
            Add(title); Add(author); Add(date);
            title.enableRichText = author.enableRichText = date.enableRichText = false;
        }
        public void Bind(ContentMetadata item)
        {
            title.text = item.Title.Length == 0 ? "Title unavailable" : item.Title;
            author.text = item.Author.Length == 0 ? "Author unavailable" : item.Author;
            author.tooltip = "Author";
            date.text = item.Date?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Date unavailable";
            tooltip = item.Warning ?? "";
            title.languageDirection = IsArabic(item.Title) ? LanguageDirection.RTL : LanguageDirection.LTR;
            author.languageDirection = IsArabic(item.Author) ? LanguageDirection.RTL : LanguageDirection.LTR;
        }
        internal static bool IsArabic(string value)
        {
            foreach (char c in value ?? "")
            {
                if (c >= '\u0600' && c <= '\u06FF') return true;
                if (char.IsLetter(c)) return false;
            }
            return false;
        }
    }
}
