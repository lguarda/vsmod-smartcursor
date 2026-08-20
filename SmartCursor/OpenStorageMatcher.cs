
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {

// This matcher is a meta matcher
// it will check what's targeted in specific storage and make it availble for other matcher
public class OpenStorageSelector {

    // the following logic is take from decompiled BlockEntityShelf
    // unfortunately back shelf slot can't be focused and i don't want to change the behavior
    // it's a known limitation
    public static ItemSlot GetTargetedShelfSlot(BlockEntityShelf shelf, BlockSelection blockSel) {
        InventoryBase inv = shelf.Inventory;

        bool upper = blockSel.SelectionBoxIndex > 1;
        bool even = blockSel.SelectionBoxIndex % 2 == 0;

        // Determine the layout of the first slot in the selected area
        int baseSlot = upper ? 4 : 0;

        EnumShelvableLayout? layout = BlockEntityShelf.GetShelvableLayout(inv[baseSlot].Itemstack);

        if ((!layout.HasValue || layout != EnumShelvableLayout.SingleCenter) && !even) {
            layout = BlockEntityShelf.GetShelvableLayout(inv[baseSlot + 2].Itemstack);
        }

        int startSlot =
            baseSlot + ((!layout.HasValue || layout != EnumShelvableLayout.SingleCenter) ? (!even ? 2 : 0) : 0);

        bool hasSingleOrHalfLayout =
            layout.HasValue && (layout == EnumShelvableLayout.Halves || layout == EnumShelvableLayout.Quadrants);

        int slotCount = hasSingleOrHalfLayout ? 1 : 2;

        int endSlot = startSlot + slotCount;

        // Exactly what vanilla TryTake() does:
        // the item closest to the front wins.
        for (int slotId = endSlot - 1; slotId >= startSlot; slotId--) {
            if (!inv[slotId].Empty) {
                return inv[slotId];
            }
        }

        return null;
    }

    public static ItemSlot GetTargetedStorageItem(ICoreClientAPI capi) {
        BlockSelection sel = capi.World.Player.CurrentBlockSelection;
        var be = capi.World.BlockAccessor.GetBlockEntity(sel.Position);

        if (be is BlockEntityGroundStorage gs) {
            return gs.GetSlotAt(sel);
        } else if (be is BlockEntityShelf shelf) {
            return GetTargetedShelfSlot(shelf, sel);
        }
        return null;
    }
}
}
