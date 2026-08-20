using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.GameContent;
using System.Linq;

namespace SmartCursor {
public class PitKilnRule : AbstractRule {
    public PitKilnRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) {}

    public override string BuildSignature(BlockSelection sel, Block block, BlockEntity be) {
        if (be is BlockEntityPitKiln pk) {
            int stage = PitKilnReflection.GetCurrentBuildStage(pk);
            return $"pitkiln|{stage}|{pk.IsComplete}|{pk.Lit}";
        }
        return null;
    }

    private BuildStageMaterial[] GetActiveMaterialsForStage(BlockEntityPitKiln pk) {
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
                m => m.ItemStack.Equals(_capi.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes));

            if (match != null) {
                // already committed to this material for the current layer
                return new[] { match };
            }
        }

        // nothing placed yet this stage so any option is valid
        return stageMaterials;
    }

    public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item) {
        if (be is BlockEntityPitKiln pk) {
            if (!pk.IsComplete) {
                matchers.Add(new BuildStageMaterialMatcher(GetActiveMaterialsForStage(pk)));
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

public static class PitKilnReflection {
    private static readonly System.Reflection.FieldInfo currentBuildStage =
        typeof(BlockEntityPitKiln)
            .GetField("currentBuildStage",
                      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    public static int GetCurrentBuildStage(BlockEntityPitKiln pk) { return (int)currentBuildStage.GetValue(pk); }
}
}
