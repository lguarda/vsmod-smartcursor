using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;

namespace SmartCursor {

public class SmartCursorModSystem : ModSystem {
    const string HOTKEY_SMARTCURSOR = "smartcursor";
    const string HOTKEY_SMARTCURSOR_TOGGLE = "smartcursor toggle";

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

    private void RegisterKey(string keyCode, GlKeys key) {
        _capi.Input.RegisterHotKey(keyCode, $"Smart cursor key: {keyCode}", key, HotkeyType.GUIOrOtherControls);
        _capi.Input.SetHotKeyHandler(keyCode, (_) => true);
    }

    private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
        switch (hotkeycode) {
        case HOTKEY_SMARTCURSOR:
            StartSmartCursor(false);
            break;
        case HOTKEY_SMARTCURSOR_TOGGLE:
            StartSmartCursor(true);
            break;
        }
    }

    private void LoadConfig(string path) {
        try {
            _config = _capi.LoadModConfig<SmartCursorConfig>(path);
        } catch (Exception) {
            _config = null;
        }
        if (_config == null) {
            _config = new SmartCursorConfig();
        }
        _capi.StoreModConfig(_config, path);
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

        LoadConfig("smartcursor.json");
        parseDomainTools();
        parseMaterialTools();

        RegisterKey(HOTKEY_SMARTCURSOR, GlKeys.R);
        RegisterKey(HOTKEY_SMARTCURSOR_TOGGLE, GlKeys.Tilde);
        _capi.Input.AddHotkeyListener(HotKeyListener);
    }
    private bool SmartToolReload() {
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;
        Block block = bs != null ? _capi.World.BlockAccessor.GetBlock(bs.Position) : null;
        string blockCode = block?.Code?.Path;

        if (blockCode != _previousBlockCode) {
            ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
            EnumTool[] tools = SmartToolSelector();
            if (tools.Length > 0 && !IsRightTool(currentSlot, tools[0])) {
                PopTool();
                _isSmartToolHeld = PushTool();
                return true;
            }
        }
        return false;
    }
    private void SmartToolStopListListener(float t) {
        if (!_isToggleMode) {

            // When not in toggle mode and hotkey was released pop tool
            if (!_capi.Input.IsHotKeyPressed(HOTKEY_SMARTCURSOR)) {
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

    private EnumTool[] SmartToolSelectorEntity() {
        EntitySelection es = _capi.World.Player.CurrentEntitySelection;

        if (es != null) {
            Entity entity = es.Entity;
            if (!entity.Alive) {
                return [EnumTool.Knife];
            }
        }
        return [];
    }

    // This function return tool based on targeted block
    private EnumTool[] SmartToolSelector() {
        // TODO clean this
        EnumTool[] tools = SmartToolSelectorEntity();
        if (tools.Length > 0) {
            return tools;
        }
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

        if (bs == null) {
            return [];
        }

        Block block = _capi.World.BlockAccessor.GetBlock(bs.Position);
        //_capi.ShowChatMessage($"Material {block.BlockMaterial}");
        _previousBlockCode = block?.Code?.Path;
        string prefix = _previousBlockCode is string p ? (p.IndexOf('-') is int i && i >= 0 ? p[..i] : p) : null;

        if (_domainTools.TryGetValue(prefix, out tools)) {
            return tools;
        }

        if (_materialTools.TryGetValue(block.BlockMaterial, out tools)) {
            return tools;
        }

        return [];
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

    static private bool IsRightTool(ItemSlot slot, EnumTool toolType) {
        EnumTool? currentTool = slot?.Itemstack?.Collectible?.Tool;
        return currentTool == toolType;
    }

    static private int FindToolSlotInInventory(EnumTool toolType, IInventory inventory) {
        for (int i = 0; i < inventory.Count; i++) {
            if (IsRightTool(inventory[i], toolType)) {
                return i;
            }
        }
        return -1;
    }

    private bool SwapTool(string inventoryName, EnumTool toolType, ItemSlot currentSlot) {
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);

        int slotNumber = FindToolSlotInInventory(toolType, inventory);

        if (slotNumber < 0) {
            return false;
        }

        _savedSlotIndex = slotNumber;
        _savedSlotInventoryName = inventoryName;
        _savedActiveSlotIndex = _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;

        return SwapItemSlot();
    }

    private bool PushTool() {
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        EnumTool[] tools = SmartToolSelector();

        if (tools.Length == 0) {
            return false;
        }
        for (int i = 0; i < tools.Length; i++) {
            // First Stop if the current tool is the right one
            if (IsRightTool(currentSlot, tools[i])) {
                return false;
            }

            // Than prioritize hotbar for tool search
            if (SwapTool(GlobalConstants.backpackInvClassName, tools[i], currentSlot)) {
                return true;
            }

            // When not found take the tool from backpack
            if (SwapTool(GlobalConstants.hotBarInvClassName, tools[i], currentSlot)) {
                return true;
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

    private void StartSmartCursor(bool mode) {
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
