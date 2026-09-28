using System;
using Adeeb.Models;
using Adeeb.Views;

namespace Adeeb.Controllers
{
    public sealed class PlaybackController : IDisposable
    {
        private readonly PlaybackView view;
        private ProjectData project;
        private int index;
        public PlaybackController(PlaybackView view)
        {
            this.view=view;
            view.PreviousRequested+=Previous; view.NextRequested+=Next;
        }
        public void Open(ProjectData data) { project=data; index=0; view.title.text=data.title+" — Preview"; Refresh(); }
        private void Previous() { if(index>0){index--;Refresh();} }
        private void Next() { if(index<project.pages.Count-1){index++;Refresh();} }
        private void Refresh() { view.page.Render(project.pages[index],false); view.ShowPage(index,project.pages.Count); }
        public void Dispose() { view.PreviousRequested-=Previous; view.NextRequested-=Next; }
    }
}
