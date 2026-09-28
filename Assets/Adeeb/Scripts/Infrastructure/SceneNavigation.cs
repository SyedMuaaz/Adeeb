using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Adeeb.Infrastructure
{
    public sealed class SceneNavigation : MonoBehaviour
    {
        private bool navigating;
        private Button toolkitBack;
        private void Start()
        {
            var document = GetComponent<UIDocument>();
            if (document == null) return;
            toolkitBack = document.rootVisualElement.Q<Button>("back-menu");
            if (toolkitBack != null) toolkitBack.clicked += OpenMenu;
        }
        public void OpenTask1() => Open("Adeeb");
        public void OpenTask2() => Open("Search");
        public void OpenMenu() => Open("Menu");
        private void Open(string scene)
        {
            if (navigating) return;
            navigating = true;
            SceneManager.LoadSceneAsync(scene);
        }
        private void OnDestroy()
        {
            if (toolkitBack != null) toolkitBack.clicked -= OpenMenu;
        }
    }
}
