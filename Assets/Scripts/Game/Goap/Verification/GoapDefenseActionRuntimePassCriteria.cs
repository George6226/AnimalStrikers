public sealed class GoapDefenseActionRuntimePassResult
{
    public bool PatternPass;
    public bool ShouldEvaluate;
    public int TargetSlot = -1;
    public string DetailText;
}

/// <summary>守備単体検証時のランタイム合格判定（GoapDiag / GoapSummary を参照）。</summary>
public interface IGoapDefenseActionRuntimePassCriteria
{
    GoapDefenseActionUnderTest Action { get; }

    bool TryGetEvaluationSlot(GoapDefenseLayoutPatternId pattern, out int slot);
}

public static class GoapDefenseActionRuntimePassCriteria
{
    public static readonly IGoapDefenseActionRuntimePassCriteria MoveToDefensivePosition =
        new GoapMoveToDefensivePositionRuntimePassCriteria();

    public static readonly IGoapDefenseActionRuntimePassCriteria EnemyOwnerDrive =
        new GoapEnemyOwnerDriveRuntimePassCriteria();
}

/// <summary>
/// MoveToDefensivePosition 単体: [GOAP_MOVE][Defend] Execute start → Retarget 1回以上。
/// </summary>
public sealed class GoapMoveToDefensivePositionRuntimePassCriteria : IGoapDefenseActionRuntimePassCriteria
{
    public const int MinRetargetCount = 1;

    public GoapDefenseActionUnderTest Action => GoapDefenseActionUnderTest.MoveToDefensivePosition;

    public bool TryGetEvaluationSlot(GoapDefenseLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        if (pattern == GoapDefenseLayoutPatternId.Baseline
            || pattern == GoapDefenseLayoutPatternId.Custom)
        {
            return false;
        }

        slot = 0;
        return true;
    }
}

/// <summary>
/// 敵保持ドライブ追従（#7/#8/#9）: #7/#8 は Defend Retarget、#9 は RetreatLine Execute。
/// </summary>
public sealed class GoapEnemyOwnerDriveRuntimePassCriteria : IGoapDefenseActionRuntimePassCriteria
{
    public const int MinRetargetCount = 1;

    public GoapDefenseActionUnderTest Action => GoapDefenseActionUnderTest.EnemyOwnerDriveFollow;

    public bool TryGetEvaluationSlot(GoapDefenseLayoutPatternId pattern, out int slot)
    {
        slot = -1;
        return pattern == GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward
            || pattern == GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward
            || pattern == GoapDefenseLayoutPatternId.EnemyOwner_RetreatToDefensiveLine;
    }
}
