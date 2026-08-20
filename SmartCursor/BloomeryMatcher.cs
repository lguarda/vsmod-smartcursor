using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {
public class BloomeryMatcher {
    public static void GetBloomeryMatcher(List<ItemMatcher> matchers, ICoreClientAPI capi) {
        BlockSelection sel = capi.World.Player.CurrentBlockSelection;
        var be = capi.World.BlockAccessor.GetBlockEntity(sel.Position);
        if (be is not BlockEntityBloomery bloomery)
            return;

        var (fuelSlot, oreSlot, outSlot) = BloomeryReflection.GetSlots(bloomery);

        if (outSlot.StackSize > 0)
            return;
        if (bloomery.IsBurning)
            return;

        if (bloomery.CanIgnite()) {
            matchers.Add(new ItemCodeMatcher("torch-basic-lit-up"));
            return;
        }

        int oreCapacity = BloomeryReflection.GetOreCapacity(bloomery);
        bool containsOre = !oreSlot.Empty;

        if (oreSlot.StackSize >= oreCapacity)
        {
            // When the bloomery is full of ore, match any sufficient fuel
            matchers.Add(new CombustibleThresholdMatcher(capi, (combustibleProps, content) =>
                combustibleProps.BurnTemperature >= 1200 &&
                combustibleProps.BurnDuration > 30 // avoid weird stuff
            ));
        }
        else if (containsOre) {
            // Return same ore if already present
            string existingOreCode = oreSlot.Itemstack.Item.Code.Path;
            matchers.Add(new ItemCodeMatcher(existingOreCode));
        }
        else
        {
            // Ore match smeltable ore items within temperature limits
            matchers.Add(new CombustibleThresholdMatcher(capi, (combustibleProps, content) =>
                combustibleProps.SmeltedStack != null &&
                combustibleProps.MeltingPoint >= BlockEntityBloomery.MinTemp &&
                combustibleProps.MeltingPoint < BlockEntityBloomery.MaxTemp
            ));
        }
    }
}

public class CombustibleThresholdMatcher : ItemMatcher {
    private readonly ICoreClientAPI capi;
    private readonly Func<CombustibleProperties, ICoreClientAPI, bool> predicate;

    public CombustibleThresholdMatcher(ICoreClientAPI capi,
                                       Func<CombustibleProperties, ICoreClientAPI, bool> predicate) {
        this.capi = capi;
        this.predicate = predicate;
    }

    public override bool Matches(ItemSlot slot) {
        var stack = slot?.Itemstack;
        if (stack == null)
            return false;
        var props = stack.Collectible.GetCombustibleProperties(capi.World, stack, null);
        if (props == null)
            return false;
        return predicate(props, capi);
    }
}

public static class BloomeryReflection {
    private static readonly System.Reflection.FieldInfo InvField =
        typeof(BlockEntityBloomery)
            .GetField("bloomeryInv",
                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    private static readonly System.Reflection.PropertyInfo OreCapacityProp =
        typeof(BlockEntityBloomery)
            .GetProperty("OreCapacity",
                         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    public static (ItemSlot fuel, ItemSlot ore, ItemSlot output) GetSlots(BlockEntityBloomery bloomery) {
        var inv = (InventoryGeneric)InvField.GetValue(bloomery);
        // 0:fuelSlot 1:oreSlot 2:outSlot
        return (inv[0], inv[1], inv[2]);
    }

    public static int GetOreCapacity(BlockEntityBloomery bloomery) { return (int)OreCapacityProp.GetValue(bloomery); }
}
}
