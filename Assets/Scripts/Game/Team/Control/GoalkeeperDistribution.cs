using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F3: GK がボールを保持した後の配球先選定と味方の立ち位置。
/// </summary>
public static class GoalkeeperDistribution
{
    public enum TeammateRole
    {
        ReceivePass,
        Advance,
    }

    public static bool TryResolveHoldingGoalkeeper(TeamBlackboard teamBB, out AnimalFacade goalkeeper)
    {
        goalkeeper = null;
        if (teamBB == null || !teamBB.BallInfo.TeamHasBall)
        {
            return false;
        }

        int ownerId = teamBB.BallInfo.BallOwnerID;
        if (ownerId < 0)
        {
            return false;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return false;
        }

        foreach (AnimalFacade facade in regist.AllAnimals)
        {
            if (facade == null || !facade.IsGK())
            {
                continue;
            }

            var avatar = facade.GetAvatar();
            if (avatar != null && avatar.ViewID == ownerId)
            {
                goalkeeper = facade;
                return true;
            }
        }

        return false;
    }

    public static bool IsGoalkeeperHoldingBall(TeamBlackboard teamBB) =>
        TryResolveHoldingGoalkeeper(teamBB, out _);

    public static bool TrySelectPassTarget(AnimalFacade goalkeeper, bool mirrored, out AnimalFacade target)
    {
        target = null;
        if (goalkeeper == null)
        {
            return false;
        }

        if (mirrored)
        {
            return GoapPassTargetSelection.TrySelectBestEnemyTeammate(goalkeeper, out target);
        }

        return GoapPassTargetSelection.TrySelectBestAlly(goalkeeper, out target);
    }

    public static TeammateRole ResolveTeammateRole(
        AnimalFacade teammate,
        AnimalFacade goalkeeper,
        bool mirrored)
    {
        if (teammate == null
            || goalkeeper == null
            || !TrySelectPassTarget(goalkeeper, mirrored, out AnimalFacade passTarget))
        {
            return TeammateRole.Advance;
        }

        return teammate == passTarget ? TeammateRole.ReceivePass : TeammateRole.Advance;
    }

    public static TeammateNpcTacticalPositionCalculator.Result ComputeTeammateResult(
        Vector3 selfPosition,
        int formationSlotIndex,
        TeamBlackboard teamBB,
        AnimalFacade goalkeeper,
        IEnumerable<Vector3> otherTeammatePositions,
        AnimalFacade selfFacade)
    {
        if (teamBB == null || goalkeeper == null)
        {
            return Invalid();
        }

        bool mirrored = GoalkeeperPositioning.IsMirroredGoalkeeper(goalkeeper);
        TeammateRole role = selfFacade != null
            ? ResolveTeammateRole(selfFacade, goalkeeper, mirrored)
            : TeammateRole.Advance;

        Vector3 gkPosition = ResolveGoalkeeperPosition(teamBB, goalkeeper);
        Vector3 target = role == TeammateRole.ReceivePass
            ? ComputeReceivePosition(selfPosition, formationSlotIndex, teamBB, gkPosition, otherTeammatePositions)
            : ComputeAdvancePosition(selfPosition, formationSlotIndex, teamBB, otherTeammatePositions);

        return new TeammateNpcTacticalPositionCalculator.Result
        {
            TargetPosition = target,
            Mode = TeammateNpcTacticalMode.Support,
            IsValid = true,
        };
    }

