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
    ICoreServerAPI _sapi;

    double _lastPlacementTime = 0;
    const double PLACEMENT_REPEAT_MS = 240;

    const int _highlightId = 42638;
    long _listener = -1;
    bool _toggle = false;
    float _pitchStairTrigger = 3.3f;

    // Yeah so that the only way i found to get a rotated block that sucks
    private int RotateBlock(Block block, string orientation) {
        string path = block.Code.Path;

        string rotatedPath = path.Replace("north", orientation);
        rotatedPath = rotatedPath.Replace("east", orientation);
        rotatedPath = rotatedPath.Replace("west", orientation);
        rotatedPath = rotatedPath.Replace("south", orientation);
        // Mod.Logger.Debug($"Rotated path {path} -> {rotatedPath}");

        AssetLocation orientatedPath = new AssetLocation(block.Code.Domain, rotatedPath);

        Block orientedBlock = _sapi.World.GetBlock(orientatedPath);
        return orientedBlock.BlockId;
    }

    public override void StartServerSide(ICoreServerAPI api) {
        _sapi = api;

        api.Network.RegisterChannel("SmartCursor")
            .RegisterMessageType<PlaceBlockMsg>()
            .SetMessageHandler<PlaceBlockMsg>((player, msg) => {
                BlockPos pos = new BlockPos(msg.X, msg.Y, msg.Z);
                ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
                Block block = slot?.Itemstack?.Block;

                if (block != null) {

                    string orientation = BlockFacing.HorizontalFromYaw(player.Entity.Pos.Yaw).Code;
                    api.World.BlockAccessor.SetBlock(RotateBlock(block, orientation), pos);

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
        _capi.World.HighlightBlocks(_capi.World.Player, _highlightId, new List<BlockPos>(), null);
    }

    private void RegisterListener() {
        if (_toggle == false) {
            UnregisterListener();
            _toggle = true;
            _listener = _capi.Event.RegisterGameTickListener(HighlightListener, 100);
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
            } else if (action == EnumEntityAction.RightMouseDown) {
                handled = EnumHandling.PreventDefault;
                if (on) {
                    long currentTime = _capi.World.ElapsedMilliseconds;
                    if (currentTime - _lastPlacementTime >= PLACEMENT_REPEAT_MS) {
                        PlaceBlock();
                        _lastPlacementTime = currentTime;
                    }
                }
            }
        }
    }

    private BlockPos FindNextHorizontalPlacingPos() {
        EntityPos pos = _capi.World.Player.Entity.Pos;
        Vec3d dir = _capi.World.Player.Entity.Pos.GetViewVector().ToVec3d();
        double dirX = dir.X;
        double dirZ = dir.Z;

        // the -0.1 is because on a block the position is 0.001
        // so here it should work with hoe tile and slab
        double posY = Math.Floor(pos.Y - 0.1);
        BlockPos foundPos = null;

#if DEBUG
        SmartCursorUtils.RayTrace(_capi, new(pos.X, posY, pos.Z), new(dirX, 0, dirZ), 0.2, 7, (p) => {
            SmartCursorUtils.DebugDrawPoint(_capi, p);
            return false;
        });
#endif

        SmartCursorUtils.RayTrace(_capi, new(pos.X, posY, pos.Z), new(dirX, 0, dirZ), 0.3, 5, (p) => {
            BlockPos tmpPos = new BlockPos((int)p.X, (int)p.Y, (int)p.Z);
            Block block = _capi.World.BlockAccessor.GetBlock(tmpPos);
            if (block != null && block.BlockMaterial == EnumBlockMaterial.Air) {
                foundPos = tmpPos;
                return true;
            }
            return false;
        });

        return foundPos;
    }

    // TODO better name
    public BlockPos FindNextHorizontalPlacingPos2() {
        EntityPos pos = _capi.World.Player.Entity.Pos;

        BlockPos blockPos = pos.AsBlockPos.DownCopy();
        blockPos.Y = (int)Math.Floor(pos.Y - 0.1);
        BlockPos foundPos = null;

        double yaw = pos.Yaw;
        Vec3i dir = BlockFacing.HorizontalFromYaw(_capi.World.Player.Entity.Pos.Yaw).Normali;

        for (int i = 1; i <= 5; i++) {
            // This is useless but i may use this instead of raytrace for horizontal block
            int y = pos.Pitch < _pitchStairTrigger ? blockPos.Y + i : blockPos.Y;
            BlockPos checkPos = new BlockPos(blockPos.X + dir.X * i, y, blockPos.Z + dir.Z * i);
            Block block = _capi.World.BlockAccessor.GetBlock(checkPos);

            if (block.BlockMaterial == EnumBlockMaterial.Air) {
                foundPos = checkPos;
                break;
            }
        }
        return foundPos;
    }

    private BlockPos SmartPlacementGetPos() {
        EntityPos pos = _capi.World.Player.Entity.Pos;

        if (pos.Pitch > _pitchStairTrigger) {
            return FindNextHorizontalPlacingPos();
        } else {
            return FindNextHorizontalPlacingPos2();
        }
    }

    private void HighlightListener(float t) {
        BlockPos pos = SmartPlacementGetPos();
        if (pos != null) {
            _capi.World.HighlightBlocks(_capi.World.Player, _highlightId, new List<BlockPos> { pos },
                                        new List<int> { ColorUtil.ToRgba(50, 0, 160, 160) },
                                        EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
        }

        if (!_capi.Input.IsHotKeyPressed(SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT)) {
            UnregisterListener();
        }
    }

    public void PlaceBlock() {
        ItemSlot slot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;

        if (slot?.Itemstack?.Block == null) {
            // if not block don't do anything for now
            return;
        }

        // The highlight is only done each N ms so calculate the placement each time
        // so it work if user flood click
        BlockPos pos = SmartPlacementGetPos();
        if (pos != null) {
            _capi.World.Player.Entity.StartAnimation("placeblock");
            _capi.World.RegisterCallback((dt) => { _capi.World.Player.Entity.StopAnimation("placeblock"); }, 120);
            clientChannel.SendPacket(new PlaceBlockMsg { X = pos.X, Y = pos.Y, Z = pos.Z });
        }
    }
}
}
