using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SmartCursor {

public class TransferAwayModule : IModModule {
    private ICoreClientAPI _capi;
    private ModStateManager _state;


    private bool OnPutInBagHotkeyPressed(KeyCombination keyComb) {
        _state.Lock = true;
        PutItemInInventory(_capi.World.Player.InventoryManager.ActiveHotbarSlot);
        _state.Lock = false;
        return true;
    }

    public void Initialize(ICoreClientAPI capi, ModStateManager stateManager) {
        _capi = capi;
        _state = stateManager;
        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_PUTITINTHEBAG, GlKeys.B,
                                             OnPutInBagHotkeyPressed);
    }

    private bool PutItemInInventory(ItemSlot sourceSlot) {
        if (sourceSlot == null || sourceSlot.Empty)
            return false;

        ItemStackMoveOperation op = new ItemStackMoveOperation(
            _capi.World, EnumMouseButton.Left,
            EnumModifierKey.SHIFT,
            EnumMergePriority.AutoMerge, sourceSlot.StackSize) { ActingPlayer = _capi.World.Player };

        object[] packets = _capi.World.Player.InventoryManager.TryTransferAway(
            sourceSlot, ref op, onlyPlayerInventory: true, slotNotifyEffect: true);

        if (packets != null && packets.Length > 0) {
            foreach (object packet in packets) {
                if (packet != null) {
                    _capi.Network.SendPacketClient(packet);
                }
            }
            // Im not 100% convince this is needed
            sourceSlot.MarkDirty();
            return true;
        }

        return false;
    }
}
}
