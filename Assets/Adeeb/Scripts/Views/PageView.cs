using System;
using System.Collections.Generic;
using Adeeb.Models;
using Adeeb.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class PageView : MonoBehaviour
    {
        public RawImage background;
        public RectTransform objectRoot;
        private readonly Dictionary<string, PlacedObjectView> objects = new Dictionary<string, PlacedObjectView>();
        private int revision;
        private MonoBehaviour runner;
        private RemoteAssetService service;
        private IReadOnlyDictionary<string, AssetEntry> catalog;
        private bool editable;
        public event Action<string> Selected;
        public event Action<string, Vector2> Moved;
        public event Action<string> LoadFailed;

        public void Initialize(MonoBehaviour host, RemoteAssetService assets, IReadOnlyDictionary<string, AssetEntry> entries)
        { runner = host; service = assets; catalog = entries; }

        public void Render(PageData page, bool canEdit)
        {
            int current = ++revision;
            editable = canEdit;
            foreach (var item in objects.Values) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            objects.Clear();
            background.texture = null;
            background.color = Color.white;
            if (!string.IsNullOrEmpty(page.backgroundAssetId) && catalog.TryGetValue(page.backgroundAssetId, out var entry))
                runner.StartCoroutine(service.LoadTexture(entry.url, texture =>
                {
                    if (this && current == revision) background.texture = texture;
                }, error => { if (this && current == revision) LoadFailed?.Invoke(error); }));
            foreach (var data in page.objects) AddObject(data);
        }

        public void AddObject(PlacedObjectData data)
        {
            int current = revision;
            var go = new GameObject("Object_" + data.id, typeof(RectTransform), typeof(RawImage), typeof(PlacedObjectView));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(objectRoot, false);
            var image = go.GetComponent<RawImage>();
            image.color = new Color(.72f,.78f,.83f,.7f);
            var view = go.GetComponent<PlacedObjectView>();
            view.Configure(data.id, objectRoot, editable);
            view.Selected += id => Selected?.Invoke(id);
            view.Moved += (id, position) => Moved?.Invoke(id, position);
            objects.Add(data.id, view);
            UpdateObject(data);
            if (catalog.TryGetValue(data.assetId, out var entry))
                runner.StartCoroutine(service.LoadTexture(entry.url, texture =>
                {
                    if (!this || current != revision || !view) return;
                    image.texture = texture;
                    image.color = Color.white;
                    UpdateObject(data);
                }, error => { if (this && current == revision && view) LoadFailed?.Invoke(error); }));
        }

        public void UpdateObject(PlacedObjectData data)
        {
            if (!objects.TryGetValue(data.id, out var view)) return;
            var rect = (RectTransform)view.transform;
            // Base height is 30% of the page; scale is independent of screen resolution.
            float aspect = view.GetComponent<RawImage>().texture is Texture texture ? (float)texture.width / texture.height : 1;
            float h = .3f * data.scale;
            float w = h * aspect / (16f/9f);
            rect.anchorMin = new Vector2(data.x-w/2, data.y-h/2);
            rect.anchorMax = new Vector2(data.x+w/2, data.y+h/2);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public void Highlight(string id)
        {
            foreach (var pair in objects)
            {
                var outline = pair.Value.GetComponent<Outline>();
                if (!outline) outline = pair.Value.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.1f,.55f,.85f,1);
                outline.effectDistance = new Vector2(3,3);
                outline.enabled = editable && pair.Key == id;
            }
        }

        public void RemoveObject(string id)
        {
            if (!objects.TryGetValue(id, out var view)) return;
            objects.Remove(id);
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
    }
}
