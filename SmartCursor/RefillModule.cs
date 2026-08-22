using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.Client.NoObf;

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
        private SlotHandler sh;

        private long lockedAt;
        private const int GRACE_PERIOS_MS = 200;

        private ItemStack[] prevSnapshot = new ItemStack[10];
        private int prevActiveSlot = -1;
        private long tickListenerId;

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

        void OnPollTick(float dt)
        {
            if (UseFlag.selfActionInProgress)
                return;
            if (capi.World?.Player == null)
                return;

            var invMgr = capi.World.Player.InventoryManager;
            bool dragging = invMgr.MouseItemSlot != null && !invMgr.MouseItemSlot.Empty;

            var hotbar = invMgr.GetHotbarInventory();
            int active = invMgr.ActiveHotbarSlotNumber;
            bool activeSlotStable = active == prevActiveSlot;

            for (int i = 0; i < 10; i++)
            {
                var slot = hotbar[i];
                var cur = slot.Itemstack;
                var prev = prevSnapshot[i];

                bool wasEmpty = prev == null;
                bool isEmpty = cur == null || slot.StackSize == 0;

                bool becameEmpty = !wasEmpty && isEmpty;
                bool eligible = becameEmpty && i == active && activeSlotStable && !dragging && !IsInventoryOpen();

                if (eligible && prev.Collectible.MaxStackSize > 1)
                {

                    var elapsed = capi.World.ElapsedMilliseconds - lockedAt;
                    if (state.Lock)
                    {
                        lockedAt = capi.World.ElapsedMilliseconds;
                    }
                    else if (elapsed > GRACE_PERIOS_MS)
                    {
                        RunPumpLogic(i, prev.Collectible);
                    }
                }

                // always update, regardless of drag/active state, so tracking stays accurate
                prevSnapshot[i] = cur;
            }

            prevActiveSlot = active;
        }

        void RunPumpLogic(int slotIndex, CollectibleObject itemToRefill)
        {
            UseFlag.selfActionInProgress = true;
            try
            {
                string path = itemToRefill?.Code?.Path;
                if (path != null)
                {
                    List<ItemMatcher> matchers = new List<ItemMatcher>();
                    matchers.Add(new ItemCodeMatcher(path));
                    sh.PushItem(matchers, state.config.itemBlackList, sh.FlipTransfer);
                }
                // scan inventory, TryPutInto the matching stack into hotbar[slotIndex]
            }
            finally
            {
                UseFlag.selfActionInProgress = false;
            }
        }

        void OnPlayerSpawn(IClientPlayer byPlayer)
        {
            tickListenerId = capi.Event.RegisterGameTickListener(OnPollTick, 100);
        }

        public void Initialize(ICoreClientAPI api, ModStateManager stateManager)
        {
            capi = api;
            state = stateManager;
            sh = new SlotHandler(capi, state);
            capi.Event.PlayerEntitySpawn += OnPlayerSpawn;
        }
    }
}
