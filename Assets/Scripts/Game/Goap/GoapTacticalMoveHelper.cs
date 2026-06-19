using UnityEngine;
using Game.Goap;

/// <summary>
/// 戦術移動アクション共通（GoapNpcMotor + ゴール用ファクト更新）。
/// </summary>
public static class GoapTacticalMoveHelper
{
    public static bool TryResolveMotor(PlayerBlackboard bb)
    {
        return GoapNpcMotor.TryResolve(bb, out _, out _, out _);
    }

    public static bool MoveToward(
        PlayerBlackboard bb,
        Vector3 target,
        float moveIntensity,
        string category,
        float arriveDistance = 0.65f)
    {
        if (bb == null)
        {
            return false;
        }

        float dist = Vector3.Distance(GoapNpcMotor.GetSelfWorldPosition(bb), target);
        if (dist <= arriveDistance)
        {
            return true;
        }

        GoapNpcMotor.MoveToward(bb, target, moveIntensity, category);
        return false;
    }

    public static void Stop(PlayerBlackboard bb, string category)
    {
        if (bb != null)
        {
            GoapNpcMotor.Stop(bb, category);
        }
    }

    public static void ApplyPassReceiveFact(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return;
        }

        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "false"), false);
    }

    public static void ApplyDefensivePositionFact(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return;
        }

        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "false"), false);
        bb.SetFact(new Fact(SymbolTag.Basic.IS_MOVING, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Basic.IS_MOVING, "false"), false);
    }
}
