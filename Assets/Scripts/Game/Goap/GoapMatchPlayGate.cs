using UnityEngine;

/// <summary>
/// 試合プレイ中（GAME）のみ GOAP を動かすゲート。
/// キックオフ前 READY などでは計画・移動を止め、NoGoal スパムと無駄なプランニングを防ぐ。
/// </summary>
public static class GoapMatchPlayGate
{
    /// <summary>
    /// 本番マッチで GOAP を動かしてよい状態か。
    /// バッチ/検証モードは常に true。StateManager 不在（EditMode）も true（既存テスト互換）。
    /// </summary>
    public static bool IsMatchPlayActive()
    {
        if (GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive)
        {
            return true;
        }

        // StateManager.Instance は不在時に Error を出すため、Find で存在確認する。
        var stateManager = Object.FindObjectOfType<StateManager>();
        if (stateManager == null)
        {
            return true;
        }

        if (!stateManager.isSameKind(StateManager.STATE_KIND.GAME))
        {
            return false;
        }

        // 6-F P0: 得点直後のリスタート中は GOAP を止め、配置完了後に MatchPlayStarted replan へ。
        return !PostGoalRestartGate.IsGoapPlanningSuppressed();
    }
}
