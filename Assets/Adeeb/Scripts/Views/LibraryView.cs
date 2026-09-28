using System;
using System.Collections.Generic;
using Adeeb.Models;
using Adeeb.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class LibraryView : MonoBehaviour
    {
        public Button createButton, resumeButton, refreshButton;
        public Text message, connectionStatus;
        public Transform cardRoot;
        public SavedProjectCardView cardTemplate;
        private readonly List<GameObject> cards = new List<GameObject>();
        public event Action CreateRequested, ResumeRequested, RefreshRequested;
        public event Action<ProjectData> ProjectSelected;
        private void Awake()
        {
            createButton.onClick.AddListener(() => CreateRequested?.Invoke());
            resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            refreshButton.onClick.AddListener(() => RefreshRequested?.Invoke());
        }
        public void Show(bool hasDraft)
        {
            gameObject.SetActive(true);
            resumeButton.gameObject.SetActive(hasDraft);
            message.text = hasDraft ? "Resume your current draft, or open a saved story below."
                : "Create a classroom story, or open a saved story below.";
        }
        public void ShowProjects(IEnumerable<ProjectData> projects, MonoBehaviour runner, RemoteAssetService assets, IReadOnlyDictionary<string,AssetEntry> catalog)
        {
            foreach (var card in cards) { card.SetActive(false); Destroy(card); }
            cards.Clear();
            foreach (var project in projects)
            {
                var card = Instantiate(cardTemplate, cardRoot);
                card.name = "Project_" + project.id;
                card.title.text = project.title;
                card.button.onClick.AddListener(() => ProjectSelected?.Invoke(project));
                card.cover.Initialize(runner, assets, catalog);
                card.cover.LoadFailed += error => connectionStatus.text = "Some cover images could not load. Choose Refresh to retry.";
                card.gameObject.SetActive(true);
                card.cover.Render(project.pages[0], false);
                cards.Add(card.gameObject);
            }
        }
    }
}
