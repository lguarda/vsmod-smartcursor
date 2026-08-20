using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {

public class BlockCrockMatcher {

    public static void Add(List<ItemMatcher> matchers, ItemSlot slot, ICoreClientAPI capi) {
        ItemStack stack = slot?.Itemstack;
        if (stack == null)
            return;

        if (stack.Collectible is BlockCrock crock) {
            bool hasFood = stack.Attributes.GetDecimal("quantityServings", 0) > 0;

            bool sealedCrock = stack.Attributes.GetBool("sealed", false);

            bool canBeSealed = hasFood && !sealedCrock;

            // TODO make these configurable
            if (canBeSealed) {
                matchers.Add(new ItemCodeMatcher("beeswax"));
            }
            if (hasFood) {
                matchers.Add(new ItemPathPartialMatcher("bowl"));
            }
        }
    }
}
}
