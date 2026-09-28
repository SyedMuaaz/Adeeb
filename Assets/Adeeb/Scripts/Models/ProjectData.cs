using System;
using System.Collections.Generic;

namespace Adeeb.Models
{
    [Serializable]
    public sealed class ProjectData
    {
        public string id = Guid.NewGuid().ToString();
        public string title = "Untitled project";
        public List<PageData> pages = new List<PageData> { new PageData() };
    }

    [Serializable]
    public sealed class PageData
    {
        public string id = Guid.NewGuid().ToString();
        public string backgroundAssetId;
        public List<PlacedObjectData> objects = new List<PlacedObjectData>();
    }

    [Serializable]
    public sealed class PlacedObjectData
    {
        public string id = Guid.NewGuid().ToString();
        public string assetId;
        public float x = .5f;
        public float y = .5f;
        public float scale = 1;
    }
}
