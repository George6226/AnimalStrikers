using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キック時にロブが必要か（味方／敵がパスレーン上にいるか）を判定する。
/// GOAP の <see cref="PlayerBlackboardCalculator.IsPassRouteClear"/> と整合した敵レーン判定を使う。
/// </summary>
public static class PassLaneKickPolicy
{
    private const float TeammateLaneRadius = 1.0f;

    public static bool NeedsLob(AnimalFacade passer, AnimalFacade receiver)
    {
        if (passer == null || receiver == null)
        {
            return false;
        }

        Vector3 from = ResolveBallKeepPosition(passer);
        Vector3 to = ResolveBallKeepPosition(receiver);
        if ((to - from).sqrMagnitude < 0.0001f)
        {
            return false;
        }

        if (IsTeammateOnPassLane(passer, receiver, from, to))
        {
            return true;
        }

        return IsEnemyOnPassLane(from, to, ResolveEnemyBlockingRange());
    }

    public static float ResolveEnemyBlockingRange()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        float fieldLength = teamBB != null ? teamBB.FieldInfo.FieldLength : 100f;
        float range = Mathf.Max(0.5f, fieldLength * 0.06f);
        int pressure = teamBB != null ? teamBB.BallInfo.IsBallOwnerUnderPressure : 0;
        if (pressure >= 2)
        {
            range *= 1.75f;
        }
        else if (pressure >= 1)
        {
            range *= 1.35f;
        }

        return range;
    }

    private static bool IsTeammateOnPassLane(
        AnimalFacade passer,
        AnimalFacade receiver,
        Vector3 from,
        Vector3 to)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return false;
        }

        Vector3 passDirection = (to - from).normalized;
        float passDistance = Vector3.Distance(from, to);

        foreach (AnimalFacade character in regist.Allys)
        {
            if (character == null || character == passer || character == receiver)
            {
                continue;
            }

            Vector3 characterPos = ResolveBallKeepPosition(character);
            Vector3 characterToPasser = characterPos - from;
            Vector3 projection = Vector3.Project(characterToPasser, passDirection);
            float distanceToPassLine = Vector3.Distance(characterToPasser, projection);

            if (distanceToPassLine < TeammateLaneRadius
                && projection.magnitude < passDistance
                && Vector3.Dot(projection, passDirection) > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEnemyOnPassLane(Vector3 from, Vector3 to, float blockingRange)
    {
        List<Vector3> enemyPositions = ResolveEnemyPositions();
        if (enemyPositions == null || enemyPositions.Count == 0)
        {
            return false;
        }

        return !PlayerBlackboardCalculator.IsPassRouteClear(to, from, enemyPositions, blockingRange);
    }

    private static List<Vector3> ResolveEnemyPositions()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null && teamBB.BasicInfo.EnemyPositions != null && teamBB.BasicInfo.EnemyPositions.Count > 0)
        {
            return teamBB.BasicInfo.EnemyPositions;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        var positions = new List<Vector3>();
        foreach (AnimalFacade enemy in regist.Enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            positions.Add(ResolveBallKeepPosition(enemy));
        }

        return positions;
    }

    private static Vector3 ResolveBallKeepPosition(AnimalFacade facade)
    {
        GameObject ballKeep = facade.GetBallKeep();
        return ballKeep != null ? ballKeep.transform.position : facade.transform.position;
    }
}
