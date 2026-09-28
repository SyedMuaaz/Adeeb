using Adeeb.Controllers;
using Adeeb.Services;
using Adeeb.Views;
using UnityEngine;

namespace Adeeb.Infrastructure
{
    [RequireComponent(typeof(SearchView))]
    public sealed class SearchBootstrap : MonoBehaviour
    {
        private RealtimeContentService service;
        private SearchController controller;
        public RealtimeContentService Service => service;
        public SearchController Controller => controller;
        private void Start()
        {
            var view = GetComponent<SearchView>(); view.Initialize();
            service = new RealtimeContentService();
            controller = new SearchController(this, view, service); controller.Open();
        }
        private void OnDestroy() { controller?.Dispose(); service?.Dispose(); StopAllCoroutines(); }
    }
}
