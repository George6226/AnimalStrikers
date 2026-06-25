using System.Collections.Generic;
using UnityEngine;

public static class GoapDefenseLayoutPatternLibrary
{
    public static int GetEnemyBallOwnerIndex(GoapDefenseLayoutPatternId pattern) => 0;

    public static bool TryGetFieldContext(GoapSupportLayoutTuning tuning, out GoapSupportLayoutFieldContext ctx) =>
        GoapSupportLayoutPatternLibrary.TryGetFieldContext(tuning, out ctx);

    public static Vector3 ResolveEnemyOwnerPosition(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning)
    {
        return ctx.FieldCenter + ctx.ToGoal * (ctx.FieldLength * tuning.OwnerForwardRatio);
    }

    public static Dictionary<int, Vector3> ComputeAllyTargets(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning)
    {
        var map = new Dictionary<int, Vector3>();
        Vector3 enemyOwner = ResolveEnemyOwnerPosition(pattern, ctx, tuning);
        float wingLane = ctx.FieldWidth * 0.28f;

        switch (pattern)
        {
            case GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies:
                map[0] = enemyOwner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio);
                map[1] = enemyOwner
                    + ctx.Right * (ctx.FieldWidth * tuning.ClusterLateralRatio)
                    + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio * 0.5f);
                map[2] = enemyOwner
                    - ctx.Right * (ctx.FieldWidth * tuning.ClusterLateralRatio)
                    + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio * 0.5f);
                break;

            case GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield:
                map[0] = enemyOwner + ctx.ToGoal * (ctx.FieldLength * 0.05f);
                map[1] = enemyOwner + ctx.Right * wingLane + ctx.ToGoal * (ctx.FieldLength * 0.03f);
                map[2] = enemyOwner - ctx.Right * wingLane + ctx.ToGoal * (ctx.FieldLength * 0.03f);
                break;
        }

        return map;
    }

    public static Vector3 ComputeEnemyOwnerTarget(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning) =>
        ResolveEnemyOwnerPosition(pattern, ctx, tuning);
}
