using System;
using System.Collections;
using System.Collections.Generic;
using Adeeb.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Adeeb.Services
{
    public sealed class RemoteAssetService : IDisposable
    {
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> loading = new HashSet<string>();
        private readonly HashSet<UnityWebRequest> requests = new HashSet<UnityWebRequest>();
        private bool disposed;
        public int DownloadCount { get; private set; }
        public int CacheHits { get; private set; }

        public IEnumerator LoadCatalog(string url, Action<AssetCatalog> success, Action<string> failure)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;
                requests.Add(request);
                yield return request.SendWebRequest();
                requests.Remove(request);
                if (disposed) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failure("Could not load the asset library. Check your connection and retry.");
                    yield break;
                }
                AssetCatalog catalog = null;
                string error = null;
                try
                {
                    catalog = JsonUtility.FromJson<AssetCatalog>(request.downloadHandler.text);
                    if (catalog?.assets == null || catalog.assets.Length == 0)
                        throw new FormatException("The asset library is empty.");
                    var ids = new HashSet<string>();
                    foreach (var asset in catalog.assets)
                    {
                        if (asset == null || string.IsNullOrWhiteSpace(asset.id) || !ids.Add(asset.id)
                            || string.IsNullOrWhiteSpace(asset.displayName)
                            || (asset.category != "background" && asset.category != "object")
                            || !Uri.TryCreate(asset.url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                            throw new FormatException("The asset library contains an invalid entry.");
                    }
                }
                catch (Exception ex) { error = ex.Message; }
                if (error != null) failure(error);
                else success(catalog);
            }
        }

        public IEnumerator LoadTexture(string url, Action<Texture2D> success, Action<string> failure)
        {
            while (!disposed && loading.Contains(url)) yield return null;
            if (disposed) yield break;
            if (textures.TryGetValue(url, out var cached))
            {
                CacheHits++;
                success(cached);
                yield break;
            }
            loading.Add(url);
            using (var request = UnityWebRequestTexture.GetTexture(url, true))
            {
                request.timeout = 30;
                requests.Add(request);
                yield return request.SendWebRequest();
                requests.Remove(request);
                loading.Remove(url);
                if (disposed) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failure("Image unavailable. Select it again to retry.");
                    yield break;
                }
                var texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.name = url;
                textures.Add(url, texture);
                DownloadCount++;
                success(texture);
            }
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var request in requests) request.Abort();
            requests.Clear();
            foreach (var texture in textures.Values) UnityEngine.Object.Destroy(texture);
            textures.Clear();
            loading.Clear();
        }
    }
}
