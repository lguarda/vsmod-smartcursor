using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace SmartCursor {

public static class UseFlag {
    public static bool SelfActionInProgress; // true only while OUR bag-put or pump code runs
}

public class RefillModule : IModModule {
    private ICoreClientAPI _capi;
    private ModStateManager _state;

    private long _lockedAt;
    private const int GracePeriosMs = 200;

    void OnHotbarSlotModified(int slotId) {
        if (_state.Lock) {
            _lockedAt = _capi.World.ElapsedMilliseconds;
            return;
        }

        var elapsed = _capi.World.ElapsedMilliseconds - _lockedAt;
        if (elapsed < GracePeriosMs) return;

        // Ignore in creative
        if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            return;

        if (UseFlag.SelfActionInProgress)
            return;

        var invMgr = _capi.World.Player.InventoryManager;
        var activeIndex = invMgr.ActiveHotbarSlotNumber;
        if (slotId != activeIndex)
            return;

        _capi.Event.EnqueueMainThreadTask(() => {
            // Ignore if player is mid mouse-drag (grabbed item sitting on cursor)
            if (invMgr.MouseItemSlot != null && !invMgr.MouseItemSlot.Empty)
                return;

            var slot = invMgr.GetHotbarInventory()[slotId];
            bool empty = slot.Itemstack == null || slot.StackSize == 0;
            if (empty)
                RunPumpLogic(slotId);
        }, "smartcursor-slotcheck");
    }

    void RunPumpLogic(int slotIndex) {
        _capi.ShowChatMessage($"OMG 6");
        UseFlag.SelfActionInProgress = true;
        try {
            // scan inventory, TryPutInto the matching stack into hotbar[slotIndex]
        } finally {
            UseFlag.SelfActionInProgress = false;
        }
    }

    void RunBagPutLogic(/* your hotkey handler args */) {
        UseFlag.SelfActionInProgress = true;
        try {
            // your bag-put transfer here
        } finally {
            UseFlag.SelfActionInProgress = false;
        }
    }

    void OnPlayerSpawn(IClientPlayer byPlayer) {
        byPlayer.InventoryManager.GetHotbarInventory().SlotModified += OnHotbarSlotModified;
    }

    public void Initialize(ICoreClientAPI capi, ModStateManager stateManager) {
        capi.Event.PlayerEntitySpawn += OnPlayerSpawn;
        _capi = capi;
        _state = stateManager;
    }

}
}
