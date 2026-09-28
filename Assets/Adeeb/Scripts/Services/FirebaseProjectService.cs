using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Adeeb.Models;
using UnityEngine;

namespace Adeeb.Services
{
    // All network completions return to Unity's main thread.
    public sealed class FirebaseProjectService : MonoBehaviour
    {
        public const string ProjectId = "adeeb-63981";
        public const string ApiKey = "AIzaSyDLhtdN0Of_uCXpOMFYmlEaTug2gTfWw-E";
        public string UserId { get; private set; }
        public bool IsReady => !string.IsNullOrEmpty(UserId);
        [Serializable] public sealed class Response
        {
            public string requestId, userId, error;
            public bool success;
            public ProjectData[] projects;
        }
        private readonly Dictionary<string, Action<Response>> pending = new Dictionary<string, Action<Response>>();
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AdeebFirebase_CancelTarget(string target);
        [DllImport("__Internal")] private static extern void AdeebFirebase_Request(string target, string requestId, string operation, string payload, string moduleUrl);
#endif
        public void Connect(Action<Response> completed) { Request("connect", "", completed); }
        public void LoadProjects(Action<Response> completed) { Request("list", "", completed); }
        public void Save(ProjectData project, Action<Response> completed)
        {
            string error = Validate(project);
            if (error != null) { completed(new Response { error = error }); return; }
            Request("save", JsonUtility.ToJson(project), completed);
        }
        public static string Validate(ProjectData project)
        {
            if (project == null || !Guid.TryParse(project.id, out _)) return "Invalid project ID.";
            if (string.IsNullOrWhiteSpace(project.title) || project.title.Length > 80) return "Enter a title of 1â€“80 characters.";
            if (project.pages == null || project.pages.Count == 0 || project.pages.Count > 100) return "A project must contain 1â€“100 pages.";
            var ids = new HashSet<string>();
            foreach (var page in project.pages)
            {
                if (page == null || !Guid.TryParse(page.id, out _) || !ids.Add(page.id)) return "Invalid or duplicate page ID.";
                if (string.IsNullOrEmpty(page.backgroundAssetId)) return "Choose a background for every page before saving.";
                if (page.objects == null || page.objects.Count > 200) return "A page can contain up to 200 objects.";
                foreach (var item in page.objects)
                {
                    if (item == null || !Guid.TryParse(item.id, out _) || !ids.Add(item.id) || string.IsNullOrEmpty(item.assetId))
                        return "Invalid or duplicate object.";
                    if (float.IsNaN(item.x) || float.IsNaN(item.y) || float.IsNaN(item.scale) ||
                        item.x < 0 || item.x > 1 || item.y < 0 || item.y > 1 || item.scale < .2f || item.scale > 3)
                        return "Invalid object position or scale.";
                }
            }
            return null;
        }
        private void Request(string operation, string payload, Action<Response> completed)
        {
            string id = Guid.NewGuid().ToString();
            pending.Add(id, response => { if (response.success && !string.IsNullOrEmpty(response.userId)) UserId = response.userId; completed(response); });
            StartCoroutine(Timeout(id));
#if UNITY_WEBGL && !UNITY_EDITOR
            AdeebFirebase_Request(gameObject.name, id, operation, payload, Application.streamingAssetsPath + "/Firebase/bridge.js");
#elif UNITY_EDITOR
            StartCoroutine(FirebaseEditorTransport.Execute(id, operation, payload, OnResponse));
#else
            Complete(new Response { requestId = id, error = "Firebase integration is configured for WebGL and the Unity Editor." });
#endif
        }
        private IEnumerator Timeout(string id)
        {
            yield return new WaitForSecondsRealtime(45);
            Complete(new Response { requestId = id, error = "Connection timed out. Check your connection and retry. A save may still finish; retrying uses the same project ID." });
        }
        // Called by the browser bridge through SendMessage.
        [UnityEngine.Scripting.Preserve]
        public void OnResponse(string json)
        {
            Response response;
            try { response = JsonUtility.FromJson<Response>(json); }
            catch (Exception) { return; }
            if (response != null) Complete(response);
        }
        private void Complete(Response response)
        {
            if (response.requestId == null || !pending.TryGetValue(response.requestId, out var callback)) return;
            pending.Remove(response.requestId);
            callback(response);
        }
        private void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AdeebFirebase_CancelTarget(gameObject.name);
#endif
            pending.Clear(); StopAllCoroutines();
        }
    }
}
