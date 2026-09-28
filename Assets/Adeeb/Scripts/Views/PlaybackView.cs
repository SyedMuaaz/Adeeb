using System;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Views
{
    public sealed class PlaybackView : MonoBehaviour
    {
        public PageView page;
        public Text title, pageLabel;
        public Button back, previous, next;
        public event Action BackRequested, PreviousRequested, NextRequested;
        private void Awake()
        {
            back.onClick.AddListener(() => BackRequested?.Invoke());
            previous.onClick.AddListener(() => PreviousRequested?.Invoke());
            next.onClick.AddListener(() => NextRequested?.Invoke());
        }
        public void ShowPage(int index, int count)
        {
            pageLabel.text = "Page " + (index+1) + " of " + count;
            previous.interactable = index > 0;
            next.interactable = index < count-1;
        }
    }
}
