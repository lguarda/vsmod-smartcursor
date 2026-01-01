using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace SmartCursor {

public class SmartCursorModSystem : ModSystem {
  const string HOTKEY_SMARTCURSOR = "smartcursor";
  const string HOTKEY_SMARTCURSOR_TOGGLE = "smartcursor toggle";

  ICoreClientAPI capi;
  int savedSlot;
  string savedSlotInventory;
  int savedActiveSlot;
  bool smartToolHeld;
  bool toggleMode;
  int lastSlot = -1;
  long listener;

  private void RegisterKey(string keyCode, GlKeys key) {
    capi.Input.RegisterHotKey(keyCode, $"Smart cursor key: {keyCode}", key,
                              HotkeyType.GUIOrOtherControls);
    capi.Input.SetHotKeyHandler(keyCode, (_) => true);
  }

  private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
    switch (hotkeycode) {
    case HOTKEY_SMARTCURSOR:
      SmartCursor(false);
      break;
    case HOTKEY_SMARTCURSOR_TOGGLE:
      SmartCursor(true);
      break;
    }
  }

  public override void StartClientSide(ICoreClientAPI api) {
    Mod.Logger.Notification("SmartCursor starting");
    smartToolHeld = false;
    capi = api;

    // TODO configurable keybind
    RegisterKey(HOTKEY_SMARTCURSOR, GlKeys.R);
    RegisterKey(HOTKEY_SMARTCURSOR_TOGGLE, GlKeys.Tilde);
    capi.Input.AddHotkeyListener(HotKeyListener);
  }

  void DetectHotbarChange(float t) {
    if (!smartToolHeld) {
      return;
    }
    var invMan = capi.World.Player.InventoryManager;
    int cur = invMan.ActiveHotbarSlotNumber;

    if (cur != lastSlot ||
        (!toggleMode && !capi.Input.IsHotKeyPressed(HOTKEY_SMARTCURSOR))) {
      PopTool();
      lastSlot = cur;
    }
  }

  EnumTool SmartToolSelector() {
    BlockSelection bs = capi.World.Player.CurrentBlockSelection;

    if (bs == null) {
      return EnumTool.Sword;
    }

    Block block = capi.World.BlockAccessor.GetBlock(bs.Position);
    switch (block.BlockMaterial) {
    case EnumBlockMaterial.Gravel:
    case EnumBlockMaterial.Sand:
    case EnumBlockMaterial.Snow:
    case EnumBlockMaterial.Soil:
      return EnumTool.Shovel;
      break;
    case EnumBlockMaterial.Metal:
    case EnumBlockMaterial.Ore:
    case EnumBlockMaterial.Stone:
    case EnumBlockMaterial.Ice:
    case EnumBlockMaterial.Glass:
    case EnumBlockMaterial.Brick:
      return EnumTool.Pickaxe;
      break;
    case EnumBlockMaterial.Plant:
      return EnumTool.Knife;
      break;
    case EnumBlockMaterial.Wood:
    case EnumBlockMaterial.Leaves:
      return EnumTool.Axe;
      break;
    // Liquid = 8
    // Air = 0
    // Ceramic = 15
    // Cloth = 16
    // Fire = 19
    // Lava = 17
    // Mantle = 12
    // Meta = 20
    // Other = 21
    default:
      return EnumTool.Sword;
    }
  }

  bool SwapItemSlot(ItemSlot a, ItemSlot b) {
    // Until i know how to do this properly right now i'm using the
    // mouseItemSlot as temporary item holder during the swap
    ItemSlot mouseItemSlot = capi.World.Player.InventoryManager.MouseItemSlot;
    if (!mouseItemSlot.Empty) {
      return false;
    }
    ItemStackMoveOperation op =
        new ItemStackMoveOperation(capi.World, EnumMouseButton.None, 0,
                                   EnumMergePriority.AutoMerge, a.StackSize);
    object obj = capi.World.Player.InventoryManager.TryTransferTo(
        a, mouseItemSlot, ref op);
    if (obj != null) {
      capi.Network.SendPacketClient(obj);
    }

    obj = capi.World.Player.InventoryManager.TryTransferTo(b, a, ref op);
    if (obj != null) {
      capi.Network.SendPacketClient(obj);
    }

    obj = capi.World.Player.InventoryManager.TryTransferTo(mouseItemSlot, b,
                                                           ref op);
    if (obj != null) {
      capi.Network.SendPacketClient(obj);
    }

    return true;
  }

  bool isRightTool(ItemSlot slot, EnumTool toolType) {
    EnumTool? currentTool = slot?.Itemstack?.Collectible?.Tool;
    return currentTool == toolType;
  }

  int FindToolSlotInInventory(EnumTool toolType, IInventory inventory) {
    for (int i = 0; i < inventory.Count; i++) {
      if (isRightTool(inventory[i], toolType)) {
        return i;
      }
    }
    return -1;
  }

  bool SwapTool(string inventoryName, EnumTool toolType, ItemSlot currentSlot) {
    IInventory inventory =
        capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);

    int slotNumber = FindToolSlotInInventory(toolType, inventory);

    if (slotNumber < 0) {
      return false;
    }
    ItemSlot slot = inventory[slotNumber];

    savedSlot = slotNumber;
    savedSlotInventory = inventoryName;
    savedActiveSlot = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;

    bool ret = SwapItemSlot(currentSlot, slot);

    // TODO is this needed?
    capi.World.Player.InventoryManager.CloseInventoryAndSync(inventory);
    return ret;
  }

  bool PushTool() {
    // Mod.Logger.Notification("OMG 4 Hello from template mod client side");
    ItemSlot currentSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
    EnumTool toolType = SmartToolSelector();
    // First Stop if the current tool is the right one
    if (isRightTool(currentSlot, toolType)) {
      return false;
    }
    if (SwapTool(GlobalConstants.backpackInvClassName, toolType, currentSlot)) {
      return true;
    }
    return SwapTool(GlobalConstants.hotBarInvClassName, toolType, currentSlot);
  }

  void PopTool() {
    smartToolHeld = false;
    capi.Event.UnregisterGameTickListener(listener);
    IInventory backpack =
        capi.World.Player.InventoryManager.GetOwnInventory(savedSlotInventory);
    IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(
        GlobalConstants.hotBarInvClassName);

    ItemSlot bar = hotbar[savedActiveSlot];
    ItemSlot back = backpack[savedSlot];

    SwapItemSlot(back, bar);
  }
  void SmartCursor(bool mode) {
    toggleMode = mode;
    if (!smartToolHeld) {
      listener = capi.Event.RegisterGameTickListener(DetectHotbarChange, 100);
      smartToolHeld = PushTool();
    } else if (toggleMode) {
      PopTool();
    }
  }
}
}
