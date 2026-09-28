using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class PlacedObjectView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        public string InstanceId { get; private set; }
        private RectTransform page;
        private RectTransform rect;
        private bool editable;
        private Vector2 dragOffset;
        public event Action<string> Selected;
        public event Action<string, Vector2> Moved;

        public void Configure(string id, RectTransform pageRect, bool canEdit)
        {
            InstanceId = id;
            page = pageRect;
            rect = (RectTransform)transform;
            editable = canEdit;
            GetComponent<RawImage>().raycastTarget = canEdit;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (editable) Selected?.Invoke(InstanceId);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!editable) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(page, data.position, data.pressEventCamera, out var p);
            dragOffset = (Vector2)rect.localPosition - p;
        }

        public void OnDrag(PointerEventData data)
        {
            if (!editable || page.rect.width <= 0 || page.rect.height <= 0) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, data.position, data.pressEventCamera, out var p)) return;
            p += dragOffset;
            var normalized = new Vector2(Mathf.Clamp01((p.x - page.rect.xMin) / page.rect.width),
                Mathf.Clamp01((p.y - page.rect.yMin) / page.rect.height));
            Moved?.Invoke(InstanceId, normalized);
        }
    }
}
