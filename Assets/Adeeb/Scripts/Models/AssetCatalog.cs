using System;

namespace Adeeb.Models
{
    [Serializable]
    public sealed class AssetCatalog
    {
        public AssetEntry[] assets;
    }

    [Serializable]
    public sealed class AssetEntry
    {
        public string id;
        public string displayName;
        public string category;
        public string url;
    }
}
