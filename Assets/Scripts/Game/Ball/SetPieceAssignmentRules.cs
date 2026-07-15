using UnityEngine;

/// <summary>
/// 6-B P0: セットプレイ再始動者の解決ルール（純関数中心・Kickoff は GK 除外のため別系統）。
/// </summary>
public static class SetPieceAssignmentRules
{
    /// <summary>ゴールキックは守備側 GK。</summary>
    public static AnimalFacade FindRestartingGoalkeeper(bool restartTeamIsOther)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        var candidates = restartTeamIsOther ? regist.Enemies : regist.Allys;
        foreach (var facade in candidates)
        {
            if (facade != null && facade.IsGK())
            {
                return facade;
            }
        }

        return null;
    }

    /// <summary>
    /// スローイン／コーナーはフィールド選手（編成スロット先頭・GK除外）。
    /// Kickoff と同型のリーダー解決を再利用。
    /// </summary>
    public static AnimalFacade FindRestartingFieldPlayer(bool restartTeamIsOther, int formationSlot = 0) =>
        BallKickoffAssignment.FindFormationSlotLeader(restartTeamIsOther, formationSlot);

    public static AnimalFacade FindRestartingPlayer(OutOfPlayClassifier.Result classify, int formationSlot = 0)
    {
        if (!classify.IsOutOfPlay || !classify.HasRestartTeam)
        {
            return null;
        }

        return classify.Kind == SetPieceKind.GoalKick
            ? FindRestartingGoalkeeper(classify.RestartTeamIsOther)
            : FindRestartingFieldPlayer(classify.RestartTeamIsOther, formationSlot);
    }

    /// <summary>ゴールキック再開位置（ゴール前ホーム深さ付近）。</summary>
    public static Vector3 ResolveGoalKickBallPosition(
        TeamFieldInfo field,
        bool restartTeamIsOther,
        float homeDepth)
    {
        if (field == null)
        {
            return Vector3.zero;
        }

        Vector3 defendGoal = restartTeamIsOther
            ? field.EnemyGoalPosition
            : field.OwnGoalPosition;
        Vector3 towardCenter = field.FieldCenter - defendGoal;
        towardCenter.y = 0f;
        if (towardCenter.sqrMagnitude < 0.001f)
        {
            towardCenter = restartTeamIsOther ? Vector3.back : Vector3.forward;
        }

        towardCenter.Normalize();
        float depth = Mathf.Max(0.5f, homeDepth);
        return defendGoal + towardCenter * depth;
    }
}
