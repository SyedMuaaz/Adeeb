using System;
using System.Collections.Generic;
using Adeeb.Models;
using Adeeb.Services;
using Adeeb.Views;
using UnityEngine;

namespace Adeeb.Controllers
{
    public sealed class SearchController : IDisposable
    {
        private readonly MonoBehaviour runner;
        private readonly SearchView view;
        private readonly RealtimeContentService service;
        private List<ContentMetadata> matches = new List<ContentMetadata>();
        private int pageIndex;
        private string currentQuery = "";
        private bool loading, disposed;
        public SearchController(MonoBehaviour runner, SearchView view, RealtimeContentService service)
        {
            this.runner = runner; this.view = view; this.service = service;
            view.SearchRequested += Search; view.RefreshRequested += Refresh; view.PageRequested += Navigate; view.QueryCleared += ClearSearch;
        }
        public void Open() { ClearSearch(); Load(false); }
        private void ClearSearch()
        {
            if (disposed) return;
            currentQuery = ""; pageIndex = 0; matches.Clear(); view.ShowEmptySearch();
        }
        private void Refresh() => Load(true);
        private void Load(bool force)
        {
            if (loading || disposed) return;
            loading = true; view.SetLoading(true); view.ShowStatus("Loading library...");
            runner.StartCoroutine(service.Load(force, (data, error) =>
            {
                if (disposed) return;
                loading = false; view.SetLoading(false);
                if (error != null)
                {
                    view.ShowStatus(error + (service.Cached != null ? " Previous results are still available." : ""));
                    if (currentQuery.Length == 0) view.ShowEmptySearch();
                    else view.ShowResults(matches, pageIndex, currentQuery); return;
                }
                view.ShowStatus(data.Items.Count + " searchable items loaded." +
                    (data.Incomplete > 0 ? " " + data.Incomplete + " have incomplete metadata." : "") +
                    (data.Skipped > 0 ? " " + data.Skipped + " unusable records skipped." : ""));
                Search(view.Query);
            }));
        }
        public void Search(string query)
        {
            if (disposed) return;
            if (CoverInfoParser.Normalize(query).Length == 0) { ClearSearch(); return; }
            if (loading || service.Cached == null) return;
            currentQuery = CoverInfoParser.Clean(query); pageIndex = 0;
            matches = ContentSearchService.Find(service.Cached.Items, query);
            view.ShowResults(matches, pageIndex, currentQuery);
        }
        private void Navigate(int delta)
        {
            if (loading || disposed) return;
            pageIndex = Math.Max(0, Math.Min(pageIndex + delta, Math.Max(0, (matches.Count - 1) / SearchView.PageSize)));
            view.ShowResults(matches, pageIndex, currentQuery);
        }
        public void Dispose()
        {
            disposed = true;
            view.SearchRequested -= Search; view.RefreshRequested -= Refresh; view.PageRequested -= Navigate; view.QueryCleared -= ClearSearch;
        }
    }
}
