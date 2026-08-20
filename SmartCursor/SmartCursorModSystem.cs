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

    private EnumTool[] StringsToEnumToolArray(string[] tools) {
        EnumTool[] result = new EnumTool[tools.Length];

        for (int i = 0; i < tools.Length; i++) {
            if (!Enum.TryParse(tools[i], ignoreCase: true, out result[i])) {
                Mod.Logger.Notification($"Invalid tool enum: {tools[i]}");
            }
        }

        return result;
    }
    private void parseMaterialTools() {
        _materialTools = new Dictionary<EnumBlockMaterial, EnumTool[]>();
        foreach (var kv in _config.materialTools) {
            EnumBlockMaterial material;
            if (Enum.TryParse(kv.Key, ignoreCase: true, out material)) {
                _materialTools[material] = StringsToEnumToolArray(kv.Value);
            }
        }
    }

    private void parseDomainTools() {
        _domainTools = new Dictionary<string, EnumTool[]>();

        foreach (var kv in _config.domainTools) {
            _domainTools[kv.Key] = StringsToEnumToolArray(kv.Value);
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        Mod.Logger.Notification("SmartCursor starting");
        _isSmartToolHeld = false;
        _capi = api;

        LoadConfig(CONFIG_PATH);
        SaveConfig(CONFIG_PATH);
        parseDomainTools();
        parseMaterialTools();

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

        if (be is BlockEntityPitKiln pk) {
            int stage = FirePitReflection.GetCurrentBuildStage(pk);

            return $"{blockCode}|stage={stage}|complete={pk.IsComplete}|lit={pk.Lit}";
        }

        if (be is BlockEntityGroundStorage gs) {
            // change whenever slot contents change (e.g. clay appears/disappears)
            int hash = 17;
            foreach (var slot in gs.Inventory)
                hash = hash * 31 + (slot.Empty ? 0 : slot.Itemstack.Collectible.Code.GetHashCode());
            return $"{blockCode}|gs={hash}";
        }
        if (be is BlockEntityBloomery bloomery) {
            var (fuelSlot, oreSlot, outSlot) = BloomeryReflection.GetSlots(bloomery);
            int oreCapacity = BloomeryReflection.GetOreCapacity(bloomery);
            bool oreFull = oreSlot.StackSize >= oreCapacity;

            return $"{blockCode}|oreFull={oreFull}|canIgnite={bloomery.CanIgnite()}|burning={bloomery.IsBurning}|hasOut={outSlot.StackSize > 0}";
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
        // "Tin bronze pickaxe"
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

    private string GetWorkItem(BlockPos pos) {
        BlockEntity be = _capi.World.BlockAccessor.GetBlockEntity(pos);
        if (be != null) {
            var workItemField = be.GetType().GetField("workItemStack", System.Reflection.BindingFlags.NonPublic |
                                                                           System.Reflection.BindingFlags.Instance);

            if (workItemField != null) {
                ItemStack workItem = workItemField.GetValue(be) as ItemStack;
                if (workItem != null) {
                    string path = workItem.Collectible.Code.Path; // Should contain clay type
                    return path;
                }
            }
        }
        return null;
    }

    // This is huge bull shilt
    static private string SelectItemFromWorkItem(string workItem) {
        switch (workItem) {
        case "clayworkitem-fire":
            return "clay-fire";
        case "clayworkitem-red":
            return "clay-red";
        case "clayworkitem-blue":
            return "clay-blue";
        default:
            return null;
        }
    }

    private void AddWorkedItemMatcher(List<ItemMatcher> matchers) {
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

        string workItem = GetWorkItem(bs.Position);
        if (workItem != null) {
            string itemName = SelectItemFromWorkItem(workItem);
            if (itemName != null) {
                matchers.Add(new ItemCodeMatcher(itemName));
            }
        }
    }

    private void AddEntityMatcher(List<ItemMatcher> matchers) {
        EntitySelection es = _capi.World.Player.CurrentEntitySelection;

        if (es != null) {
            Entity entity = es.Entity;
            // _capi.ShowChatMessage($"Entity {entity.GetName()} {!entity.Alive}");
            if (!entity.Alive) {
                matchers.Add(new ToolTypeMatcher(EnumTool.Knife));
            }
        }
    }

    // This function return tool based on targeted block
    private void AddToolTypeMatcher(List<ItemMatcher> matchers) {
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

        Block block = _capi.World.BlockAccessor.GetBlock(bs.Position);
        if (block == null) return;

        string prefix = block.Code?.Path is string p ? (p.IndexOf('-') is int i && i >= 0 ? p[..i] : p) : null;

        EnumTool[] tools;
        if (_domainTools.TryGetValue(prefix, out tools)) {
        } else if (_materialTools.TryGetValue(block.BlockMaterial, out tools)) {
        }
        if (tools != null) {
            foreach (var tool in tools) {
                matchers.Add(new ToolTypeMatcher(tool));
            }
        }

        return;
    }

    private List<ItemMatcher> BuildMatcherList() {
        List<ItemMatcher> matchers = new List<ItemMatcher>();

        AddEntityMatcher(matchers);

        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

        if (bs != null) {
            FirePitMatcher.GetFirePitMatcher(matchers, _capi);
            BloomeryMatcher.GetBloomeryMatcher(matchers, _capi);
            AddWorkedItemMatcher(matchers);
            AddToolTypeMatcher(matchers);
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

    // void ShowHeldItemCode()
    //{
    //     var slot = _capi.World.Player.Entity.RightHandItemSlot;
    //     if (slot?.Itemstack == null) {
    //         _capi.ShowChatMessage("[SmartCursor] Hand is empty");
    //         return;
    //     }
    //     _capi.Logger.Notification($"[SmartCursor] Held: {slot.Itemstack.Collectible.Code.Path}");
    //     _capi.ShowChatMessage($"[SmartCursor] Held: {slot.Itemstack.Collectible.Code.Path}");
    // }

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
