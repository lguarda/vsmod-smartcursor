using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.GameContent;
using System.Linq;

namespace SmartCursor
{
    public class CrockRule : AbstractRule
    {
        public CrockRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) { }

        public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be,
                                 ItemStack item)
        {
            if (item == null)
                return;

            if (item.Collectible is BlockCrock crock)
            {
                bool hasFood = item.Attributes.GetDecimal("quantityServings", 0) > 0;

                bool sealedCrock = item.Attributes.GetBool("sealed", false);

                bool canBeSealed = hasFood && !sealedCrock;

                // TODO make these configurable
                if (canBeSealed)
                {
                    matchers.Add(new ItemCodeMatcher("beeswax"));
                }
                if (hasFood)
                {
                    matchers.Add(new ItemPathPartialMatcher("bowl"));
                }
            }
        }
    }
}
