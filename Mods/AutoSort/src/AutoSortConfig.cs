using Vintagestory.API.Common;

namespace AutoSort
{
    public class AutoSortConfig
    {
        public bool SortPlayerInventoryAlso = true;
        public bool RespectLockedSlots = true;
        public bool MergeAcrossBackpackGrids = true;
        public bool MoveEmptyBagsLeft = true;

        public static AutoSortConfig Load(ICoreAPI api)
        {
            var conf = api.LoadModConfig<AutoSortConfig>("autosort.json");
            if (conf == null)
            {
                conf = new AutoSortConfig();
                api.StoreModConfig(conf, "autosort.json");
            }
            return conf;
        }
    }
}
