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
    private const float MovingReceiverPenalty = 8f;
    private const float LateralPassPressurePenalty = 6f;

    public struct CandidateScoreInput
    {
        public Vector3 PasserPosition;
        public float PasserFacingYDegrees;
        public Vector3 ReceiverPosition;
        public Vector3 AttackGoalPosition;
        public List<Vector3> EnemyPositions;
        public float FieldLength;
        public int OwnerPressureCount;
        public bool ReceiverIsMoving;
    }

    public static bool IsEligibleReceiver(AnimalFacade passer, AnimalFacade candidate)
    {
        return candidate != null
            && passer != null
            && candidate != passer
            && !IsFieldGoalkeeper(candidate);
    }

    private static bool IsFieldGoalkeeper(AnimalFacade facade)
    {
        if (facade == null)
        {
            return true;
        }

        AnimalInfo info = facade.GetAnimalInfo();
        return info != null && info.IsGK;
    }

    public static bool IsAllySideFieldRole(AnimalControlRole role)
    {
        return role == AnimalControlRole.Human || role == AnimalControlRole.TeammateNpc;
    }

    public static bool IsEnemySideFieldRole(AnimalControlRole role)
    {
        return role == AnimalControlRole.EnemyFieldNpc;
    }

    /// <summary>
    /// TeamRegistar のタグではなく AnimalControlAssignment で同一陣営か判定する。
    /// （オフライン GOAP 対戦で敵 Master が PlayerAgent タグのまま Allys に入る誤登録を防ぐ）
    /// </summary>
    public static bool IsSameTeamFieldReceiver(AnimalFacade passer, AnimalFacade candidate)
    {
        if (!IsEligibleReceiver(passer, candidate))
        {
            return false;
        }

        return IsAllySidePasser(passer) == IsAllySidePasser(candidate);
    }

    /// <summary>
    /// フィールドパスの有効先。Human は味方 NPC（TeammateNpc）へだけ渡し、
    /// 敵 Master が Human ロールのまま誤登録されていてもパス先に選ばない。
    /// </summary>
    public static bool IsFieldPassReceiver(AnimalFacade passer, AnimalFacade candidate)
    {
        if (!IsSameTeamFieldReceiver(passer, candidate))
        {
            return false;
        }

        var assignment = candidate.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            return false;
        }

        if (IsAllySidePasser(passer))
        {
            return assignment.Role == AnimalControlRole.TeammateNpc;
        }

        return assignment.Role == AnimalControlRole.EnemyFieldNpc;
    }

    public static IEnumerable<AnimalFacade> EnumerateSameTeamFieldReceivers(AnimalFacade passer)
    {
        if (passer == null)
        {
            yield break;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            yield break;
        }

        bool passerOnAllySide = IsAllySidePasser(passer);
        foreach (AnimalFacade facade in regist.AllAnimals)
        {
            if (facade == null || IsFieldGoalkeeper(facade))
            {
                continue;
            }

            if (IsAllySidePasser(facade) != passerOnAllySide)
            {
                continue;
            }

            if (!IsEligibleReceiver(passer, facade))
            {
                continue;
            }

            yield return facade;
        }
    }

    public static bool IsAllySidePasser(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment != null)
        {
            if (assignment.Role == AnimalControlRole.TeammateNpc)
            {
                return true;
            }

            if (assignment.Role == AnimalControlRole.EnemyFieldNpc)
            {
                return false;
            }

            if (assignment.Role == AnimalControlRole.Human)
            {
                var registForHuman = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
                if (registForHuman != null)
                {
                    if (registForHuman.Enemies.Contains(facade))
                    {
                        return false;
                    }

                    if (registForHuman.Allys.Contains(facade))
                    {
                        return true;
                    }
                }

                return true;
            }
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist != null)
        {
            if (regist.Enemies.Contains(facade))
            {
                return false;
            }

            if (regist.Allys.Contains(facade))
            {
                return true;
            }
        }

        string tag = facade.GetAvatar() != null ? facade.GetAvatar().tag : string.Empty;
        return tag == ConstData.PLAYER_TAG || tag == ConstData.NPC_TAG;
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
        float distanceRatio = distance / fieldLength;
        float idealDistance = fieldLength * 0.28f;
        score -= Mathf.Abs(distance - idealDistance) / fieldLength * 2f;

        if (input.ReceiverIsMoving)
        {
            score -= MovingReceiverPenalty;
        }

        if (input.OwnerPressureCount >= 1)
        {
            if (routeClear)
            {
                score += 1.5f;
                score += Mathf.Clamp01(1f - distanceRatio * 2f);
            }

            score -= distanceRatio * 12f;

            if (attackDir.sqrMagnitude > 0.01f)
            {
                Vector3 toReceiver = input.ReceiverPosition - input.PasserPosition;
                toReceiver.y = 0f;
                if (toReceiver.sqrMagnitude > 0.01f)
                {
                    float forwardAbs = Mathf.Abs(Vector3.Dot(attackDir.normalized, toReceiver.normalized));
                    if (forwardAbs < 0.35f)
                    {
                        score -= LateralPassPressurePenalty * input.OwnerPressureCount;
                    }
                }
            }
        }

        if (input.OwnerPressureCount >= 2)
        {
            if (!routeClear)
            {
                score -= 40f;
            }
            else
            {
                score += (1f - Mathf.Clamp01(distanceRatio * 2.5f)) * 8f;
            }
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
            if (!IsFieldPassReceiver(passer, candidate))
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
                ReceiverIsMoving = ResolveReceiverIsMoving(candidate),
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
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || passer == null)
        {
            return false;
        }

        return TrySelectBest(
            passer,
            EnumerateSameTeamFieldReceivers(passer),
            teamBB.FieldInfo.EnemyGoalPosition,
            out best);
    }

    public static bool TrySelectBestEnemyTeammate(AnimalFacade passer, out AnimalFacade best)
    {
        best = null;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || passer == null)
        {
            return false;
        }

        return TrySelectBest(
            passer,
            EnumerateSameTeamFieldReceivers(passer),
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

    private static bool ResolveReceiverIsMoving(AnimalFacade receiver)
    {
        if (receiver == null)
        {
            return false;
        }

        PlayerBlackboard bb = receiver.GetComponentInChildren<PlayerBlackboard>();
        return bb != null && bb.PhysicalState.IsMoving;
    }
}
