using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {
public class ClayFormingRule : AbstractRule {
    public ClayFormingRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) {}

    private string GetWorkItem(BlockPos pos) {
        BlockEntity be = _capi.World.BlockAccessor.GetBlockEntity(pos);
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
    private string SelectItemFromWorkItem(string workItem) {
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

    public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item) {
        if (sel == null) {
            return ;
        }
        string workItem = GetWorkItem(sel.Position);
        if (workItem != null) {
            string itemName = SelectItemFromWorkItem(workItem);
            if (itemName != null) {
                matchers.Add(new ItemCodeMatcher(itemName));
            }
        }
    }
}
}