    public static Vector3 ComputeReceivePosition(
        Vector3 selfPosition,
        int slotIndex,
        TeamBlackboard teamBB,
        Vector3 goalkeeperPosition,
        IEnumerable<Vector3> otherTeammates)
    {
        var field = teamBB.FieldInfo;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        Vector3 attackGoal = field.EnemyGoalPosition;
        Vector3 toGoal = attackGoal - goalkeeperPosition;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }
        else
        {
            toGoal.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float forwardDist = field.FieldLength * 0.20f;
        float lateralDist = field.FieldWidth * 0.20f;
        Vector3 slotLateral = GetSlotLateralOffset(slotIndex, right, lateralDist);
        float passBlockRange = field.FieldLength * 0.06f;

        var candidates = new List<Vector3>
        {
            goalkeeperPosition + toGoal * forwardDist + slotLateral,
            goalkeeperPosition + toGoal * (forwardDist * 1.08f) + slotLateral * 0.9f,
            goalkeeperPosition + toGoal * (forwardDist * 0.88f) - slotLateral,
            goalkeeperPosition + toGoal * (forwardDist * 1.15f),
        };

        Vector3 target = candidates[0];
        float bestScore = float.MinValue;
        foreach (Vector3 raw in candidates)
        {
            Vector3 candidate = GoalkeeperPositioning.ClampToField(raw, field);
            float score = 0f;
            if (PlayerBlackboardCalculator.IsPassRouteClear(
                    candidate, goalkeeperPosition, enemies, passBlockRange))
            {
                score += 8f;
            }
            else
            {
                score -= 12f;
            }

            score += Vector3.Dot(candidate - goalkeeperPosition, toGoal) * 0.05f;
            float moveDist = Vector3.Distance(candidate, selfPosition);
            score += (1f - Mathf.Clamp01(moveDist / (field.FieldLength * 0.45f))) * 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                target = candidate;
            }
        }

        target = ApplyTeammateSpacing(selfPosition, target, otherTeammates, minSeparation: 4f);
        return GoalkeeperPositioning.ClampToField(target, field);
    }

    public static Vector3 ComputeAdvancePosition(
        Vector3 selfPosition,
        int slotIndex,
        TeamBlackboard teamBB,
        IEnumerable<Vector3> otherTeammates)
    {
        var field = teamBB.FieldInfo;
        Vector3 attackGoal = field.EnemyGoalPosition;
        Vector3 toGoal = attackGoal - selfPosition;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }
        else
        {
            toGoal.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float advanceDist = field.FieldLength * 0.26f;
        float lateralDist = field.FieldWidth * 0.18f;
        Vector3 lateral = GetSlotLateralOffset(slotIndex, right, lateralDist);

        Vector3 target = selfPosition + toGoal * advanceDist + lateral;
        target = ApplyTeammateSpacing(selfPosition, target, otherTeammates, minSeparation: 4f);
        return GoalkeeperPositioning.ClampToField(target, field);
    }

    private static Vector3 ResolveGoalkeeperPosition(TeamBlackboard teamBB, AnimalFacade goalkeeper)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude > 0.01f)
        {
            return ownerPos;
        }

        GameObject ballKeep = goalkeeper.GetBallKeep();
        return ballKeep != null ? ballKeep.transform.position : goalkeeper.transform.position;
    }

    private static Vector3 GetSlotLateralOffset(int slotIndex, Vector3 right, float lateralDist)
    {
        return slotIndex switch
        {
            1 => right * lateralDist,
            2 => -right * lateralDist,
            0 => right * (lateralDist * 0.35f),
            _ => Vector3.zero,
        };
    }

    private static Vector3 ApplyTeammateSpacing(
        Vector3 selfPos,
        Vector3 target,
        IEnumerable<Vector3> otherTeammates,
        float minSeparation)
    {
        if (otherTeammates == null)
        {
            return target;
        }

        Vector3 push = Vector3.zero;
        foreach (Vector3 mate in otherTeammates)
        {
            Vector3 diff = target - mate;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < minSeparation && dist > 0.01f)
            {
                push += diff.normalized * (minSeparation - dist);
            }
        }

        return target + push * 0.6f;
    }

    private static TeammateNpcTacticalPositionCalculator.Result Invalid()
    {
        return new TeammateNpcTacticalPositionCalculator.Result
        {
            IsValid = false,
            Mode = TeammateNpcTacticalMode.Hold,
        };
    }
}
