using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace SmartCursor {
public class SmartCursorModSystem : ModSystem {
  ICoreClientAPI capi;
  int savedSlot;
  string savedSlotInventory;
  int savedActiveSlot;
  bool toogle;
  int lastSlot = -1;
  long listener;

  private void RegisterKey(string keyCode, GlKeys key) {
    capi.Input.RegisterHotKey(keyCode, $"Smart cursor key: {keyCode}", key,
                              HotkeyType.GUIOrOtherControls);
    capi.Input.SetHotKeyHandler(keyCode, (_) => true);
  }

  private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
    capi.ShowChatMessage($"key {hotkeycode} is up? {keyComb.OnKeyUp}");

    switch (hotkeycode) {
    case "smartcursor":
      SmartCursor();
      return;
    }
  }

  public override void StartClientSide(ICoreClientAPI api) {
    Mod.Logger.Notification("SmartCursor starting");
    toogle = false;
    capi = api;

    // TODO configurable keybind
    RegisterKey("smartcursor", GlKeys.R);
    capi.Input.AddHotkeyListener(HotKeyListener);
  }

  void DetectHotbarChange(float t) {
    if (!toogle) {
      return;
    }
    var invMan = capi.World.Player.InventoryManager;
    int cur = invMan.ActiveHotbarSlotNumber;

    // capi.ShowChatMessage($"pressed?
    // {capi.Input.IsHotKeyPressed("smartcursor")}");

    if (cur != lastSlot || !capi.Input.IsHotKeyPressed("smartcursor")) {
      PopTool();
      toogle = false;
      lastSlot = cur;
      capi.Event.UnregisterGameTickListener(listener);
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

  ItemSlot getItemSlotByHash(int hash) {
    IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(
        GlobalConstants.backpackInvClassName);

    for (int i = 0; i < backpack.Count; i++) {
      if (backpack[i].StackSize > 0) {
        capi.ShowChatMessage(
            $"item: {backpack[i].GetStackName()} hash {backpack[i].GetHashCode()}");
        if (backpack[i].GetHashCode() == hash) {
          return backpack[i];
        }
      }
    }
    return null;
  }

  bool SwapItemSlot(ItemSlot a, ItemSlot b) {
    ItemSlot mouseItemSlot = capi.World.Player.InventoryManager.MouseItemSlot;
    // TODO fail if mouseItemSlot is used
    // return false
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

    SwapItemSlot(currentSlot, slot);

    capi.World.Player.InventoryManager.CloseInventoryAndSync(inventory);
    return true;
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
    capi.ShowChatMessage($"Try other ");
    return SwapTool(GlobalConstants.hotBarInvClassName, toolType, currentSlot);
  }

  void PopTool() {
    IInventory backpack =
        capi.World.Player.InventoryManager.GetOwnInventory(savedSlotInventory);
    IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(
        GlobalConstants.hotBarInvClassName);

    ItemSlot bar = hotbar[savedActiveSlot];
    ItemSlot back = backpack[savedSlot];

    capi.ShowChatMessage(
        $"Try to put back {back.GetStackName()} in {bar.GetStackName()} ");
    SwapItemSlot(back, bar);
  }
  void SmartCursor() {
    Mod.Logger.Notification(
        $"OMG ==== Hello from template mod client side {toogle}");
    if (!toogle) {
      listener = capi.Event.RegisterGameTickListener(DetectHotbarChange, 100);
      toogle = PushTool();
    } else {
      // toogle = false;
      // PopTool();
    }
  }
}
}
