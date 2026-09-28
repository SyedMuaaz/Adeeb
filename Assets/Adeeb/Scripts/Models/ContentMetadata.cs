using System;

namespace Adeeb.Models
{
    /// <summary>Read-only projection of legacy CoverInfo, never written back to Firebase.</summary>
    public sealed class ContentMetadata
    {
        public string Id { get; }
        public string Title { get; }
        public string Author { get; }
        public DateTime? Date { get; }
        public string Warning { get; }
        public string SearchTitle { get; }
        public string SearchAuthor { get; }

        public ContentMetadata(string id, string title, string author, DateTime? date,
            string warning, string searchTitle, string searchAuthor)
        {
            Id = id; Title = title; Author = author; Date = date; Warning = warning;
            SearchTitle = searchTitle; SearchAuthor = searchAuthor;
        }
    }
}
