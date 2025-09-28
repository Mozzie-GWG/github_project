using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AutoSort
{
    public static class InventorySorter
    {
        public static void SortAndCompact(ICoreAPI api, IInventory inv, AutoSortConfig cfg)
        {
            if (inv == null) return;

            var slotMeta = new List<(int idx, ItemStack stack, bool locked)>();
            for (int i = 0; i < inv.Count; i++)
            {
                var slot = inv[i];
                if (slot?.Itemstack == null) continue;
                bool locked = false;
                slotMeta.Add((i, slot.Itemstack.Clone(), locked));
            }

            var merged = MergeStacks(api, slotMeta.Select(s => s.stack));

            var sorted = merged.OrderBy(IsPerishable)
                               .ThenBy(GetPerishSecondsLeft)
                               .ThenBy(GetGroupKey)
                               .ThenBy(stk => stk.GetName())
                               .ThenBy(GetWearLevel)
                               .ToList();

            for (int i = 0; i < inv.Count; i++) inv[i].Itemstack = null;

            int write = 0;
            foreach (var st in sorted)
            {
                if (write >= inv.Count) break;
                inv[write++].Itemstack = st;
            }

            inv.MarkDirty();
        }

        static List<ItemStack> MergeStacks(ICoreAPI api, IEnumerable<ItemStack> stacks)
        {
            var buckets = new List<ItemStack>();

            foreach (var next in stacks)
            {
                bool placed = false;

                for (int i = 0; i < buckets.Count; i++)
                {
                    var b = buckets[i];
                    if (b.Collectible == null || next.Collectible == null) continue;
                    if (b.Collectible.CanStackWith(api.World, b, next))
                    {
                        int room = b.Collectible.GetRemainingInventoryStackSize(b);
                        int move = GameMath.Clamp(next.StackSize, 0, room);
                        if (move > 0)
                        {
                            b.StackSize += move;
                            next.StackSize -= move;
                        }
                        if (next.StackSize <= 0)
                        {
                            placed = true;
                            break;
                        }
                    }
                }

                if (!placed && next.StackSize > 0)
                {
                    buckets.Add(next.Clone());
                }
            }

            return buckets;
        }

        static bool IsPerishable(ItemStack stack) => GetPerishSecondsLeft(stack) < double.MaxValue;

        static double GetPerishSecondsLeft(ItemStack stack)
        {
            try
            {
                ITreeAttribute trans = stack.Attributes?["transitionstate"] as ITreeAttribute;
                if (trans != null)
                {
                    if (trans.HasAttribute("freshHoursLeft"))
                        return trans.GetDouble("freshHoursLeft", double.MaxValue) * 3600d;
                    if (trans.HasAttribute("freshnessHoursLeft"))
                        return trans.GetDouble("freshnessHoursLeft", double.MaxValue) * 3600d;
                    if (trans.HasAttribute("freshnessSecondsLeft"))
                        return trans.GetDouble("freshnessSecondsLeft", double.MaxValue);
                }

                if (stack.Attributes?.HasAttribute("perishState") == true &&
                    stack.Attributes?.HasAttribute("perishHoursTotal") == true)
                {
                    double ratio = stack.Attributes.GetDouble("perishState", 0);
                    double total = stack.Attributes.GetDouble("perishHoursTotal", 0) * 3600d;
                    if (total > 0) return Math.Max(0, (1 - ratio) * total);
                }

                if (stack.Collectible?.Attributes?["perishable"].AsBool(false) == true)
                    return 1e12;

                return double.MaxValue;
            }
            catch
            {
                return double.MaxValue;
            }
        }

        static string GetGroupKey(ItemStack st)
        {
            var code = st.Collectible?.Code?.Path ?? "unknown";
            var parts = code.Split('-');
            if (parts.Length >= 2) return parts[0] + "-" + parts[1];
            return parts[0];
        }

        static int GetWearLevel(ItemStack st)
        {
            if (st.Collectible?.IsTool == true)
            {
                int max = st.Collectible.GetMaxDurability(st);
                if (max > 0) return max - st.Collectible.GetRemainingDurability(st);
            }
            return 0;
        }
    }
}
