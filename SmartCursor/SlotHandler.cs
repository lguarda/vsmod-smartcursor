using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.API.Config;

namespace SmartCursor
{

    public delegate object SlotTransferDelegate(ItemSlot sourceSlot, ItemSlot targetSlot);

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

        public bool PushItem(List<ItemMatcher> matchers, HashSet<string> itemBlackList, SlotTransferDelegate transfer)
        {
            capi.ShowChatMessage($"PUSH ITEM 1");
            ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (matchers == null || matchers.Count == 0)
            {
                return false;
            }
            foreach (var matcher in matchers)
            {
                if (matcher.Matches(currentSlot))
                {
                    return false;
                }
                for (int j = 0; j < state.config.inventories.Length; j++)
                {
                    if (TransferMatchedItem(state.config.inventories[j], matcher, itemBlackList, transfer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TransferMatchedItem(string inventoryName, ItemMatcher matcher, HashSet<string> itemBlackList, SlotTransferDelegate transfer)
        {
            IInventory inventory = capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
            if (inventory == null)
            {
                return false;
            }
            int slotNumber = FindMatchingSlotInInventory(matcher, itemBlackList, inventory);
            if (slotNumber < 0)
            {
                return false;
            }
            savedSlotIndex = slotNumber;
            savedSlotInventoryName = inventoryName;
            savedActiveSlotIndex = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
            return TransferSavedSlot(transfer);
        }

        public bool TransferSavedSlot(SlotTransferDelegate transfer)
        {
            IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            IInventory inventory = capi.World.Player.InventoryManager.GetOwnInventory(savedSlotInventoryName);
            ItemSlot sourceSlot = hotbar[savedActiveSlotIndex];
            ItemSlot targetSlot = inventory[savedSlotIndex];

            object obj = transfer(sourceSlot, targetSlot);
            if (obj != null)
            {
                capi.Network.SendPacketClient(obj);
            }
            return true;
        }

        private int FindMatchingSlotInInventory(ItemMatcher matcher, HashSet<string> itemBlackList, IInventory inventory)
        {
            for (int i = 0; i < inventory.Count; i++) {
                ItemSlot slot = inventory[i];
                if (!itemBlackList.Contains(slot.GetStackName()) && matcher.Matches(slot)) {
                    return i;
                }
            }
            return -1;
        }

        public object FlipTransfer(ItemSlot sourceSlot, ItemSlot targetSlot)
        {
            return sourceSlot.Inventory.TryFlipItems(sourceSlot.Inventory.GetSlotId(sourceSlot), targetSlot);
        }

        public object TransferToTransfer(ItemSlot sourceSlot, ItemSlot targetSlot)
        {
            ItemStackMoveOperation op = new ItemStackMoveOperation(
                capi.World, EnumMouseButton.Left,
                EnumModifierKey.SHIFT,
                EnumMergePriority.AutoMerge, sourceSlot.StackSize)
            { ActingPlayer = capi.World.Player };
            return capi.World.Player.InventoryManager.TryTransferTo(sourceSlot, targetSlot, ref op);
        }
    }
}
