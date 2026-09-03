using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SmartCursor
{

    [ModModule]
    public class TransferAwayModule : IModModule
    {
        private ICoreClientAPI capi;
        private ModStateManager state;

        private bool OnPutInBagHotkeyPressed(KeyCombination keyComb)
        {
            state.Lock();
            PutItemInInventory(capi.World.Player.InventoryManager.ActiveHotbarSlot);
            state.Unlock();
            return true;
        }

        public void Initialize(ICoreClientAPI api, ModStateManager stateManager)
        {
            capi = api;
            state = stateManager;
            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_PUTITINTHEBAG, GlKeys.B,
                                                 OnPutInBagHotkeyPressed);
        }

        private bool PutItemInInventory(ItemSlot sourceSlot)
        {
            if (sourceSlot == null || sourceSlot.Empty)
                return false;

            ItemStackMoveOperation op = new ItemStackMoveOperation(
                capi.World, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.AutoMerge,
                sourceSlot.StackSize)
            { ActingPlayer = capi.World.Player };

            object[] packets = capi.World.Player.InventoryManager.TryTransferAway(
                sourceSlot, ref op, onlyPlayerInventory: true, slotNotifyEffect: true);

            if (packets != null && packets.Length > 0)
            {
                foreach (object packet in packets)
                {
                    if (packet != null)
                    {
                        capi.Network.SendPacketClient(packet);
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
