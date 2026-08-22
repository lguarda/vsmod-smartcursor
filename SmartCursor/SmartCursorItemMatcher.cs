using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.MathTools;

namespace SmartCursor
{
    public abstract class ItemMatcher
    {
        public abstract bool Matches(ItemSlot slot);
    }

    public class ToolTypeMatcher : ItemMatcher
    {
        private readonly EnumTool toolType;

        public ToolTypeMatcher(EnumTool toolType) { this.toolType = toolType; }

        public override bool Matches(ItemSlot slot) { return slot?.Itemstack?.Collectible?.Tool == toolType; }
    }

    public class ItemCodeMatcher : ItemMatcher
    {
        private readonly string itemCode;

        public ItemCodeMatcher(string itemCode) { this.itemCode = itemCode; }

        public override bool Matches(ItemSlot slot) { return slot?.Itemstack?.Collectible?.Code?.Path == itemCode; }
    }

    public class ItemPathPartialMatcher : ItemMatcher
    {
        private readonly string itemPath;

        public ItemPathPartialMatcher(string itemPath) { this.itemPath = itemPath; }

        public override bool Matches(ItemSlot slot)
        {
            return slot?.Itemstack?.Collectible?.Code?.Path?.StartsWith(itemPath) ?? false;
        }
    }

    public class BuildStageMaterialMatcher : ItemMatcher
    {
        private readonly BuildStageMaterial[] materials;

        public BuildStageMaterialMatcher(BuildStageMaterial[] materials) { this.materials = materials; }

        public override bool Matches(ItemSlot slot)
        {
            if (slot?.Itemstack == null)
            {
                return false;
            }

            foreach (var mat in materials)
            {
                if (mat.ItemStack.Satisfies(slot.Itemstack))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
