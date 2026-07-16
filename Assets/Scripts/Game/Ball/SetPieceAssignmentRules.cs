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

    /// <summary>スローイン再開: サイド線付近に最も近いフィールド選手（GK除外）。</summary>
    public static AnimalFacade FindNearestRestartingFieldPlayer(
        bool restartTeamIsOther,
        Vector3 nearWorld)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        var candidates = restartTeamIsOther ? regist.Enemies : regist.Allys;
        AnimalFacade best = null;
        float bestSq = float.MaxValue;
        foreach (var facade in candidates)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            Vector3 pos = facade.transform.position;
            float dx = pos.x - nearWorld.x;
            float dz = pos.z - nearWorld.z;
            float sq = dx * dx + dz * dz;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = facade;
            }
        }

        return best ?? FindRestartingFieldPlayer(restartTeamIsOther);
    }

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

    /// <summary>スローイン再開位置（サイド線上・Z はアウト位置をフィールド内にクランプ）。</summary>
    public static Vector3 ResolveThrowInBallPosition(
        TeamFieldInfo field,
        float sideSignX,
        float ballWorldZ)
    {
        if (field == null)
        {
            return Vector3.zero;
        }

        float halfW = field.FieldWidth * 0.5f;
        float halfL = field.FieldLength * 0.5f;
        float sign = Mathf.Sign(sideSignX);
        if (Mathf.Approximately(sign, 0f))
        {
            sign = 1f;
        }

        float x = field.FieldCenter.x + sign * halfW;
        float z = Mathf.Clamp(
            ballWorldZ,
            field.FieldCenter.z - halfL,
            field.FieldCenter.z + halfL);
        return new Vector3(x, 0.5f, z);
    }
}
