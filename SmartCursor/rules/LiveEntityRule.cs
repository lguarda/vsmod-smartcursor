using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace SmartCursor
{
    public class LiveEntityRule : AbstractRule
    {
        public LiveEntityRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) { }
        public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item)
        {
            EntitySelection es = capi.World.Player.CurrentEntitySelection;

            if (es != null)
            {
                Entity entity = es.Entity;
                // capi.ShowChatMessage($"Entity {entity.GetName()} {!entity.Alive}");
                if (!entity.Alive)
                {
                    matchers.Add(new ToolTypeMatcher(EnumTool.Knife));
                }
            }
        }
    }
}
