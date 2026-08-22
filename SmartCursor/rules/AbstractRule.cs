using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace SmartCursor
{
    public abstract class AbstractRule
    {
        protected ICoreClientAPI capi;

        public AbstractRule(SmartCursorConfig config, ICoreClientAPI api)
        {
            capi = api;
            Setup(config);
        }

        // return a string of the item status, it should reflect change if matcher function should be reran
        public virtual string BuildSignature(BlockSelection sel, Block block, BlockEntity be) { return null; }

        public virtual void Setup(SmartCursorConfig config) { }

        // this function will be called each time item under cursor has changed
        // so here check if current matcher is related and add item matcher to the list accordingly
        public abstract void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item);
    }
}
