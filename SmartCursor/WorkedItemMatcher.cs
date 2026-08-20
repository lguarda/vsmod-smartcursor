using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {
public class WorkedItemMatcher {

    private static string GetWorkItem(BlockPos pos, ICoreClientAPI capi) {
        BlockEntity be = capi.World.BlockAccessor.GetBlockEntity(pos);
        if (be != null) {
            var workItemField = be.GetType().GetField("workItemStack", System.Reflection.BindingFlags.NonPublic |
                                                                           System.Reflection.BindingFlags.Instance);

            if (workItemField != null) {
                ItemStack workItem = workItemField.GetValue(be) as ItemStack;
                if (workItem != null) {
                    string path = workItem.Collectible.Code.Path; // Should contain clay type
                    return path;
                }
            }
        }
        return null;
    }

    // This is huge bull shilt
    private static string SelectItemFromWorkItem(string workItem) {
        switch (workItem) {
        case "clayworkitem-fire":
            return "clay-fire";
        case "clayworkitem-red":
            return "clay-red";
        case "clayworkitem-blue":
            return "clay-blue";
        default:
            return null;
        }
    }

    public static void Add(List<ItemMatcher> matchers, ICoreClientAPI capi) {
        BlockSelection bs = capi.World.Player.CurrentBlockSelection;

        string workItem = WorkedItemMatcher.GetWorkItem(bs.Position, capi);
        if (workItem != null) {
            string itemName = WorkedItemMatcher.SelectItemFromWorkItem(workItem);
            if (itemName != null) {
                matchers.Add(new ItemCodeMatcher(itemName));
            }
        }
    }
}
}
