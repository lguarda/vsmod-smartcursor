using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.API.Config;

namespace SmartCursor {

public delegate object SlotTransferDelegate(ItemSlot sourceSlot, ItemSlot targetSlot);

public class SlotHandler {

    ICoreClientAPI _capi;
    private ModStateManager _state;

    int _savedSlotIndex;
    string _savedSlotInventoryName;
    int _savedActiveSlotIndex;

    public SlotHandler(ICoreClientAPI capi, ModStateManager stateManager) {
        _state = stateManager;
        _capi = capi;
    }

    public bool PushItem(List<ItemMatcher> matchers, HashSet<string> itemBlackList, SlotTransferDelegate transfer) {
        _capi.ShowChatMessage($"OMG 2");
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        if (matchers == null || matchers.Count == 0) {
            _capi.ShowChatMessage($"OMG 3");
            return false;
        }
        foreach (var matcher in matchers) {
            if (matcher.Matches(currentSlot)) {
                _capi.ShowChatMessage($"OMG 4");
                return false;
            }
            for (int j = 0; j < _state.config.inventories.Length; j++) {
                if (TransferMatchedItem(_state.config.inventories[j], matcher, itemBlackList, transfer)) {
                    _capi.ShowChatMessage($"OMG 5");
                    return true;
                }
            }
        }
        return false;
    }

    private bool TransferMatchedItem(string inventoryName, ItemMatcher matcher, HashSet<string> itemBlackList, SlotTransferDelegate transfer) {
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
        if (inventory == null) {
            return false;
        }
        int slotNumber = FindMatchingSlotInInventory(matcher, itemBlackList, inventory);
        if (slotNumber < 0) {
            return false;
        }
        _savedSlotIndex = slotNumber;
        _savedSlotInventoryName = inventoryName;
        _savedActiveSlotIndex = _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
        return TransferSavedSlot(transfer);
    }

    public bool TransferSavedSlot(SlotTransferDelegate transfer) {
        IInventory hotbar = _capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(_savedSlotInventoryName);
        ItemSlot sourceSlot = hotbar[_savedActiveSlotIndex];
        ItemSlot targetSlot = inventory[_savedSlotIndex];

        object obj = transfer(sourceSlot, targetSlot);
        if (obj != null) {
            _capi.Network.SendPacketClient(obj);
        }
        return true;
    }

    private bool isItemBlackListed(ItemSlot item) {
        // ex: "Tin bronze pickaxe" since it's needed fot the quest
        return _state.config.itemBlackList.Contains(item.GetStackName());
    }

    private int FindMatchingSlotInInventory(ItemMatcher matcher, HashSet<string> itemBlackList, IInventory inventory) {
        for (int i = 0; i < inventory.Count; i++) {
            ItemSlot slot = inventory[i];
            if (!isItemBlackListed(slot) && matcher.Matches(slot)) {
                return i;
            }
        }
        return -1;
    }

    public object FlipTransfer(ItemSlot sourceSlot, ItemSlot targetSlot) {
        return sourceSlot.Inventory.TryFlipItems(sourceSlot.Inventory.GetSlotId(sourceSlot), targetSlot);
    }

    public object TransferToTransfer(ItemSlot sourceSlot, ItemSlot targetSlot) {
        ItemStackMoveOperation op = new ItemStackMoveOperation(
            _capi.World, EnumMouseButton.Left,
            EnumModifierKey.SHIFT,
            EnumMergePriority.AutoMerge, sourceSlot.StackSize) { ActingPlayer = _capi.World.Player };
        return _capi.World.Player.InventoryManager.TryTransferTo(sourceSlot, targetSlot, ref op);
    }
}
}
