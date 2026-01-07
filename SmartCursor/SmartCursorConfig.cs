using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

// thanks to
// https://github.com/Xandu93/VSMods/blob/master/mods/xinvtweaks/src/InvTweaksConfig.cs
// for the inspiration

namespace SmartCursor {
public class SmartCursorConfig {
    public Dictionary<string, string[]> materialTools = new Dictionary<string, string[]>();
    public Dictionary<string, string[]> domainTools = new Dictionary<string, string[]>();
    public bool continuousMode;

    public SmartCursorConfig() {
        // Set the default value
        continuousMode = true;

        domainTools = new() {
            // We don't want Scythe for mushroom
            ["mushroom"] = new[] { "Knife" },
            // Actualy most of the time hammer are not in inventory so let's still
            // pick the pickaxe first
            // but go to hammer if there's no pickaxe
            ["anvil"] = new[] { "Pickaxe", "Hammer" },
        };
        materialTools = new() {
            ["Gravel"] = new[] { "Shovel" },
            ["Sand"] = new[] { "Shovel" },
            ["Snow"] = new[] { "Shovel" },
            ["Soil"] = new[] { "Shovel" },

            ["Metal"] = new[] { "Pickaxe" },
            ["Ore"] = new[] { "Pickaxe" },
            ["Stone"] = new[] { "Pickaxe" },
            ["Ice"] = new[] { "Pickaxe" },
            ["Glass"] = new[] { "Pickaxe" },
            ["Brick"] = new[] { "Pickaxe" },
            ["Ceramic"] = new[] { "Pickaxe" },

            ["Wood"] = new[] { "Axe" },
            ["Plant"] = new[] { "Scythe", "Knife" },
            ["Leaves"] = new[] { "Shears", "Axe" },
            // Liquid = 8
            // TODO Add Bucket and bowl
            // Air = 0
            // Cloth = 16
            // Fire = 19
            // Lava = 17
            // Mantle = 12
            // Meta = 20
            // Other = 21
        };
    }
}
}
