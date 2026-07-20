/// <summary>
/// 攻守所有権フラグの変化に対する AIContextSwitcher の Abort 判定。
/// PASS/FREE（両方 false）を挟む遷移は GoapAgent の BallContextChanged 再計画に委ねる。
/// </summary>
public static class PossessionContextSwitchRules
{
    public static bool IsNeutralPossession(bool teamHasBall, bool enemyHasBall) =>
        !teamHasBall && !enemyHasBall;

    /// <summary>
    /// チーム↔敵の直接交代のみ Abort。中立（PASS/FREE）を経由する変化は抑止する。
    /// </summary>
    public static bool ShouldAbortOnPossessionChange(
        bool lastTeamHasBall,
        bool lastEnemyHasBall,
        bool nowTeamHasBall,
        bool nowEnemyHasBall)
    {
        if (IsNeutralPossession(lastTeamHasBall, lastEnemyHasBall)
            || IsNeutralPossession(nowTeamHasBall, nowEnemyHasBall))
        {
            return false;
        }

        return lastTeamHasBall != nowTeamHasBall || lastEnemyHasBall != nowEnemyHasBall;
    }
}
