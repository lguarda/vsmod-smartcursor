using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;

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

  private void RegisterKey(string keyCode, GlKeys key) {
    _capi.Input.RegisterHotKey(keyCode, $"Smart cursor key: {keyCode}", key,
                               HotkeyType.GUIOrOtherControls);
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

  public override void StartClientSide(ICoreClientAPI api) {
    Mod.Logger.Notification("SmartCursor starting");
    _isSmartToolHeld = false;
    _capi = api;

    RegisterKey(HOTKEY_SMARTCURSOR, GlKeys.R);
    RegisterKey(HOTKEY_SMARTCURSOR_TOGGLE, GlKeys.Tilde);
    _capi.Input.AddHotkeyListener(HotKeyListener);
  }

  private void SmartToolStopListListener(float t) {
    // This should not happen but don't do anything if there's no current smart
    // tool held
    if (!_isSmartToolHeld) {
      return;
    }
    int currentActiveSlotIndex =
        _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;

    // To avoid confusion when active bar change disable the smart tool
    // or when not in toggle mode and hotkey was released
    if (currentActiveSlotIndex != _savedActiveSlotIndex ||
        (!_isToggleMode && !_capi.Input.IsHotKeyPressed(HOTKEY_SMARTCURSOR))) {
      PopTool();
    }
  }

  private EnumTool? SmartToolSelectorEntity() {
    EntitySelection es = _capi.World.Player.CurrentEntitySelection;

    if (es != null) {
      Entity entity = es.Entity;
      if (!entity.Alive) {
        return EnumTool.Knife;
      }
    }
    return null;
  }

  // This function return tool based on targeted block
  private EnumTool? SmartToolSelector() {
    EnumTool? tool = SmartToolSelectorEntity();
    if (tool != null) {
      return tool;
    }
    BlockSelection bs = _capi.World.Player.CurrentBlockSelection;

    if (bs == null) {
      return EnumTool.Sword;
    }

    Block block = _capi.World.BlockAccessor.GetBlock(bs.Position);
    // _capi.ShowChatMessage($"key {block.BlockMaterial}");
    switch (block.BlockMaterial) {
    case EnumBlockMaterial.Gravel:
    case EnumBlockMaterial.Sand:
    case EnumBlockMaterial.Snow:
    case EnumBlockMaterial.Soil:
      return EnumTool.Shovel;
    case EnumBlockMaterial.Metal:
    case EnumBlockMaterial.Ore:
    case EnumBlockMaterial.Stone:
    case EnumBlockMaterial.Ice:
    case EnumBlockMaterial.Glass:
    case EnumBlockMaterial.Brick:
    case EnumBlockMaterial.Ceramic:
      return EnumTool.Pickaxe;
    case EnumBlockMaterial.Plant:
      return EnumTool.Knife;
    case EnumBlockMaterial.Wood:
    case EnumBlockMaterial.Leaves:
      return EnumTool.Axe;
    // Liquid = 8
    // Air = 0
    // Cloth = 16
    // Fire = 19
    // Lava = 17
    // Mantle = 12
    // Meta = 20
    // Other = 21
    default:
      return null;
    }
  }

  private bool SwapItemSlot(ItemSlot a, ItemSlot b) {
    // Until i know how to do this properly right now i'm using the
    // mouseItemSlot as temporary item holder during the swap
    ItemSlot mouseItemSlot = _capi.World.Player.InventoryManager.MouseItemSlot;
    if (!mouseItemSlot.Empty) {
      return false;
    }

    // I really need to find a better way to do this it's seems to work
    // Now this two move operation are here to account for empty item slot as
    // well as slot with multiple stack size maybe try with max stack size and
    // call it a day?
    ItemStackMoveOperation op_a =
        new ItemStackMoveOperation(_capi.World, EnumMouseButton.None, 0,
                                   EnumMergePriority.AutoMerge, a.StackSize);

    ItemStackMoveOperation op_b =
        new ItemStackMoveOperation(_capi.World, EnumMouseButton.None, 0,
                                   EnumMergePriority.AutoMerge, b.StackSize);

    object obj = _capi.World.Player.InventoryManager.TryTransferTo(
        a, mouseItemSlot, ref op_a);
    if (obj != null) {
      _capi.Network.SendPacketClient(obj);
    }

    obj = _capi.World.Player.InventoryManager.TryTransferTo(b, a, ref op_b);
    if (obj != null) {
      _capi.Network.SendPacketClient(obj);
    }

    obj = _capi.World.Player.InventoryManager.TryTransferTo(mouseItemSlot, b,
                                                            ref op_a);
    if (obj != null) {
      _capi.Network.SendPacketClient(obj);
    }

    return true;
  }

  private bool IsRightTool(ItemSlot slot, EnumTool toolType) {
    EnumTool? currentTool = slot?.Itemstack?.Collectible?.Tool;
    return currentTool == toolType;
  }

  private int FindToolSlotInInventory(EnumTool toolType, IInventory inventory) {
    for (int i = 0; i < inventory.Count; i++) {
      if (IsRightTool(inventory[i], toolType)) {
        return i;
      }
    }
    return -1;
  }

  private bool SwapTool(string inventoryName, EnumTool toolType,
                        ItemSlot currentSlot) {
    IInventory inventory =
        _capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);

    int slotNumber = FindToolSlotInInventory(toolType, inventory);

    if (slotNumber < 0) {
      return false;
    }
    ItemSlot slot = inventory[slotNumber];

    _savedSlotIndex = slotNumber;
    _savedSlotInventoryName = inventoryName;
    _savedActiveSlotIndex =
        _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;

    bool ret = SwapItemSlot(currentSlot, slot);

    // TODO is this needed?
    _capi.World.Player.InventoryManager.CloseInventoryAndSync(inventory);
    return ret;
  }

  private bool PushTool() {
    ItemSlot currentSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
    EnumTool? tool = SmartToolSelector();
    if (tool == null) {
      return false;
    }
    EnumTool toolType = (EnumTool)tool;

    // First Stop if the current tool is the right one
    if (IsRightTool(currentSlot, toolType)) {
      return false;
    }

    // Than prioritize hotbar for tool search
    if (SwapTool(GlobalConstants.backpackInvClassName, toolType, currentSlot)) {
      return true;
    }

    // When not found take the tool from backpack
    return SwapTool(GlobalConstants.hotBarInvClassName, toolType, currentSlot);
  }

  private void UnregisterSmartToolStopListListener() {
    if (_listener >= 0) {
      _capi.Event.UnregisterGameTickListener(_listener);
      _listener = -1;
    }
  }
  private void PopTool() {
    _isSmartToolHeld = false;
    UnregisterSmartToolStopListListener();
    IInventory inventory = _capi.World.Player.InventoryManager.GetOwnInventory(
        _savedSlotInventoryName);
    IInventory hotbar = _capi.World.Player.InventoryManager.GetOwnInventory(
        GlobalConstants.hotBarInvClassName);

    ItemSlot bar = hotbar[_savedActiveSlotIndex];
    ItemSlot back = inventory[_savedSlotIndex];

    SwapItemSlot(bar, back);
  }

  private void StartSmartCursor(bool mode) {
    _isToggleMode = mode;
    if (!_isSmartToolHeld) {
      UnregisterSmartToolStopListListener();
      _listener =
          _capi.Event.RegisterGameTickListener(SmartToolStopListListener, 100);
      _isSmartToolHeld = PushTool();
    } else if (_isToggleMode) {
      PopTool();
    }
  }
}
}
