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
public class PlaceBlockMsg
{
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

    public override void StartServerSide(ICoreServerAPI api) {

        api.Network.RegisterChannel("SmartCursor")
            .RegisterMessageType<PlaceBlockMsg>()
            .SetMessageHandler<PlaceBlockMsg>((player, msg) => {
                BlockPos pos = new BlockPos(msg.X, msg.Y, msg.Z);
                ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
                if (slot?.Itemstack?.Block != null)
                {
                    api.World.BlockAccessor.SetBlock(slot.Itemstack.Block.BlockId, pos);
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            });
    }

    private void HotKeyListener(string hotkeycode, KeyCombination keyComb) {
        switch (hotkeycode) {
        case SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT:
            SmartPlace();
            break;
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        clientChannel = api.Network.RegisterChannel("SmartCursor")
            .RegisterMessageType<PlaceBlockMsg>();


        SmartCursorKeybind.RegisterClientKey(_capi, SmartCursorKeybind.HOTKEY_SMARTCURSOR_PLACEMENT, GlKeys.F);
        _capi.Input.AddHotkeyListener(HotKeyListener);
    }


    static public BlockPos FindNextHorizontalPlacingPos(ICoreClientAPI capi) {
        EntityPos pos = capi.World.Player.Entity.Pos;

        // Block directly below player
        BlockPos blockPos = pos.AsBlockPos.DownCopy(); // or .Add(0, -1, 0)
        // Block block = api.World.BlockAccessor.GetBlock(blockPos);
        // return blockPos;

        BlockPos foundPos = null;

        double yaw = pos.Yaw;
        int dx = 0, dz = 0;

        // Normalize to 0-2π
        yaw = yaw % (2 * Math.PI);
        if (yaw < 0) yaw += 2 * Math.PI;

        // Convert to cardinal direction
        if (yaw < Math.PI / 4 || yaw >= 7 * Math.PI / 4) dz = 1; // North
        else if (yaw < 3 * Math.PI / 4) dx = 1; // East
        else if (yaw < 5 * Math.PI / 4) dz = -1; // South
        else dx = -1; // West

        for (int i = 1; i <= 5; i++) {
            BlockPos checkPos = new BlockPos(blockPos.X+dx*i, blockPos.Y, blockPos.Z+dz*i);
            Block block = capi.World.BlockAccessor.GetBlock(checkPos);

            if (block.BlockMaterial == EnumBlockMaterial.Air) {
                foundPos = checkPos;
                break;
            }
        }
        return foundPos;
    }

    static public void PlaceActiveSlotAt(ICoreClientAPI capi, BlockPos targetPos) {
        if (targetPos == null) {
            return;
        }
        // Get the slot with the block you want to place
        ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;

        capi.ShowChatMessage($"Slot: {slot != null}");
        capi.ShowChatMessage($"ItemStack: {slot?.Itemstack != null}");
        capi.ShowChatMessage($"Item: {slot?.Itemstack?.Collectible?.Code}");
        capi.ShowChatMessage($"Target pos: {targetPos}");

        IClientPlayer player = capi.World.Player;

    }

    public void SmartPlace() {
        //DebugHighlightBlock(SmartPlacement.FindNextHorizontalPlacingPos(_capi));
        BlockPos pos = SmartPlacement.FindNextHorizontalPlacingPos(_capi);
        if (pos != null) {
           _capi.Logger.Debug($"OMG 1 {pos.X} {pos.Y} {pos.Z}");

           _capi.World.Player.Entity.StartAnimation("placeblock");
           // Stop animation after delay (300ms for example)
            _capi.World.RegisterCallback((dt) => {
                _capi.World.Player.Entity.StopAnimation("placeblock");
            }, 120);
           clientChannel.SendPacket(new PlaceBlockMsg {
                X = pos.X,
                Y = pos.Y,
                Z = pos.Z
            });
           _capi.Logger.Debug($"OMG 2");
        }

    }

}
}
