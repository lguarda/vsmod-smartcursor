using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;

using Vintagestory.API.MathTools;

namespace SmartCursor {

public class SmartCursorModSystem : ModSystem {

    const string CONFIG_PATH = "smartcursor.json";
    const string HOTKEY_SMARTCURSOR = "smartcursor";
    const string HOTKEY_SMARTCURSOR_TOGGLE = "smartcursor toggle";
    const string HOTKEY_SMARTCURSOR_ONE_SHOT = "smartcursor one shot";
    const string HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE = "smartcursor blacklist toggle";

    ICoreClientAPI _capi;
    int _savedSlotIndex;
    string _savedSlotInventoryName;
    int _savedActiveSlotIndex;
    bool _isSmartToolHeld;
    bool _isToggleMode;
    long _listener = -1;

    List<AbstractRule> rules = new List<AbstractRule>();

    string _previousBlockCode;

    SmartCursorConfig _config;
    Dictionary<EnumBlockMaterial, EnumTool[]> _materialTools;
    Dictionary<string, EnumTool[]> _domainTools;

    private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
        switch (hotkeycode) {
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR:
            StartSmartCursor(false);
            break;
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR_TOGGLE:
            StartSmartCursor(true);
            break;
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR_ONE_SHOT:
            PushTool();
            break;
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE:
            BlackListItem();
            break;
        }
    }

    private void SaveConfig(string path) { _capi.StoreModConfig(_config, path); }

