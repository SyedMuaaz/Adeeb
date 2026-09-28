using System;
using Adeeb.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class EditorView : MonoBehaviour
    {
        public AssetBrowserView assetBrowser;
        public PageView page;
        public InputField title; public Button save; public CanvasGroup interaction;
        public Button back, preview, addPage, previous, next, removeObject, smaller, larger;
        public Text pageLabel, selectionLabel, status;
        public event Action SaveRequested, BackRequested, PreviewRequested, AddPageRequested, PreviousRequested, NextRequested, DeleteRequested;
        public event Action<float> ScaleRequested;
        public event Action<string> TitleChanged;
        private void Awake()
        {
            save.onClick.AddListener(() => SaveRequested?.Invoke());
            back.onClick.AddListener(() => BackRequested?.Invoke());
            preview.onClick.AddListener(() => PreviewRequested?.Invoke());
            addPage.onClick.AddListener(() => AddPageRequested?.Invoke());
            previous.onClick.AddListener(() => PreviousRequested?.Invoke());
            next.onClick.AddListener(() => NextRequested?.Invoke());
            removeObject.onClick.AddListener(() => DeleteRequested?.Invoke());
            smaller.onClick.AddListener(() => ScaleRequested?.Invoke(-.1f));
            larger.onClick.AddListener(() => ScaleRequested?.Invoke(.1f));
            title.onEndEdit.AddListener(value => TitleChanged?.Invoke(value));
        }
        public void ShowPage(int index, int count)
        {
            pageLabel.text = "Page " + (index+1) + " of " + count;
            previous.interactable = index > 0;
            next.interactable = index < count-1;
        }
        public void ShowSelection(string name, float scale)
        {
            bool selected = name != null;
            selectionLabel.text = selected ? name + "  /  " + Mathf.RoundToInt(scale*100) + "%" : "Select an object to move or resize it";
            smaller.interactable = selected && scale > .2f;
            larger.interactable = selected && scale < 3f;
            removeObject.interactable = selected;
        }
    }
}
