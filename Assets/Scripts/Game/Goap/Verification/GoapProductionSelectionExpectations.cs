public interface IGoapProductionSelectionExpectation
{
    bool TryGetExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate);
}

public static class GoapProductionSelectionExpectations
{
    public static readonly IGoapProductionSelectionExpectation CreateSupportAngle =
        new GoapCreateSupportAngleProductionSelectionExpectation();

    public static readonly IGoapProductionSelectionExpectation MoveToSupportPosition =
        new GoapMoveToSupportProductionSelectionExpectation();

    public static readonly IGoapProductionSelectionExpectation GetOpen =
        new GoapGetOpenProductionSelectionExpectation();

    public static readonly IGoapProductionSelectionExpectation CombinedSupportRegression =
        new GoapCombinedSupportRegressionExpectation();
}

/// <summary>CSA 本番選出: slot1/2=CreateSupportAngle, slot0=MoveToSupportPosition。</summary>
public sealed class GoapCreateSupportAngleProductionSelectionExpectation : IGoapProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            return true;
        }

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (slot == ownerSlot)
        {
            return true;
        }

        if ((pattern == GoapSupportLayoutPatternId.RwOwner_WingHold
                || pattern == GoapSupportLayoutPatternId.LwOwner_WingHold)
            && slot == 0)
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = slot == 0 ? "MoveToSupportPosition" : "CreateSupportAngle";
        return true;
    }
}

/// <summary>MoveToSupport 本番選出: 非保持者 slot0=MoveToSupportPosition。翼・保持者は評価対象外。</summary>
public sealed class GoapMoveToSupportProductionSelectionExpectation : IGoapProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            return true;
        }

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (slot == ownerSlot || slot == 1 || slot == 2)
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = "MoveToSupportPosition";
        return true;
    }
}

/// <summary>GetOpen 本番選出: CF 保持時 slot1/2=GetOpen。#6 AtCorrectLanes は SKIP。</summary>
public sealed class GoapGetOpenProductionSelectionExpectation : IGoapProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom
            || pattern == GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes)
        {
            return true;
        }

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (ownerSlot != 0 || slot == ownerSlot)
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = "GetOpen";
        return true;
    }
}

/// <summary>
/// GetOpen + CSA + MTS 統合本番選出: パターンごとに期待アクションを切替。
/// #6 → slot1/2=CSA / #8,#9 → slot0=MTS + 非保持翼=CSA / それ以外 #2〜12 → GetOpen。
/// </summary>
public sealed class GoapCombinedSupportRegressionExpectation : IGoapProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            return true;
        }

        if (pattern == GoapSupportLayoutPatternId.RwOwner_WingHold
            || pattern == GoapSupportLayoutPatternId.LwOwner_WingHold)
        {
            return TryGetWingHoldExpectation(pattern, slot, out expectedAction, out shouldEvaluate);
        }

        if (pattern == GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes)
        {
            return TryGetCfOwnerWingExpectation(pattern, slot, out expectedAction, out shouldEvaluate);
        }

        if (GoapSupportLayoutPatternCatalog.IsCombinedSupportRegressionGetOpenPattern(pattern))
        {
            return GoapProductionSelectionExpectations.GetOpen.TryGetExpectation(
                pattern,
                slot,
                out expectedAction,
                out shouldEvaluate);
        }

        return true;
    }

    private static bool TryGetWingHoldExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (slot == ownerSlot)
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = slot == 0 ? "MoveToSupportPosition" : "CreateSupportAngle";
        return true;
    }

    private static bool TryGetCfOwnerWingExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        if (slot == ownerSlot)
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = "CreateSupportAngle";
        return true;
    }
}