    private void LoadConfig(string path) {
        try {
            _config = _capi.LoadModConfig<SmartCursorConfig>(path);
        } catch (Exception) {
            _config = null;
        }
        if (_config == null) {
            _config = new SmartCursorConfig();
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("SmartCursor starting");
        _isSmartToolHeld = false;
        _capi = api;

        LoadConfig(CONFIG_PATH);
        SaveConfig(CONFIG_PATH);

        rules.Add(new LiveEntityRule(_config, api));
        if (_config.extended_rule) {
            rules.Add(new TorchRule(_config, api));
            rules.Add(new PitKilnRule(_config, api));
            rules.Add(new BloomeryRule(_config, api));
            rules.Add(new CrockRule(_config, api));
            rules.Add(new ClayFormingRule(_config, api));
        }
        rules.Add(new ToolRule(_config, api));

        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_BLACKLIST_TOGGLE, GlKeys.R,
                                             true, true);
        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR, GlKeys.R);
        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_TOGGLE, GlKeys.R, true);
        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_ONE_SHOT, GlKeys.Unknown);
        _capi.Input.AddHotkeyListener(HotKeyListener);
    }

    private string GetCurrentSelectionSignature() {
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;
        if (bs == null) {
            return null;
        }

        Block block = _capi.World.BlockAccessor.GetBlock(bs.Position);
        if (block == null) {
            return null;
        }
        string blockCode = block?.Code?.Path;

        var be = _capi.World.BlockAccessor.GetBlockEntity(bs.Position);

        foreach (var rule in rules) {
            string signature = rule.BuildSignature(bs, block, be);
            if (signature != null) {
                return signature;
            }
        }

        if (be is BlockEntityGroundStorage gs) {
            var hc = new HashCode();
            foreach (var slot in gs.Inventory) {
                // Add each slot in order
                hc.Add(slot.Empty ? 0 : slot.Itemstack.Collectible.Code.GetHashCode());
            }
            return hc.ToHashCode().ToString();
        }

        return blockCode;
    }

    private bool SmartToolReload() {
        string selSign = GetCurrentSelectionSignature();
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;

        bool selectionChanged = selSign != _previousBlockCode;
        bool handDepleted = _isSmartToolHeld && currentSlot.Empty;

        if (!selectionChanged && !handDepleted) {
            return false;
        }

        _previousBlockCode = selSign;

        if (handDepleted) {
            // restore whatever was in the slot before our last swap, then re-evaluate fresh
            PopTool();
            currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        }

        List<ItemMatcher> matchers = BuildMatcherList();
        if (matchers.Count > 0 && !IsRightItem2(currentSlot, matchers)) {
            PopTool(); // no-op if handDepleted branch already popped
            _isSmartToolHeld = PushTool();
            return true;
        }

        return false;
    }
    private void SmartToolStopListListener(float t) {
        if (!_isToggleMode) {

            // When not in toggle mode and hotkey was released pop tool
            if (!_capi.Input.IsHotKeyPressed(SmartCursorKeybind.HOTKEY_SMARTCURSOR)) {
                PopTool();
                UnregisterSmartToolStopListListener();
                return;
            }

            // When continuousMode enabled and reload was done stop here
            if (_config.continuousMode && SmartToolReload()) {
                return;
            }
        }

        // When player take item pop tools to avoid weird item movement
        // TODO find better solution is there any event?
        ItemSlot mouseItemSlot = _capi.World.Player.InventoryManager.MouseItemSlot;
        if (!mouseItemSlot.Empty) {
            PopTool();
            UnregisterSmartToolStopListListener();
            return;
        }

        // To avoid confusion when active bar change disable the smart tool
        int currentActiveSlotIndex = _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
        if (currentActiveSlotIndex != _savedActiveSlotIndex) {
            PopTool();
            UnregisterSmartToolStopListListener();
            return;
        }
    }

    private bool SwapItemSlot() {
        IInventory hotbar = _capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(_savedSlotInventoryName);
        object obj = hotbar.TryFlipItems(_savedActiveSlotIndex, inventory[_savedSlotIndex]);
        if (obj != null) {
            _capi.Network.SendPacketClient(obj);
        }

        return true;
    }

    private bool IsRightItem(ItemSlot slot, ItemMatcher matcher) {
        return !isItemBlackListed(slot) && matcher.Matches(slot);
    }

    private bool IsRightItem2(ItemSlot slot, List<ItemMatcher> matchers) {
        foreach (var matcher in matchers) {
            if (!isItemBlackListed(slot) && matcher.Matches(slot)) {
                return true;
            }
        }
        return false;
    }

    private bool SwapItemSlotSaved(string inventoryName, int slotNumber) {
        if (slotNumber < 0) {
            return false;
        }

        _savedSlotIndex = slotNumber;
        _savedSlotInventoryName = inventoryName;
        _savedActiveSlotIndex = _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;

        return SwapItemSlot();
    }

    bool isItemBlackListed(ItemSlot item) {
        // ex: "Tin bronze pickaxe" since it's needed fot the quest
        return _config.itemBlackList.Contains(item.GetStackName());
    }

    private void BlackListItem() {
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        if (!currentSlot.Empty) {
            string name = currentSlot.GetStackName();
            if (_config.itemBlackList.Contains(name)) {
                _config.itemBlackList.Remove(name);
                _capi.ShowChatMessage($"Removed from blacklist: {name}");
            } else {
                _config.itemBlackList.Add(name);
                _capi.ShowChatMessage($"Added to Blacklist: {name}");
            }
            SaveConfig(CONFIG_PATH);
        }
    }

    private int FindToolSlotInInventory(ItemMatcher matcher, IInventory inventory) {
        for (int i = 0; i < inventory.Count; i++) {
            if (IsRightItem(inventory[i], matcher)) {
                return i;
            }
        }
        return -1;
    }

    private bool SwapItemName(string inventoryName, ItemMatcher matcher) {
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
        if (inventory == null) {
            return false;
        }

        int slotNumber = FindToolSlotInInventory(matcher, inventory);

        return SwapItemSlotSaved(inventoryName, slotNumber);
    }

    private List<ItemMatcher> BuildMatcherList() {
        List<ItemMatcher> matchers = new List<ItemMatcher>();

        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

        Block block = bs != null ? _capi.World.BlockAccessor.GetBlock(bs.Position) : null;
        BlockEntity be = bs != null ? _capi.World.BlockAccessor.GetBlockEntity(bs.Position) : null;
        var slot = OpenStorageSelector.GetTargetedStorageItem(_capi);
        ItemStack stack = slot?.Itemstack;

        foreach (var rule in rules) {
            rule.Run(matchers, bs, block, be, stack);
        }

        return matchers;
    }

    private bool PushTool() {
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        List<ItemMatcher> matchers = BuildMatcherList();
        if (matchers == null || matchers.Count == 0) {
            return false;
        }

        foreach (var matcher in matchers) {
            if (matcher.Matches(currentSlot)) {
                return false;
            }

            // Search on each inventory configured in inventories order matter
            for (int j = 0; j < _config.inventories.Length; j++) {
                if (SwapItemName(_config.inventories[j], matcher)) {
                    return true;
                }
            }
        }
        return false;
    }

    private void UnregisterSmartToolStopListListener() {
        if (_listener >= 0) {
            _capi.Event.UnregisterGameTickListener(_listener);
            _listener = -1;
        }
    }
    private void PopTool() {
        if (_isSmartToolHeld) {
            _isSmartToolHeld = false;
            SwapItemSlot();
        }
    }

    private void DebugHighlightBlock(BlockPos pos) {
        if (pos != null) {
            _capi.World.HighlightBlocks(_capi.World.Player, 123, new List<BlockPos> { pos });
        }
    }
    // void DumpItem(ItemStack stack) {
    //     if (stack == null)
    //         return;

    //    var collectible = stack.Collectible;

    //    _capi.Logger.Notification($"=== ITEM ===");
    //    _capi.Logger.Notification($"Code: {collectible.Code}");
    //    _capi.Logger.Notification($"Class: {collectible.GetType().FullName}");
    //    _capi.Logger.Notification($"Attributes: {collectible.Attributes?.Token}");
    //    _capi.Logger.Notification($"Stack Attributes: {stack.Attributes?.ToJsonToken()}");
    //}

    // void ShowHeldItemCode() {
    //     var slot = _capi.World.Player.Entity.RightHandItemSlot;
    //     if (slot?.Itemstack == null) {
    //         _capi.ShowChatMessage("[SmartCursor] Hand is empty");
    //         return;
    //     }

    //    _capi.Logger.Notification($"[SmartCursor] Held: {slot.Itemstack.Collectible.Code}");
    //    _capi.ShowChatMessage($"[SmartCursor] PATH: {slot.Itemstack.Collectible.Code.Path}");
    //    _capi.ShowChatMessage($"[SmartCursor] CODE: {slot.Itemstack.Collectible.Code}");
    //    DumpItem(slot?.Itemstack);
    //}

    private void StartSmartCursor(bool mode) {
        // ShowHeldItemCode();
        _isToggleMode = mode;
        if (!_isSmartToolHeld) {
            UnregisterSmartToolStopListListener();
            _listener = _capi.Event.RegisterGameTickListener(SmartToolStopListListener, 100);
            _isSmartToolHeld = PushTool();
        } else if (_isToggleMode) {
            PopTool();
            UnregisterSmartToolStopListListener();
        }
    }
}
}
