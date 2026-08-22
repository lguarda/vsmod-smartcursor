using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace SmartCursor
{

    public static class UseFlag
    {
        public static bool selfActionInProgress; // true only while OUR bag-put or pump code runs
    }

    public class RefillModule : IModModule
    {
        private ICoreClientAPI capi;
        private ModStateManager state;

        private long lockedAt;
        private const int GRACE_PERIOS_MS = 200;

        void OnHotbarSlotModified(int slotId)
        {
            if (state.Lock)
            {
                lockedAt = capi.World.ElapsedMilliseconds;
                return;
            }

            var elapsed = capi.World.ElapsedMilliseconds - lockedAt;
            if (elapsed < GRACE_PERIOS_MS) return;

            // Ignore in creative
            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                return;

            if (UseFlag.selfActionInProgress)
                return;

            var invMgr = capi.World.Player.InventoryManager;
            var activeIndex = invMgr.ActiveHotbarSlotNumber;
            if (slotId != activeIndex)
                return;

            capi.Event.EnqueueMainThreadTask(() =>
            {
                // Ignore if player is mid mouse-drag (grabbed item sitting on cursor)
                if (invMgr.MouseItemSlot != null && !invMgr.MouseItemSlot.Empty)
                    return;

                var slot = invMgr.GetHotbarInventory()[slotId];
                bool empty = slot.Itemstack == null || slot.StackSize == 0;
                if (empty)
                    RunPumpLogic(slotId);
            }, "smartcursor-slotcheck");
        }

        void RunPumpLogic(int slotIndex)
        {
            capi.ShowChatMessage($"OMG 6");
            UseFlag.selfActionInProgress = true;
            try
            {
                // scan inventory, TryPutInto the matching stack into hotbar[slotIndex]
            }
            finally
            {
                UseFlag.selfActionInProgress = false;
            }
        }

        void RunBagPutLogic(/* your hotkey handler args */)
        {
            UseFlag.selfActionInProgress = true;
            try
            {
                // your bag-put transfer here
            }
            finally
            {
                UseFlag.selfActionInProgress = false;
            }
        }

        void OnPlayerSpawn(IClientPlayer byPlayer)
        {
            byPlayer.InventoryManager.GetHotbarInventory().SlotModified += OnHotbarSlotModified;
        }

        public void Initialize(ICoreClientAPI api, ModStateManager stateManager)
        {
            capi.Event.PlayerEntitySpawn += OnPlayerSpawn;
            capi = api;
            state = stateManager;
        }

    }
}
