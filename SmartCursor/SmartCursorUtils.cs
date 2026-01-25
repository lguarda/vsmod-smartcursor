using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;

using Vintagestory.API.MathTools;
//
namespace SmartCursor {
public class SmartCursorUtils {

    static public void DebugHighlightBlock(ICoreClientAPI capi, BlockPos pos) {
        if (pos != null) {
            capi.World.HighlightBlocks(capi.World.Player, 123, new List<BlockPos> { pos });
        }
    }

    static public void DebugDrawPoint(ICoreClientAPI capi, Vec3d pos) {
        SimpleParticleProperties p =
            new SimpleParticleProperties(1,                                // float minQuantity,
                                         1,                                // float maxQuantity
                                         ColorUtil.ToRgba(255, 255, 0, 0), // int color
                                         pos,                              // Vec3d minPos
                                         pos,                              // Vec3d maxPos
                                         Vec3f.Zero,                       // Vec3f minVelocity
                                         Vec3f.Zero,                       // Vec3f maxVelocity
                                         6f,                               // float lifeLength = 1
                                         0,                                // float gravityEffect = 1
                                         0.5f,                             // float minSize = 1
                                         0.5f,                             // float maxSize = 1
                                         EnumParticleModel.Cube // EnumParticleModel model = EnumParticleModel.Cube)
            );
        p.WithTerrainCollision = false;
        capi.World.SpawnParticles(p);
    }

    static public void RayTrace(ICoreClientAPI capi, Vec3d pos, Vec3d dir, double step, double dist,
                                Func<Vec3d, bool> fn) {
        double maxDistance = dist;
        for (dist = 0; dist <= maxDistance; dist += step) {
            Vec3d p = new Vec3d((pos.X + dir.X * dist), (pos.Y + dir.Y * dist), (pos.Z + dir.Z * dist));
            if (fn(p)) {
                break;
            }
        }
    }
}
}
