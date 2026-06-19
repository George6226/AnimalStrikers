using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 味方NPCの目標位置を攻守・編成スロットから算出する（段階1・ルールベース）。
/// </summary>
public static class TeammateNpcTacticalPositionCalculator
{
    public struct Result
    {
        public Vector3 TargetPosition;
        public TeammateNpcTacticalMode Mode;
        public bool IsValid;

        public string ModeLabel => Mode switch
        {
            TeammateNpcTacticalMode.Support => "SUPPORT",
            TeammateNpcTacticalMode.Defend => "DEFEND",
            TeammateNpcTacticalMode.ChaseBall => "CHASE",
            TeammateNpcTacticalMode.Hold => "HOLD",
            _ => "-",
        };
    }

    public static Result Calculate(
        Vector3 selfPosition,
        int formationSlotIndex,
        TeamBlackboard teamBB,
        IEnumerable<Vector3> otherTeammatePositions,
        AnimalFacade selfFacade = null)
    {
        if (teamBB == null)
        {
            return Invalid();
        }

        var ball = teamBB.BallInfo;
        var field = teamBB.FieldInfo;

        if (!ball.IsExistBall)
        {
            return Invalid();
        }

        if (ball.TeamHasBall)
        {
            return new Result
            {
                TargetPosition = CalculateAttackSupportPosition(
                    selfPosition, formationSlotIndex, teamBB, otherTeammatePositions),
                Mode = TeammateNpcTacticalMode.Support,
                IsValid = true,
            };
        }

        if (ball.EnemyHasBall)
        {
            return new Result
            {
                TargetPosition = CalculateDefensePosition(
                    selfPosition, formationSlotIndex, teamBB, otherTeammatePositions),
                Mode = TeammateNpcTacticalMode.Defend,
                IsValid = true,
            };
        }

        bool chaseFreeBall = TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc()
            && (selfFacade == null
                || TeammateNpcGoapRoleDifferentiation.IsFreeBallChaseLeader(selfFacade));
        if (chaseFreeBall)
        {
            return new Result
            {
                TargetPosition = ClampToField(ball.BallPosition, field),
                Mode = TeammateNpcTacticalMode.ChaseBall,
                IsValid = true,
            };
        }

        return new Result
        {
            TargetPosition = CalculateFreeBallSecondaryPosition(
                selfPosition, formationSlotIndex, teamBB, otherTeammatePositions),
            Mode = TeammateNpcTacticalMode.Support,
            IsValid = true,
        };
    }

