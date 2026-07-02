using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GOAP 基準のパス先選定（パスレーン・前方性・向き・距離をスコアリング。ランダム選定は使わない）。
/// </summary>
public static class GoapPassTargetSelection
{
    private const float FacingConeDegrees = 30f;
    private const float BlockedRouteScore = -50f;
    private const float ClearRouteBaseScore = 10f;

    public struct CandidateScoreInput
    {
        public Vector3 PasserPosition;
        public float PasserFacingYDegrees;
        public Vector3 ReceiverPosition;
        public Vector3 AttackGoalPosition;
        public List<Vector3> EnemyPositions;
        public float FieldLength;
        public int OwnerPressureCount;
    }

    public static bool IsEligibleReceiver(AnimalFacade passer, AnimalFacade candidate)
    {
        return candidate != null
            && passer != null
            && candidate != passer
            && !candidate.IsGK();
    }

    public static float ScoreCandidate(in CandidateScoreInput input)
    {
        float fieldLength = Mathf.Max(input.FieldLength, 1f);
        float blockingRange = Mathf.Max(0.5f, fieldLength * 0.06f);
        var enemies = input.EnemyPositions ?? new List<Vector3>();

        bool routeClear = PlayerBlackboardCalculator.IsPassRouteClear(
            input.ReceiverPosition,
            input.PasserPosition,
            enemies,
            blockingRange);

        float score = routeClear ? ClearRouteBaseScore : BlockedRouteScore;

        float angleDiff = ComputeFacingAngleDiff(
            input.PasserPosition,
            input.ReceiverPosition,
            input.PasserFacingYDegrees);
        if (angleDiff <= FacingConeDegrees)
        {
            score += 3f;
        }
        else
        {
            score -= angleDiff * 0.05f;
        }

        Vector3 attackDir = input.AttackGoalPosition - input.PasserPosition;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.01f)
        {
            attackDir.Normalize();
            Vector3 toReceiver = input.ReceiverPosition - input.PasserPosition;
            toReceiver.y = 0f;
            if (toReceiver.sqrMagnitude > 0.01f)
            {
                float forward = Vector3.Dot(attackDir, toReceiver.normalized);
                score += forward * 4f;
            }
        }

        float distance = Vector3.Distance(input.PasserPosition, input.ReceiverPosition);
        float idealDistance = fieldLength * 0.28f;
        score -= Mathf.Abs(distance - idealDistance) / fieldLength * 2f;

        if (routeClear && input.OwnerPressureCount >= 1)
        {
            score += 1.5f;
            score += Mathf.Clamp01(1f - distance / (fieldLength * 0.5f));
        }

        return score;
    }

    public static bool TrySelectBest(
        AnimalFacade passer,
        IEnumerable<AnimalFacade> pool,
        Vector3 attackGoalPosition,
        out AnimalFacade best)
    {
        best = null;
        if (passer == null || pool == null)
        {
            return false;
        }

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        var enemies = teamBB != null ? teamBB.BasicInfo.EnemyPositions : new List<Vector3>();
        float fieldLength = teamBB != null ? teamBB.FieldInfo.FieldLength : 100f;
        int pressure = teamBB != null ? teamBB.BallInfo.IsBallOwnerUnderPressure : 0;

        Vector3 passerPos = ResolvePassOrigin(passer);
        float facingY = 360f - passer.transform.localEulerAngles.y;
        float bestScore = float.MinValue;

        foreach (AnimalFacade candidate in pool)
        {
            if (!IsEligibleReceiver(passer, candidate))
            {
                continue;
            }

            var input = new CandidateScoreInput
            {
                PasserPosition = passerPos,
                PasserFacingYDegrees = facingY,
                ReceiverPosition = ResolveReceivePosition(candidate),
                AttackGoalPosition = attackGoalPosition,
                EnemyPositions = enemies,
                FieldLength = fieldLength,
                OwnerPressureCount = pressure,
            };

            float score = ScoreCandidate(input);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best != null;
    }

    public static bool TrySelectBestAlly(AnimalFacade passer, out AnimalFacade best)
    {
        best = null;
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (regist == null || teamBB == null)
        {
            return false;
        }

        return TrySelectBest(
            passer,
            regist.Allys,
            teamBB.FieldInfo.EnemyGoalPosition,
            out best);
    }

    public static bool TrySelectBestEnemyTeammate(AnimalFacade passer, out AnimalFacade best)
    {
        best = null;
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (regist == null || teamBB == null)
        {
            return false;
        }

        return TrySelectBest(
            passer,
            regist.Enemies,
            teamBB.FieldInfo.OwnGoalPosition,
            out best);
    }

    public static float ComputeFacingAngleDiff(Vector3 origin, Vector3 target, float passerFacingYDegrees)
    {
        float theta = Mathf.Atan2(target.z - origin.z, target.x - origin.x) * Mathf.Rad2Deg - 90f;
        if (theta < 0f)
        {
            theta += 360f;
        }

        return Mathf.Abs(passerFacingYDegrees - theta);
    }

    private static Vector3 ResolvePassOrigin(AnimalFacade passer)
    {
        GameObject ballKeep = passer.GetBallKeep();
        return ballKeep != null ? ballKeep.transform.position : passer.transform.position;
    }

    private static Vector3 ResolveReceivePosition(AnimalFacade receiver)
    {
        GameObject ballKeep = receiver.GetBallKeep();
        return ballKeep != null ? ballKeep.transform.position : receiver.transform.position;
    }
}
