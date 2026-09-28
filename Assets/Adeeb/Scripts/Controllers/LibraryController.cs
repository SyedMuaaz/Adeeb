using System;
using Adeeb.Models;
using Adeeb.Views;
using UnityEngine.UI;

namespace Adeeb.Controllers
{
    public sealed class LibraryController : IDisposable
    {
        private readonly LibraryView view;
        private ProjectData draft;
        private bool replacePending;
        public event Action<ProjectData> OpenRequested;
        public LibraryController(LibraryView view)
        {
            this.view=view;
            view.CreateRequested+=Create;
            view.ResumeRequested+=Resume;
        }
        public void Show()
        {
            replacePending=false;
            view.createButton.GetComponentInChildren<Text>(true).text="Create New";
            view.Show(draft!=null);
        }
        private void Create()
        {
            if(draft!=null && !replacePending)
            {
                replacePending=true;
                view.message.text="Creating a new project will discard this unsaved draft. Choose Create New again to confirm, or Resume Draft.";
                return;
            }
            draft=new ProjectData(); replacePending=false; OpenRequested?.Invoke(draft);
        }
        private void Resume() { if(draft!=null) OpenRequested?.Invoke(draft); }
        public void Dispose() { view.CreateRequested-=Create; view.ResumeRequested-=Resume; }
    }
}
