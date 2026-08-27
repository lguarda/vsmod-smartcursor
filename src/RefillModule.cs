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

        // I don't really like this but until i found reliable way
        // to get inventory event polltick is completely debunced and
        // refill seems to works ok
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

                // prev exist so was not empty and is a stackable item
                if (prev != null && (prev?.Collectible?.MaxStackSize ?? 0) > 1) {
                    // TODO some item get taken 2 by to or one by one
                    // bool isEmpty = cur == null || slot.StackSize <= 0;
                    // here choosing 1 for isempty mean when  there's still one item
                    // the refill kicks in
                    bool becameEmpty = cur == null || slot.StackSize <= 1;

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
                    var ms = sh.PushItem(matchers, state.config.itemBlackList, SlotHandlerCurrentSlotMethod.Ignore);
                    if (ms != null) {
                        sh.TransferSavedSlot(ms, sh.TransferToTransfer);
                    }
                    // capi.ShowChatMessage($"Last Item was {itemToRefill?.Code?.Path}");
                }
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
