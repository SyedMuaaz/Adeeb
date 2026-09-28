using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Adeeb.Services
{
    /// <summary>Task 2 read-only transport. No dependency on Task 1's Firestore services.</summary>
    public sealed class RealtimeContentService : IDisposable
    {
        public const string DatabaseUrl = "https://adeeb-63981-default-rtdb.firebaseio.com";
        private const string ApiKey = "AIzaSyDLhtdN0Of_uCXpOMFYmlEaTug2gTfWw-E";
        private string token, refreshToken;
        private DateTime expires;
        private UnityWebRequest activeRequest;
        private bool disposed;
        public ContentParseResult Cached { get; private set; }
        public int DownloadCount { get; private set; }

        public IEnumerator Load(bool refresh, Action<ContentParseResult, string> completed)
        {
            if (!refresh && Cached != null) { completed(Cached, null); yield break; }
            string error = null;
            if (string.IsNullOrEmpty(token) || DateTime.UtcNow >= expires)
            {
                string response = null;
                bool renewing = !string.IsNullOrEmpty(refreshToken);
                string url = renewing ? "https://securetoken.googleapis.com/v1/token?key=" + ApiKey :
                    "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + ApiKey;
                string body = renewing ? new JObject { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken }.ToString(Formatting.None) : "{\"returnSecureToken\":true}";
                yield return Send(url, body, (data, failure) => { response = data; error = failure; });
                if (disposed) yield break;
                if (error != null) { completed(null, error); yield break; }
                try
                {
                    var auth = JObject.Parse(response);
                    token = (string)auth[renewing ? "id_token" : "idToken"];
                    refreshToken = (string)auth[renewing ? "refresh_token" : "refreshToken"];
                    if (string.IsNullOrEmpty(token)) throw new JsonException();
                    expires = DateTime.UtcNow.AddMinutes(50);
                }
                catch (JsonException) { completed(null, "The sign-in response was invalid. Select Refresh to retry."); yield break; }
            }
            string json = null;
            yield return Send(DatabaseUrl + "/StoryLibary.json?auth=" + Uri.EscapeDataString(token), null,
                (data, failure) => { json = data; error = failure; });
            if (disposed) yield break;
            if (error != null) { completed(null, error); yield break; }
            DownloadCount++;
            var parsed = new ContentParseResult();
            yield return CoverInfoParser.Parse(json, parsed);
            if (disposed) yield break;
            if (parsed.Error != null) { completed(null, parsed.Error); yield break; }
            Cached = parsed;
            completed(parsed, null);
        }

        private IEnumerator Send(string url, string body, Action<string, string> completed)
        {
            using (var request = new UnityWebRequest(url, body == null ? "GET" : "POST"))
            {
                activeRequest = request;
                request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 25;
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                yield return request.SendWebRequest();
                activeRequest = null;
                if (disposed) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (request.responseCode == 401) { token = null; expires = DateTime.MinValue; }
                    // Never log the request URL: it contains a short-lived authentication token.
                    completed(null, request.responseCode == 401 || request.responseCode == 403 ?
                        "Library access was denied. Check authentication and database rules, then Refresh." :
                        "Could not load the library. Check your connection and select Refresh to retry.");
                }
                else completed(request.downloadHandler.text, null);
            }
        }

        public void Dispose() { disposed = true; activeRequest?.Abort(); Cached = null; token = refreshToken = null; }
    }
}
