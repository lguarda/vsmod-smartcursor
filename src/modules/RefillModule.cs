using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.Client.NoObf;

namespace SmartCursor
{
    [ModModule]
    public class RefillModule : IModModule
    {
        private ICoreClientAPI capi;
        private ModStateManager state;
        private SlotHandler sh;

        private const int GRACE_PERIOS_MS = 200;

        private ItemStack[] prevSnapshot = new ItemStack[10];
        private int prevActiveSlot = -1;
        private long tickListenerId = -1;

        bool IsInventoryOpen()
        {
            foreach (var dlg in capi.Gui.OpenedGuis)
            {
                if (dlg.GetType().Name == "GuiDialogInventory")
                {
                    return true;
                }
            }
            return false;
        }

        bool MouseLock()
        {
            var invMgr = capi.World.Player.InventoryManager;
            bool dragging = invMgr.MouseItemSlot != null && !invMgr.MouseItemSlot.Empty;
            if (dragging)
            {
                return true;
            }
            // TODO i need to choose betwen both and maybe add lock timer
            ItemSlot currentSlot = invMgr.CurrentHoveredSlot;
            return currentSlot != null;
        }

        // I don't really like this but until i found reliable way
        // to get inventory event polltick is completely debunced and
        // refill seems to works ok
        /*
        void OnPollTick(float dt)
        {
            if (!state.config.slotRefill) {
                Dispose();
                return;
            }
            if (state.lockInv)
            {
                return;
            }
            if (capi.World?.Player == null)
                return;

            var invMgr = capi.World.Player.InventoryManager;

            var hotbar = invMgr.GetHotbarInventory();
            int active = invMgr.ActiveHotbarSlotNumber;
            bool activeSlotStable = active == prevActiveSlot;

            for (int i = 0; i < 10; i++)
            {
                var slot = hotbar[i];
                var cur = slot.Itemstack;
                var prev = prevSnapshot[i];

                // prev exist so was not empty and is a stackable item
                if (prev != null && (prev?.Collectible?.MaxStackSize ?? 0) > 1)
                {
                    // TODO some item get taken 2 by to or one by one
                    // bool isEmpty = cur == null || slot.StackSize <= 0;
                    // here choosing 1 for isempty mean when  there's still one item
                    // the refill kicks in
                    bool becameEmpty = cur == null || (slot.StackSize <= 1 && prev.StackSize > slot.StackSize);

                    bool eligible = becameEmpty && i == active && activeSlotStable && !MouseLock() && !IsInventoryOpen();

                    if (eligible && prev.Collectible.MaxStackSize > 1)
                    {
                        var elapsed = capi.World.ElapsedMilliseconds - state.unlockAt;
                        if (!state.lockInv && elapsed > GRACE_PERIOS_MS)
                        {
                            RunPumpLogic(i, prev.Collectible);
                        }
                    }
                }

                // always update, regardless of drag/active state, so tracking stays accurate
                prevSnapshot[i] = cur;
            }

            prevActiveSlot = active;
        }
        */
        void OnPollTick(float dt)
        {
            if (!state.config.slotRefill)
            {
                Dispose();
                return;
            }
            if (state.lockInv) return;
            if (capi.World?.Player == null) return;

            var invMgr = capi.World.Player.InventoryManager;
            var hotbar = invMgr.GetHotbarInventory();
            int active = invMgr.ActiveHotbarSlotNumber;
            bool activeSlotStable = active == prevActiveSlot;

            for (int i = 0; i < 10; i++)
            {
                ProcessSlot(hotbar[i], i, active, activeSlotStable);
            }

            prevActiveSlot = active;
        }

        void ProcessSlot(ItemSlot slot, int index, int activeSlot, bool activeSlotStable)
        {
            var cur = slot.Itemstack;
            var prev = prevSnapshot[index];

            if (activeSlotStable && IsStackableCollectible(prev))
            {
                bool becameEmpty = HasBecomeEmpty(slot, cur, prev);
                bool eligible = becameEmpty && index == activeSlot
                                && !MouseLock() && !IsInventoryOpen();

                if (eligible && prev.Collectible.MaxStackSize > 1 && IsGracePeriodElapsed())
                {
                    RunPumpLogic(index, prev.Collectible);
                }
            }

            // always update, regardless of drag/active state, so tracking stays accurate
            prevSnapshot[index] = cur;
        }

        bool IsStackableCollectible(ItemStack prev)
        {
            return prev != null && (prev?.Collectible?.MaxStackSize ?? 0) > 1;
        }

        bool HasBecomeEmpty(ItemSlot slot, ItemStack cur, ItemStack prev)
        {
            // TODO some item get taken 2 by 2 or one by one
            // here choosing 1 for isEmpty means when there's still one item
            // the refill kicks in
            return cur == null || (slot.StackSize <= 1 && prev.StackSize > slot.StackSize);
        }

        bool IsGracePeriodElapsed()
        {
            var elapsed = capi.World.ElapsedMilliseconds - state.unlockAt;
            return !state.lockInv && elapsed > GRACE_PERIOS_MS;
        }

        void RunPumpLogic(int slotIndex, CollectibleObject itemToRefill)
        {
            state.Lock();
            try
            {
                string path = itemToRefill?.Code?.Path;
                if (path != null)
                {
                    List<ItemMatcher> matchers = new List<ItemMatcher>();
                    matchers.Add(new ItemCodeMatcher(path));
                    var ms = sh.PushItem(matchers, state.config.itemBlackList, SlotHandlerCurrentSlotMethod.Ignore);
                    if (ms != null)
                    {
                        sh.TransferSavedSlot(ms, sh.TransferToTransfer);
                    }
                    // capi.ShowChatMessage($"Last Item was {itemToRefill?.Code?.Path}");
                }
            }
            finally
            {
                // TODO log error
                state.Unlock();
            }
        }

        private void HotKeyListener(string hotkeycode, KeyCombination keyComb)
        {
            switch (hotkeycode)
            {
                case "dropitem":
                case "dropitems":
                    capi.ShowChatMessage($"OMG");
                    state.Unlock();
                    break;
            }
        }

        public void Initialize(ICoreClientAPI api, ModStateManager stateManager)
        {
            capi = api;
            state = stateManager;
            if (!state.config.slotRefill)
            {
                return;
            }
            sh = new SlotHandler(capi, state);
            tickListenerId = capi.Event.RegisterGameTickListener(OnPollTick, 100);
            capi.Input.AddHotkeyListener(HotKeyListener);
        }

        public void Dispose()
        {
            if (tickListenerId >= 0)
            {
                capi.Event.UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
            }
        }

    }
}
