using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace SmartCursor
{

    public class SmartCursorModule : IModModule
    {

        ICoreClientAPI capi;
        bool isSmartToolHeld;
        bool isToggleMode;
        long listener = -1;
        private ModStateManager state;
        private SlotHandler sh;

        List<AbstractRule> rules = new List<AbstractRule>();

        string previousBlockCode;

        private void HotKeyListener(string hotkeycode, KeyCombination keyComb)
        {
            switch (hotkeycode)
            {
                case SmartCursorKeybind.HOTKEY_SMARTCURSOR_PIPETTE:
                    StartPipette();
                    break;
                case SmartCursorKeybind.HOTKEY_SMARTCURSOR:
                    StartSmartCursor(false);
                    break;
                case SmartCursorKeybind.HOTKEY_SMARTCURSOR_TOGGLE:
                    StartSmartCursor(true);
                    break;
                case SmartCursorKeybind.HOTKEY_SMARTCURSOR_ONE_SHOT:
                    PushItem();
                    break;
                case SmartCursorKeybind.HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE:
                    BlackListItem();
                    break;
            }
        }

        public void Initialize(ICoreClientAPI api, ModStateManager stateManager)
        {
            isSmartToolHeld = false;
            state = stateManager;
            capi = api;
            sh = new SlotHandler(capi, state);

            rules.Add(new LiveEntityRule(state.config, capi));
            if (state.config.extendedRule)
            {
                // it work let's go
                //rules.Add(new PipetteRule(state.config, capi));
                rules.Add(new TorchRule(state.config, capi));
                rules.Add(new PitKilnRule(state.config, capi));
                rules.Add(new BloomeryRule(state.config, capi));
                rules.Add(new CrockRule(state.config, capi));
                rules.Add(new ClayFormingRule(state.config, capi));
                rules.Add(new TroughRule(state.config, capi));
            }
            rules.Add(new ToolRule(state.config, capi));

            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE, GlKeys.R,
                                                 null, true, true);
            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR, GlKeys.R);
            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_TOGGLE, GlKeys.R, null, true);
            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_ONE_SHOT, GlKeys.Unknown);
            SmartCursorKeybind.RegisterClientKey(capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_PIPETTE, GlKeys.Unknown);
            capi.Input.AddHotkeyListener(HotKeyListener);
        }

        private string GetCurrentSelectionSignature(List<AbstractRule> rules_list)
        {
            BlockSelection bs = capi.World.Player.CurrentBlockSelection;
            if (bs == null)
            {
                return null;
            }

            Block block = capi.World.BlockAccessor.GetBlock(bs.Position);
            if (block == null)
            {
                return null;
            }
            string blockCode = block?.Code?.Path;

            var be = capi.World.BlockAccessor.GetBlockEntity(bs.Position);

            foreach (var rule in rules_list)
            {
                string signature = rule.BuildSignature(bs, block, be);
                if (signature != null)
                {
                    return signature;
                }
            }

            if (be is BlockEntityGroundStorage gs)
            {
                var hc = new HashCode();
                foreach (var slot in gs.Inventory)
                {
                    // Add each slot in order
                    hc.Add(slot.Empty ? 0 : slot.Itemstack.Collectible.Code.GetHashCode());
                }
                return hc.ToHashCode().ToString();
            }

            return blockCode;
        }

        private bool SmartToolReload()
        {
            string selSign = GetCurrentSelectionSignature(rules);
#if DEBUG
            SmartCursorUtils.Log(capi, $"Target Signature{selSign}");
#endif
            ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;

            bool selectionChanged = selSign != previousBlockCode;
            bool handDepleted = isSmartToolHeld && currentSlot.Empty;

            if (!selectionChanged && !handDepleted)
            {
                return false;
            }

            previousBlockCode = selSign;

            if (handDepleted)
            {
                // restore whatever was in the slot before our last swap, then re-evaluate fresh
                PopTool();
                currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            }

            // here remove this with new slothandler
            List<ItemMatcher> matchers = BuildMatcherList(rules);
            if (matchers.Count > 0 && !IsRightItem2(currentSlot, matchers))
            {
                PopTool(); // no-op if handDepleted branch already popped
                isSmartToolHeld = PushItem();
                return true;
            }

            return false;
        }
        private void SmartToolStopListListener(float t)
        {
            if (!isToggleMode)
            {
                // When not in toggle mode and hotkey was released pop tool
                if (!capi.Input.IsHotKeyPressed(SmartCursorKeybind.HOTKEY_SMARTCURSOR))
                {
                    PopTool();
                    UnregisterSmartToolStopListListener();
                    return;
                }

                // When continuousMode enabled and reload was done stop here
                if (state.config.continuousMode && SmartToolReload())
                {
                    return;
                }
            }

            // When player take item pop tools to avoid weird item movement
            // TODO find better solution is there any event?
            ItemSlot mouseItemSlot = capi.World.Player.InventoryManager.MouseItemSlot;
            if (!mouseItemSlot.Empty)
            {
                PopTool();
                UnregisterSmartToolStopListListener();
                return;
            }

            // To avoid confusion when active bar change disable the smart tool
            int currentActiveSlotIndex = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
            if (currentActiveSlotIndex != sh.savedActiveSlotIndex)
            {
                PopTool();
                UnregisterSmartToolStopListListener();
                return;
            }
        }

        private bool IsRightItem2(ItemSlot slot, List<ItemMatcher> matchers)
        {
            foreach (var matcher in matchers)
            {
                if (!IsItemBlackListed(slot) && matcher.Matches(slot))
                {
                    return true;
                }
            }
            return false;
        }

        bool IsItemBlackListed(ItemSlot item)
        {
            // ex: "Tin bronze pickaxe" since it's needed fot the quest
            return state.config.itemBlackList.Contains(item.GetStackName());
        }

        private void BlackListItem()
        {
            ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (!currentSlot.Empty)
            {
                string name = currentSlot.GetStackName();
                if (state.config.itemBlackList.Contains(name))
                {
                    state.config.itemBlackList.Remove(name);
                    capi.ShowChatMessage($"Removed from blacklist: {name}");
                }
                else
                {
                    state.config.itemBlackList.Add(name);
                    capi.ShowChatMessage($"Added to Blacklist: {name}");
                }
                state.SaveConfig();
            }
        }

        private List<ItemMatcher> BuildMatcherList(List<AbstractRule> rules_list)
        {
            List<ItemMatcher> matchers = new List<ItemMatcher>();

            BlockSelection bs = capi.World.Player.CurrentBlockSelection;

            Block block = bs != null ? capi.World.BlockAccessor.GetBlock(bs.Position) : null;
            BlockEntity be = bs != null ? capi.World.BlockAccessor.GetBlockEntity(bs.Position) : null;
            var slot = OpenStorageSelector.GetTargetedStorageItem(capi);
            ItemStack stack = slot?.Itemstack;

            foreach (var rule in rules_list)
            {
                rule.Run(matchers, bs, block, be, stack);
            }

            return matchers;
        }

        private bool PushItem()
        {
            List<ItemMatcher> matchers = BuildMatcherList(rules);
            var ms = sh.PushItem(matchers, state.config.itemBlackList);
            if (ms == null) {
                return false;
            }
            return sh.TransferSavedSlot(ms, sh.FlipTransfer);
        }

        private bool Pipette()
        {
            List<ItemMatcher> matchers = new List<ItemMatcher>();
            AbstractRule pipetteRule = new PipetteRule(state.config, capi);

            BlockSelection bs = capi.World.Player.CurrentBlockSelection;
            Block block = bs != null ? capi.World.BlockAccessor.GetBlock(bs.Position) : null;
            //pipetteRule.Run(matchers, bs, block, null, null);
            pipetteRule.Run(matchers, null, block, null, null);

            var ms = sh.PushItem(matchers, null);
            if (ms == null) {
                return false;
            }
            return sh.TransferSavedSlot(ms, sh.FlipTransfer);
        }



        private void UnregisterSmartToolStopListListener()
        {
            if (listener >= 0)
            {
                capi.Event.UnregisterGameTickListener(listener);
                listener = -1;
            }
        }
        private void PopTool()
        {
            if (isSmartToolHeld)
            {
                isSmartToolHeld = false;
                sh.TransferSavedSlot(sh.FlipTransfer);
            }
        }


        private void StartSmartCursor(bool mode)
        {
 #if DEBUG
             BlockSelection bs = capi.World.Player.CurrentBlockSelection;
             var es = capi.World.Player.CurrentEntitySelection?.Entity;
             if (bs != null)
             {
                 Block block = capi.World.BlockAccessor.GetBlock(bs.Position);
                 SmartCursorUtils.Log(capi, $"Target block code {block?.Code}");
             }
             if (es != null) {
                 SmartCursorUtils.Log(capi,
                     $"Entity: {es.Code}, " +
                     $"Type: {es.GetType().FullName}, " +
                     $"Id: {es.EntityId}"
                 );
             }
             SmartCursorUtils.ShowHeldItemCode(capi);
 #endif
             isToggleMode = mode;
             if (!isSmartToolHeld)
             {
                 UnregisterSmartToolStopListListener();
                 listener = capi.Event.RegisterGameTickListener(SmartToolStopListListener, 100);
                 isSmartToolHeld = PushItem();
             }
             else if (isToggleMode)
             {
                 PopTool();
                 UnregisterSmartToolStopListListener();
             }
        }

        private void StartPipette()
        {
            isToggleMode = true;
            UnregisterSmartToolStopListListener();
            if (isSmartToolHeld)
            {
                PopTool();
            }
            listener = capi.Event.RegisterGameTickListener(SmartToolStopListListener, 100);
            isSmartToolHeld = Pipette();
        }
    }
}
