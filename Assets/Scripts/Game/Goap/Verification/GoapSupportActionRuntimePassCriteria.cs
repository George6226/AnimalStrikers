using System;
using System.Collections.Generic;

public sealed class GoapSupportActionRuntimePassResult
{
    public bool PatternPass;
    public bool ShouldEvaluate;
    public int TargetSlot = -1;
    public string DetailText;
}

/// <summary>単体検証時のランタイム合格判定（GoapDiag / GoapSummary を参照）。</summary>
public interface IGoapSupportActionRuntimePassCriteria
{
    GoapSupportActionUnderTest Action { get; }

    /// <summary>パターンごとに評価対象 slot を返す。false=SKIP。</summary>
    bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot);
}

public static class GoapSupportActionRuntimePassCriteria
{
    public static readonly IGoapSupportActionRuntimePassCriteria MoveToSupportPosition =
        new GoapMoveToSupportRuntimePassCriteria();

    public static readonly IGoapSupportActionRuntimePassCriteria CreateSupportAngle =
        new GoapCreateSupportAngleRuntimePassCriteria();

    public static readonly IGoapSupportActionRuntimePassCriteria GetOpen =
        new GoapGetOpenRuntimePassCriteria();

    public static readonly IGoapSupportActionRuntimePassCriteria WingOwnerDrive =
        new GoapWingOwnerDriveRuntimePassCriteria();

    public static readonly IGoapSupportActionRuntimePassCriteria CfOwnerDrive =
        new GoapCfOwnerDriveRuntimePassCriteria();
}

/// <summary>
/// MoveToSupport 単体合格基準:
/// 1. 翼保持パターン（Rw/Lw Owner）で slot0=非保持者 CF が対象
/// 2. GoapDiag [GOAP_MOVE][Support]: Execute start → Complete reason=tactical_in_position
/// 3. GoapSummary: ActionStart(action=MoveToSupportPosition)（補助）
/// FAIL: timeout / Cancel / Execute なし / tactical_in_position 未到達
/// </summary>
public sealed class GoapMoveToSupportRuntimePassCriteria : IGoapSupportActionRuntimePassCriteria
{
    public GoapSupportActionUnderTest Action => GoapSupportActionUnderTest.MoveToSupportPosition;

    public bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            return false;
        }

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (ownerSlot == 0)
        {
            return false;
        }

        slot = 0;
        return true;
    }
}

/// <summary>
/// CreateSupportAngle 単体合格基準:
/// 1. 非保持者の翼 slot1/2 が対象（CF 保持パターン中心）
/// 2. GoapDiag [GOAP_MOVE][SupportAngle]: Finish passReceive=true (tactical check passed)
/// </summary>
public sealed class GoapCreateSupportAngleRuntimePassCriteria : IGoapSupportActionRuntimePassCriteria
{
    public GoapSupportActionUnderTest Action => GoapSupportActionUnderTest.CreateSupportAngle;

    public bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// GetOpen 単体合格基準:
/// 1. CF 保持パターンで非保持翼 slot1/2 が対象
/// 2. GoapDiag [GOAP_MOVE][GetOpen]: Execute → Finish arrived=true
/// 3. GoapSummary: ActionStart(action=GetOpen)（補助）
/// SKIP: #6 AtCorrectLanes（理想レーン上＝GetOpen 不要）
/// </summary>
public sealed class GoapGetOpenRuntimePassCriteria : IGoapSupportActionRuntimePassCriteria
{
    public GoapSupportActionUnderTest Action => GoapSupportActionUnderTest.GetOpen;

    public bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom
            || pattern == GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes)
        {
            return false;
        }

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (ownerSlot != 0)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// 翼保持ドライブ追従（#17/#18）合格基準:
/// 1. slot0: [GOAP_MOVE][Support] Retarget が 1 回以上（保持者移動に追従）
/// 2. 非保持翼: [GOAP_MOVE][SupportAngle] Retarget が 1 回以上かつ
///    ContinueMoving または passReceive=true（レーン維持追従）
/// </summary>
public sealed class GoapWingOwnerDriveRuntimePassCriteria : IGoapSupportActionRuntimePassCriteria
{
    public const int MinRetargetCount = 1;

    public GoapSupportActionUnderTest Action => GoapSupportActionUnderTest.WingOwnerDriveFollow;

    public bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        return pattern == GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward
            || pattern == GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward;
    }
}

/// <summary>
/// CF 保持ドライブ追従（#13〜16）合格基準:
/// 両翼 slot1/2: [GOAP_MOVE][SupportAngle] Retarget が 1 回以上かつ
/// ContinueMoving または passReceive=true（レーン維持追従）
/// </summary>
public sealed class GoapCfOwnerDriveRuntimePassCriteria : IGoapSupportActionRuntimePassCriteria
{
    public const int MinRetargetCount = 1;

    public GoapSupportActionUnderTest Action => GoapSupportActionUnderTest.CfOwnerDriveFollow;

    public bool TryGetEvaluationSlot(GoapSupportLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        int number = GoapSupportLayoutPatternCatalog.GetNumber(pattern);
        return number >= 13 && number <= 16;
    }
}
