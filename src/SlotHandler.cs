using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.API.Config;

namespace SmartCursor
{

    public delegate object SlotTransferDelegate(ItemSlot sourceSlot, ItemSlot targetSlot);
    public class MatchedSlot {
        public int index;
        public string inventoryName;
    }

    public enum SlotHandlerCurrentSlotMethod
    {
        Ignore,
        Stop
    }

    public class SlotHandler
    {

        ICoreClientAPI capi;
        private ModStateManager state;

        int savedSlotIndex;
        string savedSlotInventoryName;
        public int savedActiveSlotIndex;

        public SlotHandler(ICoreClientAPI api, ModStateManager stateManager)
        {
            state = stateManager;
            capi = api;
        }

        public MatchedSlot PushItem(List<ItemMatcher> matchers, HashSet<string> itemBlackList, SlotHandlerCurrentSlotMethod method = SlotHandlerCurrentSlotMethod.Stop)
        {
            ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (matchers == null || matchers.Count == 0)
            {
                return null;
            }
            foreach (var matcher in matchers)
            {
                if (matcher.Matches(currentSlot))
                {
                    if (method == SlotHandlerCurrentSlotMethod.Stop) {
                        return null;
                    }

                }
                for (int j = 0; j < state.config.inventories.Length; j++)
                {
                    MatchedSlot slot = FindMatchingSlotInInventory(state.config.inventories[j], matcher, itemBlackList);
                    if (slot != null)
                    {
                        return slot;
                    }
                }
            }
            return null;
        }

        private MatchedSlot FindMatchingSlotInInventory(string inventoryName, ItemMatcher matcher, HashSet<string> itemBlackList)
        {
            IInventory inventory = capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
            if (inventory == null)
            {
                return null;
            }
            int slotNumber = FindInventoryIndex(matcher, itemBlackList, inventory);
            if (slotNumber < 0)
            {
                return null;
            }
            return new MatchedSlot { index = slotNumber, inventoryName = inventoryName };
        }

        private int FindInventoryIndex(ItemMatcher matcher, HashSet<string> itemBlackList, IInventory inventory)
        {
            ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemSlot slot = inventory[i];
                if (inventory.ClassName == "hotbar" && slot == currentSlot) {
                    continue;
                }
                if ((itemBlackList == null || !itemBlackList.Contains(slot.GetStackName())) && matcher.Matches(slot))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool TransferSavedSlot(MatchedSlot ms, SlotTransferDelegate transfer) {
            savedSlotIndex = ms.index;
            savedSlotInventoryName = ms.inventoryName;
            savedActiveSlotIndex = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
            return TransferSavedSlot(transfer);
        }

        public bool TransferSavedSlot(SlotTransferDelegate transfer)
        {
            IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            IInventory inventory = capi.World.Player.InventoryManager.GetOwnInventory(savedSlotInventoryName);
            ItemSlot targetSlot = hotbar[savedActiveSlotIndex];
            ItemSlot sourceSlot = inventory[savedSlotIndex];

            SmartCursorUtils.Log(capi, $" {inventory[savedSlotIndex]?.StackSize} ");
            object obj = transfer(sourceSlot, targetSlot);
            if (obj != null)
            {
                capi.Network.SendPacketClient(obj);
                sourceSlot.MarkDirty();
                targetSlot.MarkDirty();
                SmartCursorUtils.Log(capi, $"AFTER: source.StackSize={sourceSlot?.Itemstack?.StackSize}, target.StackSize={targetSlot?.Itemstack?.StackSize}");
            } else {
                SmartCursorUtils.Log(capi, $" tranfer fail");
            }
            return true;
        }

        public object FlipTransfer(ItemSlot sourceSlot, ItemSlot targetSlot)
        {
            return sourceSlot.Inventory.TryFlipItems(sourceSlot.Inventory.GetSlotId(sourceSlot), targetSlot);
        }

        public object TransferToTransfer(ItemSlot sourceSlot, ItemSlot targetSlot)
        {
            ItemStackMoveOperation op = new ItemStackMoveOperation(
                capi.World, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.DirectMerge,
                sourceSlot.StackSize)
            { ActingPlayer = capi.World.Player };
#if DEBUG
            capi.Logger.Debug($"[MergeDebug] sourceSlot stackSize: {sourceSlot.StackSize}, itemstack: {sourceSlot.Itemstack?.Collectible?.Code}");
            capi.Logger.Debug($"[MergeDebug] targetSlot stackSize: {targetSlot.StackSize}, itemstack: {targetSlot.Itemstack?.Collectible?.Code}");
            capi.Logger.Debug($"[MergeDebug] sourceSlot.CanTake: {sourceSlot.CanTake()}");
            capi.Logger.Debug($"[MergeDebug] targetSlot.CanHold: {targetSlot.CanHold(sourceSlot)}");
#endif
            var result = capi.World.Player.InventoryManager.TryTransferTo(sourceSlot, targetSlot, ref op);
#if DEBUG
            capi.Logger.Debug($"[MergeDebug] TryTransferTo result: {result}, MovedQuantity: {op.MovedQuantity}, RequestedQuantity: {op.RequestedQuantity}");
            if (result == null)
            {
                capi.Logger.Debug("[MergeDebug] result is null — likely inventory rejected the move at InventoryManager level (permission/type mismatch/no matching inventory found).");
            }
#endif


            return result;
        }
    }
}