    private static Vector3 CalculateFreeBallSecondaryPosition(
        Vector3 selfPos,
        int slotIndex,
        TeamBlackboard teamBB,
        IEnumerable<Vector3> otherTeammates)
    {
        var field = teamBB.FieldInfo;
        var ball = teamBB.BallInfo;
        Vector3 ballPos = ball.BallPosition;

        Vector3 toGoal = (field.EnemyGoalPosition - ballPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float lateralDist = field.FieldWidth * 0.24f;
        Vector3 lateral = GetSlotLateralOffset(slotIndex, right, lateralDist);
        float behindDist = field.FieldLength * 0.14f;

        Vector3 target = ballPos - toGoal * behindDist + lateral;
        target = ApplyTeammateSpacing(selfPos, target, otherTeammates, minSeparation: 5f);
        return ClampToField(target, field);
    }

    private static Vector3 CalculateAttackSupportPosition(
        Vector3 selfPos,
        int slotIndex,
        TeamBlackboard teamBB,
        IEnumerable<Vector3> otherTeammates)
    {
        var field = teamBB.FieldInfo;
        var ball = teamBB.BallInfo;
        Vector3 ownerPos = ball.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = ball.BallPosition;
        }

        Vector3 toGoal = (field.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float forwardDist = field.FieldLength * 0.18f;
        float lateralDist = field.FieldWidth * 0.22f;
        float passBlockRange = field.FieldLength * 0.06f;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;

        Vector3 slotLateral = GetSlotLateralOffset(slotIndex, right, lateralDist);
        Vector3 oppositeLateral = slotLateral.sqrMagnitude > 0.01f ? -slotLateral : right * lateralDist;

        var candidates = new List<Vector3>
        {
            ownerPos + toGoal * forwardDist + slotLateral,
            ownerPos + toGoal * forwardDist + oppositeLateral,
            ownerPos + toGoal * (forwardDist * 0.85f) + oppositeLateral * 1.15f,
            ownerPos + toGoal * (forwardDist * 1.05f) + oppositeLateral * 0.9f,
            ownerPos + toGoal * (forwardDist * 0.72f) + oppositeLateral,
            ownerPos + toGoal * forwardDist,
        };

        Vector3 target = candidates[0];
        float bestScore = float.MinValue;
        foreach (Vector3 raw in candidates)
        {
            Vector3 candidate = ClampToField(raw, field);
            float score = ScoreAttackSupportCandidate(
                candidate, ownerPos, selfPos, enemies, passBlockRange, field.FieldLength);
            if (score > bestScore)
            {
                bestScore = score;
                target = candidate;
            }
        }

        target = ApplyTeammateSpacing(selfPos, target, otherTeammates, minSeparation: 4f);
        target = AvoidHumanPlayer(target, minDistance: 5f);
        return ClampToField(target, field);
    }

    private static float ScoreAttackSupportCandidate(
        Vector3 candidate,
        Vector3 ownerPos,
        Vector3 selfPos,
        List<Vector3> enemies,
        float passBlockRange,
        float fieldLength)
    {
        float score = 0f;

        if (PlayerBlackboardCalculator.IsPassRouteClear(candidate, ownerPos, enemies, passBlockRange))
        {
            score += 6f;
        }
        else
        {
            score -= 8f;
        }

        float minEnemyDist = float.MaxValue;
        if (enemies != null)
        {
            foreach (Vector3 enemyPos in enemies)
            {
                minEnemyDist = Mathf.Min(minEnemyDist, Vector3.Distance(candidate, enemyPos));
            }
        }

        score += Mathf.Clamp01(minEnemyDist / (fieldLength * 0.15f)) * 2f;

        float moveDist = Vector3.Distance(candidate, selfPos);
        score += (1f - Mathf.Clamp01(moveDist / (fieldLength * 0.4f))) * 0.4f;

        return score;
    }

    private static Vector3 CalculateDefensePosition(
        Vector3 selfPos,
        int slotIndex,
        TeamBlackboard teamBB,
        IEnumerable<Vector3> otherTeammates)
    {
        var field = teamBB.FieldInfo;
        var ball = teamBB.BallInfo;
        Vector3 ownerPos = ball.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = ball.BallPosition;
        }

        Vector3 ownGoal = field.OwnGoalPosition;
        Vector3 toOwnGoal = (ownGoal - ownerPos).normalized;
        if (toOwnGoal.sqrMagnitude < 0.0001f)
        {
            toOwnGoal = (ownGoal - field.FieldCenter).normalized;
        }

        Vector3 toGoalAttack = (field.EnemyGoalPosition - ownerPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toGoalAttack).normalized;

        float markDepth = field.FieldLength * 0.12f;
        float lateralDist = field.FieldWidth * 0.2f;
        Vector3 lateral = GetSlotLateralOffset(slotIndex, right, lateralDist);

        Vector3 target = ownerPos + toOwnGoal * markDepth + lateral;

        if (Vector3.Distance(selfPos, ownerPos) > field.FieldLength * 0.35f)
        {
            float retreatDepth = field.FieldLength * 0.28f;
            float sign = Mathf.Sign(field.FieldCenter.z - ownGoal.z);
            Vector3 lineZ = new Vector3(field.FieldCenter.x, 0f, ownGoal.z + retreatDepth * sign);
            target = Vector3.Lerp(target, lineZ + lateral * 0.5f, 0.45f);
        }

        target = ApplyTeammateSpacing(selfPos, target, otherTeammates, minSeparation: 4f);
        return ClampToField(target, field);
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

    private static Vector3 AvoidHumanPlayer(Vector3 target, float minDistance)
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        if (squad == null)
        {
            return target;
        }

        foreach (var human in squad.GetHumanControllableFieldPlayers())
        {
            if (human == null)
            {
                continue;
            }

            Vector3 hp = human.transform.position;
            Vector3 diff = target - hp;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < minDistance && dist > 0.01f)
            {
                target = hp + diff.normalized * minDistance;
            }
        }

        return target;
    }

    private static Vector3 ClampToField(Vector3 pos, TeamFieldInfo field)
    {
        float halfW = field.FieldWidth * 0.5f;
        float halfL = field.FieldLength * 0.5f;
        Vector3 c = field.FieldCenter;
        return new Vector3(
            Mathf.Clamp(pos.x, c.x - halfW, c.x + halfW),
            pos.y,
            Mathf.Clamp(pos.z, c.z - halfL, c.z + halfL));
    }

    private static Result Invalid()
    {
        return new Result { IsValid = false, Mode = TeammateNpcTacticalMode.Hold };
    }
}
