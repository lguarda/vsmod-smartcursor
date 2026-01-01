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

  // Called on server and client
  // public override void Start(ICoreAPI api)
  //{
  //    Mod.Logger.Notification("Hello from template mod: " +
  //    Lang.Get("mymodid:hello"));
  //}

  // public override void StartServerSide(ICoreServerAPI api)
  //{
  //     Mod.Logger.Notification("Hello from template mod server side");
  // }

  // https://github.com/Xandu93/VSMods/blob/master/mods/xinvtweaks/src/InventoryUtil.cs
  //
  private void RegisterKey(string keyCode, GlKeys key) {
    capi.Input.RegisterHotKey(keyCode, $"Smart cursor key: {keyCode}", key,
                              HotkeyType.GUIOrOtherControls);
    capi.Input.SetHotKeyHandler(keyCode, (_) => true);
  }

  // Listining for the cycle camera
  private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
    Mod.Logger.Notification($"OMG 1 hotkey: {hotkeycode}");
    switch (hotkeycode) {
    case "smartcursor":
      SmartCursor();
      return;
      // case "camerastepright": IncreaseCameraRight(); return;
      // case "camerastepleft": IncreaseCameraLeft(); return;
      // case "camerastepup": IncreaseCameraUp(); return;
      // case "camerastepdown": IncreaseCameraDown(); return;
    }
  }

  public override void StartClientSide(ICoreClientAPI api) {
    toogle = false;
    Mod.Logger.Notification("OMG 2 Hello from template mod client side");
    capi = api;
    RegisterKey("smartcursor", GlKeys.R);
    capi.Input.AddHotkeyListener(HotKeyListener);
    capi.Event.RegisterGameTickListener(DetectHotbarChange, 100);
  }

  void DetectHotbarChange(float t) {
    var invMan = capi.World.Player.InventoryManager;
    int cur = invMan.ActiveHotbarSlotNumber;

    if (cur != lastSlot) {
        if (toogle) {
            PopTool();
            toogle = false;
        }
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
      return EnumTool.Pickaxe;
      break;
    //
    case EnumBlockMaterial.Plant:
      return EnumTool.Knife;
      break;
    //
    case EnumBlockMaterial.Wood:
    case EnumBlockMaterial.Leaves:
      return EnumTool.Axe;
      break;
    // Air = 0
    // Brick = 18
    // Ceramic = 15
    // Cloth = 16
    // Fire = 19
    // Gravel = 2
    // Lava = 17
    // Liquid = 8
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
      IInventory inventory = capi.World.Player.InventoryManager.GetOwnInventory(inventoryName);

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
      //Mod.Logger.Notification("OMG 4 Hello from template mod client side");
      ItemSlot currentSlot =
          capi.World.Player.InventoryManager.ActiveHotbarSlot;
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
      IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(savedSlotInventory);
      IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);

      ItemSlot bar = hotbar[savedActiveSlot];
      ItemSlot back = backpack[savedSlot];

      capi.ShowChatMessage($"Try to put back {back.GetStackName()} in {bar.GetStackName()} ");
      SwapItemSlot(back, bar);
  }
  void SmartCursor() {
    Mod.Logger.Notification(
        $"OMG ==== Hello from template mod client side {toogle}");
    if (!toogle) {
      toogle = PushTool();
    } else {
      toogle = false;
      PopTool();
    }
  }
}
}
