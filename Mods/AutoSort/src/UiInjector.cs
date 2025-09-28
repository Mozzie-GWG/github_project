using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace AutoSort
{
    public class UiInjector
    {
        ICoreClientAPI capi;
        AutoSortConfig cfg;

        public UiInjector(ICoreClientAPI capi, AutoSortConfig cfg)
        {
            this.capi = capi;
            this.cfg = cfg;
            capi.Event.GuiOpened += OnGuiOpened;
        }

        void OnGuiOpened(EnumGuiDialog dialogType, IGuiDialog dialog)
        {
            if (dialog is IGuiDialogInventory && dialog?.SingleComposer != null)
            {
                var invDialog = (IGuiDialogInventory)dialog;

                var comp = dialog.SingleComposer;
                var bounds = comp.Bounds;
                var btnBounds = ElementBounds.Fixed(bounds.fixedX + bounds.fixedWidth - 96, bounds.fixedY + 8, 88, 24);

                comp.AddSmallButton(
                    Lang.Get("autosort-button", "Auto-Sort"),
                    OnClickAutoSort,
                    btnBounds
                );

                comp.Compose();
            }
        }

        bool OnClickAutoSort()
        {
            var invs = ClientOpenInventories();
            foreach (var inv in invs)
            {
                InventorySorter.SortAndCompact(capi, inv, cfg);
            }

            if (cfg.SortPlayerInventoryAlso)
            {
                var pinv = capi.World.Player?.InventoryManager?.GetOwnInventory("player");
                if (pinv != null) InventorySorter.SortAndCompact(capi, pinv, cfg);
            }

            capi.World.Player?.Entity?.WalkInventory(inv => { inv.MarkDirty(); return true; });
            capi.ShowChatMessage(Lang.Get("Auto-sorted open inventories."));
            return true;
        }

        System.Collections.Generic.IEnumerable<IInventory> ClientOpenInventories()
        {
            var dialogs = capi.Gui.OpenedGuis?.ToArray() ?? new IGuiDialog[0];
            foreach (var dlg in dialogs)
            {
                if (dlg is IGuiDialogInventory invDlg)
                {
                    var inv = invDlg.Inventories?.FirstOrDefault();
                    if (inv != null) yield return inv;
                }
            }
        }
    }
}
