using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace SmartCursor
{
    public class PipetteRule : AbstractRule
    {

        public PipetteRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) { }
        public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be,
                                 ItemStack item)
        {
            var path = block?.Code?.Path;
            if (path != null)
            {
                matchers.Add(new ItemCodeMatcher(path));
            }
        }
    }
}
