using ProtoBuf;

using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using System;
using System.Collections.Generic;

using Vintagestory.API.MathTools;

namespace SmartCursor {

[ProtoContract]
public class PlaceBlockMsg {
    [ProtoMember(1)]
    public int X;

    [ProtoMember(2)]
    public int Y;

    [ProtoMember(3)]
    public int Z;
}

public class SmartPlacement : ModSystem {

    IClientNetworkChannel clientChannel;
    ICoreClientAPI _capi;

    double _lastPlacementTime = 0;
    const double PLACEMENT_REPEAT_MS = 240;

    const int _highlight_id = 42638;
    long _listener = -1;
    bool _toggle = false;

    public override void StartServerSide(ICoreServerAPI api) {

        api.Network.RegisterChannel("SmartCursor")
            .RegisterMessageType<PlaceBlockMsg>()
            .SetMessageHandler<PlaceBlockMsg>((player, msg) => {
                BlockPos pos = new BlockPos(msg.X, msg.Y, msg.Z);
                ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
                if (slot?.Itemstack?.Block != null) {
                    api.World.BlockAccessor.SetBlock(slot.Itemstack.Block.BlockId, pos);
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            });
    }

    private void UnregisterListener() {
        if (_listener >= 0) {
            _capi.Event.UnregisterGameTickListener(_listener);
            _listener = -1;
        }
        _toggle = false;
        _capi.World.HighlightBlocks(_capi.World.Player, _highlight_id, new List<BlockPos>(), null);
    }

    private void RegisterListener() {
        if (_toggle == false) {
            UnregisterListener();
            _toggle = true;
            _listener = _capi.Event.RegisterGameTickListener(SmartPlacementHighlightListener, 100);
        }
    }

    private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
        switch (hotkeycode) {
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT:
            RegisterListener();
            break;
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        clientChannel = api.Network.RegisterChannel("SmartCursor").RegisterMessageType<PlaceBlockMsg>();

        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT, GlKeys.F);
        _capi.Input.AddHotkeyListener(HotKeyListener);

        _capi.Input.InWorldAction += HookRightClick;
    }

    private void HookRightClick(EnumEntityAction action, bool on, ref EnumHandling handled) {
        if (_capi.Input.IsHotKeyPressed(SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT)) {
            if (action == EnumEntityAction.InWorldRightMouseDown) {
                handled = EnumHandling.PreventDefault;
            }
            else if (action == EnumEntityAction.RightMouseDown) {
                handled = EnumHandling.PreventDefault;
                if (on) {
                   long currentTime = _capi.World.ElapsedMilliseconds;
                    if (currentTime - _lastPlacementTime >= PLACEMENT_REPEAT_MS) {
                        SmartPlace();
                        _lastPlacementTime = currentTime;
                    }
                }
            }
        }

    }

    static public BlockPos FindNextHorizontalPlacingPos(ICoreClientAPI capi) {
        EntityPos pos = capi.World.Player.Entity.Pos;

        BlockPos blockPos = pos.AsBlockPos.DownCopy();
        blockPos.Y = (int)Math.Floor(pos.Y - 0.1);
        capi.ShowChatMessage($"OMG 10000 {blockPos.Y} {pos.Y}");
        BlockPos foundPos = null;

        double yaw = pos.Yaw;
        int dx = 0, dz = 0;

        // Normalize to 0-2π
        yaw = yaw % (2 * Math.PI);
        if (yaw < 0)
            yaw += 2 * Math.PI;

        // Convert to cardinal direction
        if (yaw < Math.PI / 4 || yaw >= 7 * Math.PI / 4)
            dz = 1; // North
        else if (yaw < 3 * Math.PI / 4)
            dx = 1; // East
        else if (yaw < 5 * Math.PI / 4)
            dz = -1; // South
        else
            dx = -1; // West

        for (int i = 1; i <= 5; i++) {
            BlockPos checkPos = new BlockPos(blockPos.X + dx * i, blockPos.Y, blockPos.Z + dz * i);
            Block block = capi.World.BlockAccessor.GetBlock(checkPos);

            if (block.BlockMaterial == EnumBlockMaterial.Air) {
                foundPos = checkPos;
                break;
            }
        }
        return foundPos;
    }


    private void SmartPlacementHighlightListener(float t) {
        BlockPos pos = FindNextHorizontalPlacingPos(_capi);
        _capi.ShowChatMessage($"OMG 1");
        //int yellow = ColorUtil.ToRgba(150, 255, 255, 0);
        if (pos != null) {
            _capi.ShowChatMessage($"OMG 2 found pos");
            // _capi.World.HighlightBlocks(_capi.World.Player, _highlight_id, new List<BlockPos> { pos }, new List<int> {8} );
            _capi.World.HighlightBlocks(
                _capi.World.Player, 
                _highlight_id,
                new List<BlockPos> { pos },
                new List<int> { ColorUtil.ToRgba(50, 0, 160, 160) }, // Semi-transparent yellow
                EnumHighlightBlocksMode.Absolute,
                EnumHighlightShape.Arbitrary
            );
        }

        if (!_capi.Input.IsHotKeyPressed(SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT)) {
            _capi.ShowChatMessage($"OMG 3 unregister");
            UnregisterListener();
        }
    }

    // static public void PlaceActiveSlotAt(ICoreClientAPI capi, BlockPos targetPos) {
    //     if (targetPos == null) {
    //         return;
    //     }
    //     // Get the slot with the block you want to place
    //     ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;

    //     capi.ShowChatMessage($"Slot: {slot != null}");
    //     capi.ShowChatMessage($"ItemStack: {slot?.Itemstack != null}");
    //     capi.ShowChatMessage($"Item: {slot?.Itemstack?.Collectible?.Code}");
    //     capi.ShowChatMessage($"Target pos: {targetPos}");

    //     IClientPlayer player = capi.World.Player;
    // }

    public void SmartPlace() {
        BlockPos pos = SmartPlacement.FindNextHorizontalPlacingPos(_capi);
        if (pos != null) {
            _capi.World.Player.Entity.StartAnimation("placeblock");
            _capi.World.RegisterCallback((dt) => { _capi.World.Player.Entity.StopAnimation("placeblock"); }, 120);
            clientChannel.SendPacket(new PlaceBlockMsg { X = pos.X, Y = pos.Y, Z = pos.Z });
        }
    }
}
}
