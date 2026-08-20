using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

using System.Linq;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace SmartCursor {

public class FirePitMatcher {

    private static BuildStageMaterial[] GetActiveMaterialsForStage(BlockEntityPitKiln pk, ICoreClientAPI capi) {
        var stageMaterials = pk.NextBuildStage.Materials;

        int stage = (int)typeof(BlockEntityPitKiln)
                        .GetField("currentBuildStage",
                                  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .GetValue(pk);

        // Stage 8 is the fuel one so below just return the current stage
        if (stage <= 7)
            return stageMaterials;

        // slots 0-3 is ground storage (clay)
        // slots 4+ = fuel/build materials
        for (int i = 4; i < pk.Inventory.Count; i++) {
            ItemSlot slot = pk.Inventory[i];
            if (slot.Empty)
                continue;

            var match = stageMaterials.FirstOrDefault(
                m => m.ItemStack.Equals(capi.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes));

            if (match != null) {
                // already committed to this material for the current layer
                return new[] { match };
            }
        }

        // nothing placed yet this stage so any option is valid
        return stageMaterials;
    }

    public static void Add(List<ItemMatcher> matchers, ICoreClientAPI capi) {
        BlockSelection sel = capi.World.Player.CurrentBlockSelection;
        var be = capi.World.BlockAccessor.GetBlockEntity(sel.Position);

        if (be is BlockEntityPitKiln pk) {
            if (!pk.IsComplete) {
                matchers.Add(new BuildStageMaterialMatcher(FirePitMatcher.GetActiveMaterialsForStage(pk, capi)));
            } else if (!pk.Lit) {
                matchers.Add(new ItemCodeMatcher("torch-basic-lit-up"));
            }
        } else if (be is BlockEntityGroundStorage gs) {
            foreach (var slot in gs.Inventory) {
                if (slot?.Itemstack?.Collectible?.Code?.Path != null) {
                    // TODO may be it's not enough
                    if (!(slot?.Itemstack?.Collectible?.Code?.Path?.EndsWith("-raw") ?? false)) {
                        return;
                    }
                }
            }
            matchers.Add(new ItemCodeMatcher("drygrass"));
        }
    }
}

public static class FirePitReflection {
    private static readonly System.Reflection.FieldInfo currentBuildStage =
        typeof(BlockEntityPitKiln)
            .GetField("currentBuildStage",
                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    public static int GetCurrentBuildStage(BlockEntityPitKiln pk) { return (int)currentBuildStage.GetValue(pk); }
}
}
