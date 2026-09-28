using System;
using System.Collections.Generic;
using Adeeb.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class AssetBrowserView : MonoBehaviour
    {
        public Transform cardRoot;
        public Button cardTemplate;
        public Button retryButton;
        public Text statusText; public Text categoryHeading;
        private readonly Dictionary<string, Button> cards = new Dictionary<string, Button>();
        private readonly Dictionary<string, string> categories = new Dictionary<string, string>(); private string visibleCategory; public event Action<AssetEntry> AssetSelected;
        public event Action RetryRequested;

        private void Awake() { retryButton.onClick.AddListener(OnRetry); }
        private void OnDestroy() { retryButton.onClick.RemoveListener(OnRetry); }
        private void OnRetry() { RetryRequested?.Invoke(); }

        public void ShowCatalog(AssetCatalog catalog)
        {
            foreach (var card in cards.Values) Destroy(card.gameObject);
            cards.Clear(); categories.Clear();
            foreach (var asset in catalog.assets)
            {
                var card = Instantiate(cardTemplate, cardRoot);
                card.name = "Asset_" + asset.id;
                card.GetComponentInChildren<Text>(true).text = asset.displayName + "\n" +
                    (asset.category == "background" ? "Background" : "Object");
                card.onClick.AddListener(() => AssetSelected?.Invoke(asset));
                card.gameObject.SetActive(visibleCategory == null || asset.category == visibleCategory);
                cards.Add(asset.id, card); categories.Add(asset.id, asset.category);
            }
        }

        public void ShowCategory(string category)
        {
            visibleCategory = category;
            if (categoryHeading != null)
                categoryHeading.text = category == "background" ? "Choose a background" : "Add objects";
            foreach (var pair in cards)
                pair.Value.gameObject.SetActive(categories[pair.Key] == category);
            var scroll = GetComponent<ScrollRect>();
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
        }

        public void SetThumbnail(string id, Texture2D texture)
        {
            if (!cards.TryGetValue(id, out var card)) return;
            var image = card.GetComponentInChildren<RawImage>(true);
            image.texture = texture;
            image.color = Color.white;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        public void ShowStatus(string message, bool retry = false)
        {
            statusText.text = message;
            retryButton.gameObject.SetActive(retry);
        }
    }
}
