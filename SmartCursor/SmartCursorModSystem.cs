using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
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

    private EnumTool[] SmartToolSelectorEntity() {
        EntitySelection es = _capi.World.Player.CurrentEntitySelection;

        if (es != null) {
            Entity entity = es.Entity;
            _capi.ShowChatMessage($"Entity {entity.GetName()}");
            if (!entity.Alive) {
                return [EnumTool.Knife];
            }
        }
        return [];
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
    private string SelectItemFromWorkItem(string workItem) {
        switch (workItem) {
        case "clayworkitem-fire":
            return "Fire clay";
        case "clayworkitem-red":
            return "Red clay";
        case "clayworkitem-blue":
            return "Blue clay";
        default:
            return null;
        }
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
        _capi.ShowChatMessage($"Material {block.BlockMaterial}");
        _previousBlockCode = block?.Code?.Path;
        _capi.ShowChatMessage($"path {_previousBlockCode}");

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

    private bool IsRightTool(ItemSlot slot, EnumTool toolType) {
        EnumTool? currentTool = slot?.Itemstack?.Collectible?.Tool;
        return currentTool == toolType && !isItemBlackListed(slot);
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

    private int FindToolSlotInInventory(EnumTool toolType, IInventory inventory) {
        for (int i = 0; i < inventory.Count; i++) {
            if (IsRightTool(inventory[i], toolType)) {
                return i;
            }
        }
        return -1;
    }

    private int FindStackNameSlotInInventory(string stackName, IInventory inventory) {
        for (int i = 0; i < inventory.Count; i++) {
            // TODO opti this
            _capi.ShowChatMessage($"4 omg {inventory[i].GetStackName()} == {stackName}");
            if (inventory[i].GetStackName() == stackName) {
                return i;
            }
        }
        return -1;
    }

    private bool SwapItemName(string inventoryName, string stackName, ItemSlot currentSlot) {
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
        if (inventory == null) {
            return false;
        }

        int slotNumber = FindStackNameSlotInInventory(stackName, inventory);

        return SwapItemSlotSaved(inventoryName, slotNumber);
    }

    private bool SwapTool(string inventoryName, EnumTool toolType, ItemSlot currentSlot) {
        IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);
        if (inventory == null) {
            return false;
        }

        int slotNumber = FindToolSlotInInventory(toolType, inventory);

        return SwapItemSlotSaved(inventoryName, slotNumber);
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

    private bool PushTool() {
        ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;

        // TODO clean also this, i need to merge this nested for loop with the other loop
        // maybe creating a structure to match both tools, and stackname
        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;
        _capi.ShowChatMessage($"1 omg");
        if (bs != null) {
            string workItem = GetWorkItem(bs.Position);
            _capi.ShowChatMessage($"2 omg {workItem}");
            if (workItem != null) {
                string itemName = SelectItemFromWorkItem(workItem);
                _capi.ShowChatMessage($"3 omg {workItem} {itemName}");
                if (itemName != null) {
                    for (int j = 0; j < _config.inventories.Length; j++) {
                        if (SwapItemName(_config.inventories[j], itemName, currentSlot)) {
                            return true;
                        }
                    }
                }
            }
        }

        EnumTool[] tools = SmartToolSelector();

        if (tools.Length == 0) {
            return false;
        }
        for (int i = 0; i < tools.Length; i++) {
            // First Stop if the current tool is the right one
            if (IsRightTool(currentSlot, tools[i])) {
                return false;
            }

            // Search on each inventory configured in inventories order matter
            for (int j = 0; j < _config.inventories.Length; j++) {
                if (SwapTool(_config.inventories[j], tools[i], currentSlot)) {
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

    void DebugDrawPoint(Vec3d pos) {
        SimpleParticleProperties p =
            new SimpleParticleProperties(1,                                // float minQuantity,
                                         1,                                // float maxQuantity
                                         ColorUtil.ToRgba(255, 255, 0, 0), // int color
                                         pos,                              // Vec3d minPos
                                         pos,                              // Vec3d maxPos
                                         Vec3f.Zero,                       // Vec3f minVelocity
                                         Vec3f.Zero,                       // Vec3f maxVelocity
                                         6f,                               // float lifeLength = 1
                                         0,                                // float gravityEffect = 1
                                         0.3f,                             // float minSize = 1
                                         0.3f,                             // float maxSize = 1
                                         EnumParticleModel.Cube // EnumParticleModel model = EnumParticleModel.Cube)
            );
        p.WithTerrainCollision = false;
        _capi.World.SpawnParticles(p);
    }

    Block? RayTraceBlock() {
        EntityPlayer ep = _capi.World.Player.Entity;
        // TODO: this is not the real camera position, is seems slightly offseted behind
        Vec3d eyePos = ep.CameraPos;
        eyePos.Y += ep.LocalEyePos.Y;
        _capi.ShowChatMessage($"Hit block {eyePos.X} {eyePos.Y} {eyePos.Z}");
        Vec3d dir = ep.Pos.GetViewVector().ToVec3d();

        float maxDistance = 3f;
        float step = 0.2f;
        for (float d = 0; d <= maxDistance; d += step) {
            Vec3d p = eyePos + dir * d;

            BlockPos tmpPos = new BlockPos((int)p.X, (int)p.Y, (int)p.Z);
            Block block = _capi.World.BlockAccessor.GetBlock(tmpPos);
            DebugDrawPoint(p);
            if (block.BlockMaterial != EnumBlockMaterial.Air) {
                // _capi.ShowChatMessage($"Hit block {block.BlockMaterial}");
                _capi.ShowChatMessage($"Hit block {eyePos.X} {eyePos.Y} {eyePos.Z}");
                DebugHighlightBlock(tmpPos);
                _capi.ShowChatMessage($"code {block?.Code?.Path}");
                return block;
            }
        }
        return null;
    }

    private void StartSmartCursor(bool mode) {
        // SmartPlacement.PlaceActiveSlotAt(_capi, SmartPlacement.FindNextHorizontalPlacingPos(_capi));
        // ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        //_capi.ShowChatMessage($"ICI {currentSlot.GetStackName()}");

        // Block block = RayTraceBlock();
        // if (block != null) {
        //     _capi.ShowChatMessage($"Hit block {block.BlockMaterial}");
        // }
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
