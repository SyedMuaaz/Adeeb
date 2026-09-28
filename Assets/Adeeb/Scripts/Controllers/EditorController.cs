using System;
using System.Collections.Generic;
using Adeeb.Models;
using Adeeb.Views;
using UnityEngine;

namespace Adeeb.Controllers
{
    public sealed class EditorController : IDisposable
    {
        private readonly EditorView view;
        private readonly IReadOnlyDictionary<string, AssetEntry> catalog;
        private ProjectData project;
        private int pageIndex;
        private string selectedId;
        public ProjectData Project => project;
        public int PageIndex => pageIndex;
        private PageData Page => project.pages[pageIndex];
        public EditorController(EditorView view, IReadOnlyDictionary<string, AssetEntry> catalog)
        {
            this.view=view; this.catalog=catalog;
            view.assetBrowser.AssetSelected += AddAsset;
            view.page.Selected += Select;
            view.page.Moved += Move;
            view.page.LoadFailed += ShowError;
            view.AddPageRequested += AddPage;
            view.PreviousRequested += Previous;
            view.NextRequested += Next;
            view.ScaleRequested += Scale;
            view.DeleteRequested += Delete;
            view.TitleChanged += Rename;
        }
        public void Open(ProjectData data)
        {
            if(project!=data) pageIndex=0;
            project=data;
            view.title.SetTextWithoutNotify(data.title);
            Refresh();
        }
        public void AddAsset(AssetEntry entry)
        {
            if(project==null) return; if(entry.category != "background" && string.IsNullOrEmpty(Page.backgroundAssetId)) return;
            if(entry.category=="background")
            {
                Page.backgroundAssetId=entry.id;
                Refresh();
            }
            else
            {
                var item=new PlacedObjectData {assetId=entry.id};
                Page.objects.Add(item);
                view.page.AddObject(item);
                Select(item.id);
            }
            view.status.text="Unsaved changes. Select Save to keep them in your library.";
        }
        private void AddPage() { MarkChanged(); project.pages.Add(new PageData()); pageIndex=project.pages.Count-1; Refresh(); }
        private void Previous() { if(pageIndex>0){pageIndex--;Refresh();} }
        private void Next() { if(pageIndex<project.pages.Count-1){pageIndex++;Refresh();} }
        private void Rename(string value)
        {
            MarkChanged();
            project.title=string.IsNullOrWhiteSpace(value)?"Untitled project":value.Trim();
            view.title.SetTextWithoutNotify(project.title);
        }
        private void Refresh()
        {
            view.assetBrowser.ShowCategory(string.IsNullOrEmpty(Page.backgroundAssetId) ? "background" : "object");
            view.page.Render(Page,true);
            view.ShowPage(pageIndex,project.pages.Count);
            Select(null);
        }
        private PlacedObjectData Selected => Page.objects.Find(item=>item.id==selectedId);
        private void Select(string id)
        {
            selectedId=id;
            view.page.Highlight(id);
            var item=Selected;
            view.ShowSelection(item!=null && catalog.TryGetValue(item.assetId,out var entry)?entry.displayName:null,item?.scale??1);
        }
        private void Move(string id,Vector2 position)
        {
            var item=Page.objects.Find(o=>o.id==id);
            if(item==null)return;
            MarkChanged(); item.x=position.x; item.y=position.y;
            view.page.UpdateObject(item);
        }
        private void Scale(float delta)
        {
            var item=Selected; if(item==null)return;
            MarkChanged(); item.scale=Mathf.Clamp(item.scale+delta,.2f,3f);
            view.page.UpdateObject(item); Select(item.id);
        }
        private void Delete()
        {
            var item=Selected; if(item==null)return;
            MarkChanged(); Page.objects.Remove(item); view.page.RemoveObject(item.id); Select(null);
        }
        private void MarkChanged() { view.status.text="Unsaved changes. Select Save to keep them in your library."; }
        private void ShowError(string error) { view.status.text=error; }
        public void Dispose()
        {
            view.assetBrowser.AssetSelected-=AddAsset; view.page.Selected-=Select; view.page.Moved-=Move;
            view.page.LoadFailed-=ShowError; view.AddPageRequested-=AddPage; view.PreviousRequested-=Previous;
            view.NextRequested-=Next; view.ScaleRequested-=Scale; view.DeleteRequested-=Delete; view.TitleChanged-=Rename;
        }
    }
}
