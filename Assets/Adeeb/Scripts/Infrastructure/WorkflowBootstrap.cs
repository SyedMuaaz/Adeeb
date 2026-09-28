using System;
using System.Collections;
using System.Collections.Generic;
using Adeeb.Controllers;
using Adeeb.Models;
using Adeeb.Services;
using Adeeb.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Adeeb.Infrastructure
{
    public sealed class WorkflowBootstrap : MonoBehaviour
    {
        public LibraryView library;
        public EditorView editor;
        public PlaybackView playback;
        private RemoteAssetService assets;
        private FirebaseProjectService firebase;
        private LibraryController libraryController;
        private EditorController editorController;
        private PlaybackController playbackController;
        private readonly Dictionary<string,AssetEntry> catalog = new Dictionary<string,AssetEntry>();
        private readonly List<ProjectData> savedProjects = new List<ProjectData>();
        private bool loading, connecting, listing, saving, savedPlayback, reloadPending;
        public ProjectData CurrentProject => editorController?.Project;
        private void Start()
        {
            assets = new RemoteAssetService();
            firebase = gameObject.AddComponent<FirebaseProjectService>();
            libraryController = new LibraryController(library);
            editorController = new EditorController(editor,catalog);
            playbackController = new PlaybackController(playback);
            editor.page.Initialize(this,assets,catalog);
            playback.page.Initialize(this,assets,catalog);
            libraryController.OpenRequested += OpenEditor;
            library.RefreshRequested += RefreshLibrary;
            library.ProjectSelected += OpenSaved;
            editor.BackRequested += ShowLibrary;
            editor.PreviewRequested += ShowPreview;
            editor.SaveRequested += SaveProject;
            playback.BackRequested += ReturnFromPlayback;
            playback.page.LoadFailed += ShowPlaybackError;
            editor.assetBrowser.RetryRequested += LoadCatalog;
            ShowLibrary(); LoadCatalog(); Connect();
        }
        private void Connect()
        {
            if (connecting) return;
            connecting = true;
            library.connectionStatus.text = "Connecting to your library...";
            firebase.Connect(response => {
                connecting = false;
                if (!response.success) { library.connectionStatus.text = response.error + " Choose Refresh to retry."; return; }
                editor.save.interactable = true;
                LoadProjects();
            });
        }
        private void RefreshLibrary()
        {
            if (catalog.Count == 0) LoadCatalog();
            if (!firebase.IsReady) Connect(); else LoadProjects();
        }
        private void LoadProjects()
        {
            if (listing) { reloadPending = true; return; }
            listing = true; library.refreshButton.interactable = false;
            library.connectionStatus.text = "Loading saved stories...";
            firebase.LoadProjects(response => {
                listing = false; library.refreshButton.interactable = true;
                if (!response.success) { library.connectionStatus.text = response.error + " Choose Refresh to retry."; return; }
                savedProjects.Clear();
                int invalid = 0;
                foreach (var project in response.projects ?? Array.Empty<ProjectData>())
                    if (FirebaseProjectService.Validate(project) == null) savedProjects.Add(project); else invalid++;
                savedProjects.Sort((a,b) => string.Compare(a.title,b.title,StringComparison.OrdinalIgnoreCase));
                library.connectionStatus.text = savedProjects.Count == 0 ? "Connected. No saved stories yet." : savedProjects.Count + " saved stories";
                if (invalid > 0) library.connectionStatus.text += " Some invalid records were skipped.";
                RenderCovers(); if (reloadPending) { reloadPending = false; LoadProjects(); }
            });
        }
        private void RenderCovers()
        {
            if (catalog.Count > 0) library.ShowProjects(savedProjects,this,assets,catalog);
        }
        private void SaveProject()
        {
            if (saving || CurrentProject == null) return;
            if (!firebase.IsReady) { editor.status.text = "Not connected. Return to Main Menu and choose Refresh."; return; }
            CurrentProject.title = string.IsNullOrWhiteSpace(editor.title.text) ? "Untitled project" : editor.title.text.Trim();
            editor.title.SetTextWithoutNotify(CurrentProject.title);
            var validation = FirebaseProjectService.Validate(CurrentProject);
            if (validation != null) { editor.status.text = validation; return; }
            saving = true; editor.interaction.interactable = false;
            editor.status.text = "Saving...";
            firebase.Save(CurrentProject,response => {
                saving = false; editor.interaction.interactable = true;
                editor.status.text = response.success ? "Saved to your library." : "Save failed: " + response.error + " Select Save to retry.";
                if (response.success) LoadProjects();
            });
        }
        private void LoadCatalog()
        {
            if (loading) return;
            loading = true; editor.assetBrowser.ShowStatus("Loading assets...");
            StartCoroutine(assets.LoadCatalog("https://adeeb-63981.web.app/catalog.json",data => {
                loading = false;
                catalog.Clear(); foreach(var entry in data.assets) catalog.Add(entry.id,entry);
                editor.assetBrowser.ShowCatalog(data);
                editor.assetBrowser.ShowStatus("Select an item to add it to this page.");
                RenderCovers(); if (reloadPending) { reloadPending = false; LoadProjects(); }
                StartCoroutine(Thumbnails(data));
            },error => { loading = false; editor.assetBrowser.ShowStatus(error,true); library.connectionStatus.text = "Asset catalog unavailable. Choose Refresh to retry."; }));
        }
        private IEnumerator Thumbnails(AssetCatalog data)
        {
            foreach (var entry in data.assets)
                yield return assets.LoadTexture(entry.url,texture => editor.assetBrowser.SetThumbnail(entry.id,texture),
                    error => editor.assetBrowser.ShowStatus("Some images could not load. Select an asset to retry."));
        }
        private void ShowLibrary()
        {
            editor.gameObject.SetActive(false); playback.gameObject.SetActive(false); libraryController.Show();
        }
        private void OpenEditor(ProjectData project)
        {
            library.gameObject.SetActive(false); playback.gameObject.SetActive(false); editor.gameObject.SetActive(true);
            editorController.Open(project);
        }
        private void ShowPreview()
        {
            savedPlayback = false;
            editor.gameObject.SetActive(false); playback.gameObject.SetActive(true);
            playback.back.GetComponentInChildren<Text>(true).text = "Back to Editor";
            playbackController.Open(CurrentProject);
        }
        private void OpenSaved(ProjectData project)
        {
            savedPlayback = true;
            library.gameObject.SetActive(false); editor.gameObject.SetActive(false); playback.gameObject.SetActive(true);
            playback.back.GetComponentInChildren<Text>(true).text = "Main Menu";
            playbackController.Open(project); playback.title.text = project.title;
        }
        private void ReturnFromPlayback() { if (savedPlayback) ShowLibrary(); else OpenEditor(CurrentProject); }
        private void ShowPlaybackError(string error) { playback.title.text = "Image unavailable. Reopen this story to retry."; }
        private void OnDestroy()
        {
            if (libraryController != null) libraryController.OpenRequested -= OpenEditor;
            library.RefreshRequested -= RefreshLibrary; library.ProjectSelected -= OpenSaved;
            editor.BackRequested -= ShowLibrary; editor.PreviewRequested -= ShowPreview; editor.SaveRequested -= SaveProject;
            playback.BackRequested -= ReturnFromPlayback; playback.page.LoadFailed -= ShowPlaybackError;
            editor.assetBrowser.RetryRequested -= LoadCatalog;
            libraryController?.Dispose(); editorController?.Dispose(); playbackController?.Dispose();
            assets?.Dispose(); StopAllCoroutines();
        }
    }
}
