#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Adeeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Adeeb.Services
{
    // Editor-only adapter. No Admin credentials: requests obey the same owner rules as WebGL.
    internal static class FirebaseEditorTransport
    {
        private const string SessionKey = "Adeeb.Firebase.Editor.RefreshToken";
        private static string token, userId;
        private static DateTime expires;
        private static bool authenticating;
        public static IEnumerator Execute(string id, string operation, string payload, Action<string> completed)
        {
            string error = null;
            yield return Authenticate(value => error = value);
            if (error != null) { Reply(id, false, error, null, completed); yield break; }
            var root = "https://firestore.googleapis.com/v1/projects/" + FirebaseProjectService.ProjectId + "/databases/(default)/documents/users/" + userId + "/projects";
            if (operation == "connect") { Reply(id, true, null, null, completed); yield break; }
            if (operation == "save")
            {
                JObject project = JObject.Parse(payload);
                yield return Send(root + "/" + (string)project["id"], "PATCH", new JObject { ["fields"] = EncodeFields(project) }.ToString(Formatting.None),
                    true, value => { }, value => error = value);
                Reply(id, error == null, error, null, completed);
            }
            else if (operation == "list")
            {
                var projects = new List<ProjectData>();
                string pageToken = null;
                do
                {
                    JObject response = null;
                    yield return Send(root + "?pageSize=100" + (pageToken == null ? "" : "&pageToken=" + Uri.EscapeDataString(pageToken)),
                        "GET", null, true, value => response = value, value => error = value);
                    if (error != null) break;
                    foreach (var document in response["documents"] as JArray ?? new JArray())
                    {
                        var decoded = DecodeFields(document["fields"] as JObject ?? new JObject());
                        var project = JsonUtility.FromJson<ProjectData>(decoded.ToString(Formatting.None));
                        if (FirebaseProjectService.Validate(project) == null) projects.Add(project);
                    }
                    pageToken = (string)response["nextPageToken"];
                } while (!string.IsNullOrEmpty(pageToken));
                Reply(id, error == null, error, projects.ToArray(), completed);
            }
            else Reply(id, false, "Unknown Firebase operation.", null, completed);
        }
        private static IEnumerator Authenticate(Action<string> failed)
        {
            while (authenticating) yield return null;
            if (token != null && DateTime.UtcNow < expires) yield break;
            authenticating = true;
            try
            {
                string refresh = EditorPrefs.GetString(SessionKey, "");
                JObject response = null;
                string error = null;
                if (!string.IsNullOrEmpty(refresh))
                {
                    yield return Send("https://securetoken.googleapis.com/v1/token?key=" + FirebaseProjectService.ApiKey, "POST",
                        new JObject { ["grant_type"] = "refresh_token", ["refresh_token"] = refresh }.ToString(Formatting.None),
                        false, value => response = value, value => error = value);
                    if (error != null) { failed("Sign-in failed. Check your connection or the Editor anonymous account. " + error); yield break; }
                    token = (string)response["id_token"]; userId = (string)response["user_id"];
                    EditorPrefs.SetString(SessionKey, (string)response["refresh_token"]);
                }
                else
                {
                    yield return Send("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + FirebaseProjectService.ApiKey, "POST",
                        "{\"returnSecureToken\":true}", false, value => response = value, value => error = value);
                    if (error != null) { failed(error); yield break; }
                    token = (string)response["idToken"]; userId = (string)response["localId"];
                    EditorPrefs.SetString(SessionKey, (string)response["refreshToken"]);
                }
                expires = DateTime.UtcNow.AddMinutes(50);
            }
            finally { authenticating = false; }
        }
        private static IEnumerator Send(string url, string method, string body, bool authenticated, Action<JObject> success, Action<string> failed)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 25;
                if (body != null) { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); request.SetRequestHeader("Content-Type", "application/json"); }
                if (authenticated) request.SetRequestHeader("Authorization", "Bearer " + token);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = "Firebase request failed (" + request.responseCode + ").";
                    try { message = (string)JObject.Parse(request.downloadHandler.text)["error"]?["message"] ?? message; } catch (JsonException) { }
                    failed(message); yield break;
                }
                JObject response;
                try { response = JObject.Parse(request.downloadHandler.text); }
                catch (JsonException) { failed("Firebase returned an unreadable response."); yield break; }
                success(response);
            }
        }
        private static JObject EncodeFields(JObject value)
        {
            var result = new JObject();
            foreach (var property in value.Properties()) result[property.Name] = Encode(property.Value);
            return result;
        }
        private static JObject Encode(JToken value)
        {
            if (value is JObject obj) return new JObject { ["mapValue"] = new JObject { ["fields"] = EncodeFields(obj) } };
            if (value is JArray array) { var values = new JArray(); foreach (var item in array) values.Add(Encode(item)); return new JObject { ["arrayValue"] = new JObject { ["values"] = values } }; }
            if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float) return new JObject { ["doubleValue"] = value.Value<double>() };
            if (value.Type == JTokenType.Null) return new JObject { ["nullValue"] = JValue.CreateNull() };
            return new JObject { ["stringValue"] = value.Value<string>() };
        }
        private static JObject DecodeFields(JObject fields)
        {
            var result = new JObject();
            foreach (var property in fields.Properties()) result[property.Name] = Decode(property.Value);
            return result;
        }
        private static JToken Decode(JToken value)
        {
            if (value["mapValue"] != null) return DecodeFields(value["mapValue"]["fields"] as JObject ?? new JObject());
            if (value["arrayValue"] != null) { var array = new JArray(); foreach (var item in value["arrayValue"]["values"] as JArray ?? new JArray()) array.Add(Decode(item)); return array; }
            return value["stringValue"] ?? value["doubleValue"] ?? value["integerValue"] ?? JValue.CreateNull();
        }
        private static void Reply(string id, bool success, string error, ProjectData[] projects, Action<string> completed)
        {
            completed(JsonUtility.ToJson(new FirebaseProjectService.Response { requestId = id, success = success, error = error, userId = userId, projects = projects }));
        }
    }
}
#endif
