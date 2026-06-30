using System.Collections.Generic;
using UnityEngine;

public static class GoapDefenseLayoutPatternLibrary
{
    public static int GetEnemyBallOwnerIndex(GoapDefenseLayoutPatternId pattern) => 0;

    public static GoapDefenseLayoutPatternId ResolveBasePattern(GoapDefenseLayoutPatternId pattern) =>
        pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward =>
                GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies,
            GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward =>
                GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield,
            _ => pattern,
        };

    public static bool TryGetFieldContext(GoapSupportLayoutTuning tuning, out GoapSupportLayoutFieldContext ctx) =>
        GoapSupportLayoutPatternLibrary.TryGetFieldContext(tuning, out ctx);

    public static Vector3 ApproximateOwnGoal(GoapSupportLayoutFieldContext ctx) =>
        ctx.FieldCenter - ctx.ToGoal * (ctx.FieldLength * 0.45f);

    public static Vector3 ResolveEnemyOwnerPosition(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning)
    {
        GoapDefenseLayoutPatternId layoutPattern = ResolveBasePattern(pattern);
        Vector3 ownGoal = ApproximateOwnGoal(ctx);

        return layoutPattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane =>
                ownGoal + ctx.ToGoal * (ctx.FieldLength * 0.1f),
            _ => ctx.FieldCenter + ctx.ToGoal * (ctx.FieldLength * tuning.OwnerForwardRatio),
        };
    }

    public static Dictionary<int, Vector3> ComputeAllyTargets(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning,
        Vector3? enemyOwnerAnchor = null)
    {
        Vector3 enemyOwner = enemyOwnerAnchor
            ?? ResolveEnemyOwnerPosition(ResolveBasePattern(pattern), ctx, tuning);
        return ComputeAllyTargetsForLayout(ResolveBasePattern(pattern), ctx, tuning, enemyOwner);
    }

    private static Dictionary<int, Vector3> ComputeAllyTargetsForLayout(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning,
        Vector3 enemyOwner)
    {
        var map = new Dictionary<int, Vector3>();
        float wingLane = ctx.FieldWidth * 0.28f;
        Vector3 ownGoal = ApproximateOwnGoal(ctx);
        float pressRadius = ctx.FieldLength * 0.1f;

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

            case GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget:
                Vector3 markBackLine = ctx.FieldCenter - ctx.ToGoal * (ctx.FieldLength * 0.12f);
                map[0] = markBackLine;
                map[1] = markBackLine + ctx.Right * wingLane;
                map[2] = markBackLine - ctx.Right * wingLane;
                break;

            case GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane:
                map[0] = enemyOwner + ctx.Right * pressRadius * 0.35f;
                map[1] = enemyOwner - ctx.Right * pressRadius * 0.35f;
                map[2] = ctx.FieldCenter - ctx.ToGoal * (ctx.FieldLength * 0.1f);
                break;

            case GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane:
                map[0] = ownGoal + ctx.Right * (ctx.FieldWidth * 0.12f);
                map[1] = ctx.FieldCenter;
                map[2] = ctx.FieldCenter - ctx.Right * wingLane;
                break;
        }

        return map;
    }

    public static bool TryGetSecondaryEnemyTarget(
        int enemyIndex,
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning,
        out Vector3 position,
        Vector3? enemyOwnerAnchor = null)
    {
        position = default;
        if (enemyIndex != 1)
        {
            return false;
        }

        GoapDefenseLayoutPatternId layoutPattern = ResolveBasePattern(pattern);
        Vector3 enemyOwner = enemyOwnerAnchor
            ?? ResolveEnemyOwnerPosition(layoutPattern, ctx, tuning);
        float wingLane = ctx.FieldWidth * 0.22f;

        switch (layoutPattern)
        {
            case GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget:
                position = enemyOwner + ctx.Right * wingLane + ctx.ToGoal * (ctx.FieldLength * 0.06f);
                return true;

            case GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane:
                position = enemyOwner - ctx.ToGoal * (ctx.FieldLength * 0.14f) + ctx.Right * wingLane;
                return true;

            default:
                return false;
        }
    }

    public static Vector3 ComputeEnemyOwnerTarget(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning) =>
        ResolveEnemyOwnerPosition(pattern, ctx, tuning);
}
