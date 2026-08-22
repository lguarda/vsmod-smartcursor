
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace SmartCursor
{
    public class TroughRule : AbstractRule
    {

        public TroughRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) { }
        public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item)
        {
            if (block is BlockTroughDoubleBlock doubleTrough)
            {
                BlockPos rootPos = sel.Position.AddCopy(doubleTrough.RootOffset);
                be = capi.World.BlockAccessor.GetBlockEntity(rootPos);
            }
            if (be is BlockEntityTrough trough)
            {
                var slot = trough.Inventory[0];

                if (slot.Empty) {
                    // any grain any mash
                    matchers.Add(new ItemPathPartialMatcher("grain"));
                    matchers.Add(new ItemPathPartialMatcher("pressedmash"));
                    // Forced flax?
                    // matchers.Add(new ItemCodeMatcher("grain-flax"));
                }
                else {
                    string path = slot.Itemstack?.Collectible?.Code?.Path;
                    matchers.Add(new ItemCodeMatcher(path));
                }
            }
        }
    }
}
