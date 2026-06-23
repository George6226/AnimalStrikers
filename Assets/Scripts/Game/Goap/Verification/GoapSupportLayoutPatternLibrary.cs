using System.Collections.Generic;
using UnityEngine;

public static class GoapSupportLayoutPatternLibrary
{
    public static GoapSupportLayoutPatternId ResolveLayoutBasePattern(GoapSupportLayoutPatternId pattern)
    {
        return pattern switch
        {
            GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForward => GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes,
            GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack => GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes,
            GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward => GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes,
            GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveLateralRight => GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes,
            GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward => GoapSupportLayoutPatternId.RwOwner_WingHold,
            GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward => GoapSupportLayoutPatternId.LwOwner_WingHold,
            _ => pattern,
        };
    }

    public static int GetBallOwnerSlotForPattern(GoapSupportLayoutPatternId pattern, int defaultOwnerSlot)
    {
        return ResolveLayoutBasePattern(pattern) switch
        {
            GoapSupportLayoutPatternId.RwOwner_WingHold => 1,
            GoapSupportLayoutPatternId.LwOwner_WingHold => 2,
            _ => defaultOwnerSlot,
        };
    }

    public static bool TryGetFieldContext(GoapSupportLayoutTuning tuning, out GoapSupportLayoutFieldContext ctx)
    {
        ctx = default;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || tuning == null)
        {
            return false;
        }

        var field = teamBB.FieldInfo;
        Vector3 toGoal = field.EnemyGoalPosition - field.FieldCenter;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        toGoal.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        Vector3 ownerAnchor = field.FieldCenter + toGoal * (field.FieldLength * tuning.OwnerForwardRatio);

        ctx = new GoapSupportLayoutFieldContext
        {
            OwnerAnchor = ownerAnchor,
            FieldCenter = field.FieldCenter,
            FieldLength = field.FieldLength,
            FieldWidth = field.FieldWidth,
            ToGoal = toGoal,
            Right = right,
        };
        return true;
    }

    public static Vector3 ResolvePatternOwnerPosition(
        GoapSupportLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning)
    {
        pattern = ResolveLayoutBasePattern(pattern);
        Vector3 central = ctx.FieldCenter + ctx.ToGoal * (ctx.FieldLength * tuning.OwnerForwardRatio);
        float wingLateral = ctx.FieldWidth * tuning.WingSideOwnerLateralRatio;

        return pattern switch
        {
            GoapSupportLayoutPatternId.RwOwner_WingHold => central + ctx.Right * wingLateral,
            GoapSupportLayoutPatternId.LwOwner_WingHold => central - ctx.Right * wingLateral,
            GoapSupportLayoutPatternId.CfOwner_OnRightWing => central + ctx.Right * wingLateral,
            GoapSupportLayoutPatternId.CfOwner_OnLeftWing => central - ctx.Right * wingLateral,
            _ => ctx.OwnerAnchor,
        };
    }

    public static Dictionary<int, Vector3> ComputeTargets(
        GoapSupportLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        GoapSupportLayoutTuning tuning)
    {
        pattern = ResolveLayoutBasePattern(pattern);
        var map = new Dictionary<int, Vector3>();
        Vector3 owner = ResolvePatternOwnerPosition(pattern, ctx, tuning);
        float wingLane = ctx.FieldWidth * 0.30f;

        switch (pattern)
        {
            case GoapSupportLayoutPatternId.CfOwner_Clustered:
                map[0] = owner;
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    + ctx.Right * (ctx.FieldWidth * tuning.ClusterLateralRatio);
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    - ctx.Right * (ctx.FieldWidth * tuning.ClusterLateralRatio);
                break;

            case GoapSupportLayoutPatternId.CfOwner_RwWrongSide:
                map[0] = owner;
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    - ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                // slot2 も理想 LW ではなく逆サイドへ（GetOpen 検証で両翼が動くよう）
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    + ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                break;

            case GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide:
                map[0] = owner;
                // slot1 も理想 RW ではなく逆サイドへ
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    - ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    + ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                break;

            case GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes:
                map[0] = owner;
                map[1] = LaneTargetBackOffset(ctx, 1, tuning);
                map[2] = LaneTargetBackOffset(ctx, 2, tuning);
                break;

            case GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes:
                map[0] = owner;
                map[1] = LaneTargetIdeal(ctx, 1);
                map[2] = LaneTargetIdeal(ctx, 2);
                break;

            case GoapSupportLayoutPatternId.CfOwner_AllOverlapped:
                map[0] = owner;
                map[1] = owner + ctx.Right * (ctx.FieldWidth * tuning.OverlapMicroLateralRatio)
                    + ctx.ToGoal * (ctx.FieldLength * tuning.OverlapMicroForwardRatio);
                map[2] = owner - ctx.Right * (ctx.FieldWidth * tuning.OverlapMicroLateralRatio)
                    + ctx.ToGoal * (ctx.FieldLength * tuning.OverlapMicroForwardRatio * 0.5f);
                break;

            case GoapSupportLayoutPatternId.RwOwner_WingHold:
                map[1] = owner;
                map[0] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio * 0.5f);
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    + ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                break;

            case GoapSupportLayoutPatternId.LwOwner_WingHold:
                map[2] = owner;
                map[0] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio * 0.5f);
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    - ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio);
                break;

            case GoapSupportLayoutPatternId.CfOwner_OnRightWing:
                map[0] = owner;
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    + ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio * 0.5f);
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.NearLaneBackOffsetRatio)
                    - ctx.Right * wingLane;
                break;

            case GoapSupportLayoutPatternId.CfOwner_OnLeftWing:
                map[0] = owner;
                map[1] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.NearLaneBackOffsetRatio)
                    + ctx.Right * wingLane;
                map[2] = owner + ctx.ToGoal * (ctx.FieldLength * tuning.ClusterForwardRatio)
                    - ctx.Right * (ctx.FieldWidth * tuning.WrongSideLateralRatio * 0.5f);
                break;

            case GoapSupportLayoutPatternId.CfOwner_WingsTooDeepBehind:
                map[0] = owner;
                map[1] = owner - ctx.ToGoal * (ctx.FieldLength * tuning.BehindOwnerBackOffsetRatio)
                    + ctx.Right * wingLane;
                map[2] = owner - ctx.ToGoal * (ctx.FieldLength * tuning.BehindOwnerBackOffsetRatio)
                    - ctx.Right * wingLane;
                break;
        }

        return map;
    }

    private static Vector3 LaneTargetIdeal(GoapSupportLayoutFieldContext ctx, int slot)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return ctx.OwnerAnchor;
        }

        return CreateSupportAnglePositioning.SelectBestPosition(
            ctx.OwnerAnchor,
            slot,
            teamBB,
            CreateSupportAnglePositioning.CreateDefaultSettings());
    }

    private static Vector3 LaneTargetBackOffset(
        GoapSupportLayoutFieldContext ctx,
        int slot,
        GoapSupportLayoutTuning tuning)
    {
        Vector3 ideal = LaneTargetIdeal(ctx, slot);
        return ideal - ctx.ToGoal * (ctx.FieldLength * tuning.NearLaneBackOffsetRatio);
    }
}
