using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using Vintagestory.GameContent;
using System;
using Vintagestory.API.MathTools;

namespace SmartCursor {
public class ToolRule : AbstractRule {
    Dictionary<EnumBlockMaterial, EnumTool[]> _materialTools;
    Dictionary<string, EnumTool[]> _domainTools;

    private EnumTool[] StringsToEnumToolArray(string[] tools) {
        EnumTool[] result = new EnumTool[tools.Length];

        for (int i = 0; i < tools.Length; i++) {
            if (!Enum.TryParse(tools[i], ignoreCase: true, out result[i])) {
                _capi.Logger.Notification($"Invalid tool enum: {tools[i]}");
            }
        }

        return result;
    }

    private void parseMaterialTools(SmartCursorConfig config) {
        _materialTools = new Dictionary<EnumBlockMaterial, EnumTool[]>();
        foreach (var kv in config.materialTools) {
            EnumBlockMaterial material;
            if (Enum.TryParse(kv.Key, ignoreCase: true, out material)) {
                _materialTools[material] = StringsToEnumToolArray(kv.Value);
            }
        }
    }

    private void parseDomainTools(SmartCursorConfig config) {
        _domainTools = new Dictionary<string, EnumTool[]>();

        foreach (var kv in config.domainTools) {
            _domainTools[kv.Key] = StringsToEnumToolArray(kv.Value);
        }
    }

    public override void Setup(SmartCursorConfig config) {
        parseMaterialTools(config);
        parseDomainTools(config);
    }

    public ToolRule(SmartCursorConfig config, ICoreClientAPI api) : base(config, api) {}

    public override void Run(List<ItemMatcher> matchers, BlockSelection sel, Block block, BlockEntity be, ItemStack item) {
        if (block == null)
            return;

        string prefix = block.Code?.Path is string p ? (p.IndexOf('-') is int i && i >= 0 ? p[..i] : p) : null;

        EnumTool[] tools;
        if (_domainTools.TryGetValue(prefix, out tools)) {
        } else if (_materialTools.TryGetValue(block.BlockMaterial, out tools)) {
        }
        if (tools != null) {
            foreach (var tool in tools) {
                matchers.Add(new ToolTypeMatcher(tool));
            }
        }
    }
}
}
