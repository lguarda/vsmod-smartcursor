using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace SmartCursor
{
    public class TorchRule : AbstractRule
    {

        public TorchRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) { }
        public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be,
                                 ItemStack item)
        {
            // could use more generic rule with
            // string blockCode = block?.Code?.Path;
            // == torch-basic-extinct-up
            if (block is BlockTorch bt)
            {
                if (bt.IsExtinct)
                {
                    matchers.Add(new ItemCodeMatcher("torch-basic-lit-up"));
                    matchers.Add(new ItemCodeMatcher("firestarter"));
                }
            }
        }
    }
}
