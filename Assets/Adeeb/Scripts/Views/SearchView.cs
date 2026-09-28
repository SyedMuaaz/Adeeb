using System;
using System.Collections.Generic;
using Adeeb.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Adeeb.Views
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SearchView : MonoBehaviour
    {
        public const int PageSize = 12;
        private TextField query;
        private Button search, refresh, previous, next;
        private Label status, summary, page;
        private ScrollView scroll;
        private readonly List<SearchResultCardView> cards = new List<SearchResultCardView>();
        public event Action<string> SearchRequested;
        public event Action RefreshRequested;
        public event Action QueryCleared;
        public event Action<int> PageRequested;
        public string Query => query.value;
        public string StatusText => status.text;
        public string SummaryText => summary.text;

        public void Initialize()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            query = root.Q<TextField>("query"); search = root.Q<Button>("search");
            refresh = root.Q<Button>("refresh"); previous = root.Q<Button>("previous"); next = root.Q<Button>("next");
            status = root.Q<Label>("status"); summary = root.Q<Label>("summary"); page = root.Q<Label>("page");
            scroll = root.Q<ScrollView>("results");
            search.clicked += Submit; refresh.clicked += Refresh; previous.clicked += Previous; next.clicked += Next;
            query.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            query.RegisterValueChangedCallback(OnQueryChanged);
            for (int i = 0; i < PageSize; i++) { var card = new SearchResultCardView(); card.style.display = DisplayStyle.None; scroll.Add(card); cards.Add(card); }
        }
        private void OnQueryChanged(ChangeEvent<string> evt)
        {
            query.languageDirection = SearchResultCardView.IsArabic(evt.newValue) ? LanguageDirection.RTL : LanguageDirection.LTR;
            if (string.IsNullOrWhiteSpace(evt.newValue)) QueryCleared?.Invoke();
        }
        private void OnKey(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
            if (search.enabledSelf) Submit(); evt.StopPropagation();
        }
        private void Submit() => SearchRequested?.Invoke(query.value);
        private void Refresh() => RefreshRequested?.Invoke();
        private void Previous() => PageRequested?.Invoke(-1);
        private void Next() => PageRequested?.Invoke(1);
        public void SetQuery(string text) => query.value = text;
        public void SetLoading(bool busy)
        {
            refresh.SetEnabled(!busy); search.SetEnabled(!busy); query.SetEnabled(!busy);
            if (busy) { previous.SetEnabled(false); next.SetEnabled(false); }
        }
        public void ShowEmptySearch()
        {
            foreach (var card in cards) card.style.display = DisplayStyle.None;
            summary.text = "Enter a content name or author to search.";
            page.text = "";
            previous.parent.style.display = DisplayStyle.None;
            previous.SetEnabled(false); next.SetEnabled(false);
            scroll.scrollOffset = Vector2.zero;
        }
        public void ShowStatus(string text) => status.text = text;
        public void ShowResults(IReadOnlyList<ContentMetadata> matches, int pageIndex, string searchedQuery)
        {
            previous.parent.style.display = matches.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            int pages = Math.Max(1, (matches.Count + PageSize - 1) / PageSize);
            for (int i = 0; i < cards.Count; i++)
            {
                int index = pageIndex * PageSize + i;
                cards[i].style.display = index < matches.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (index < matches.Count) cards[i].Bind(matches[index]);
            }
            summary.enableRichText = false;
            summary.text = matches.Count == 0 ? "No matching content." : matches.Count + " result" + (matches.Count == 1 ? "" : "s");
            if (!string.IsNullOrWhiteSpace(searchedQuery)) summary.text += " for: " + searchedQuery;
            page.text = matches.Count == 0 ? "" : "Page " + (pageIndex + 1) + " of " + pages;
            previous.SetEnabled(pageIndex > 0); next.SetEnabled(pageIndex + 1 < pages);
            scroll.scrollOffset = Vector2.zero;
        }
        private void OnDestroy()
        {
            if (search == null) return;
            search.clicked -= Submit; refresh.clicked -= Refresh; previous.clicked -= Previous; next.clicked -= Next;
            query.UnregisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            query.UnregisterValueChangedCallback(OnQueryChanged);
        }
    }
}
