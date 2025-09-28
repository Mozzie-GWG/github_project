using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AutoSort
{
    public class ModSystemAutoSort : ModSystem
    {
        ICoreClientAPI capi;
        AutoSortConfig cfg;
        UiInjector injector;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            cfg = AutoSortConfig.Load(api);

            api.RegisterCommand("autosort", "Sort player + open container inventories", "[none]", OnCmdAutoSort, Privilege.chat);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            injector = new UiInjector(capi, cfg);

            capi.Input.RegisterHotKey("autosortKey", "Auto-Sort open inventories", GlKeys.K, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("autosortKey", OnHotkey);
        }

        bool OnHotkey(KeyCombination comb)
        {
            DoSort();
            return true;
        }

        void OnCmdAutoSort(int groupId, CmdArgs args)
        {
            DoSort();
            capi?.ShowChatMessage("Auto-sorted.");
        }

        void DoSort()
        {
            if (capi == null) return;

            foreach (var dlg in capi.Gui.OpenedGuis)
            {
                if (dlg is IGuiDialogInventory invDlg)
                {
                    foreach (var inv in invDlg.Inventories)
                    {
                        InventorySorter.SortAndCompact(capi, inv, cfg);
                    }
                }
            }

            if (cfg.SortPlayerInventoryAlso)
            {
                var pinv = capi.World.Player?.InventoryManager?.GetOwnInventory("player");
                if (pinv != null) InventorySorter.SortAndCompact(capi, pinv, cfg);
            }
        }
    }
}
