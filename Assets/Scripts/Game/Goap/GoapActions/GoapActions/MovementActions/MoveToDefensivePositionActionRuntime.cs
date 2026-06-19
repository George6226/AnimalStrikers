using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

public class MoveToDefensivePositionActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "Defend";
    private const float MinRunTimeBeforeComplete = 0.35f;
    private const float ReTriggerCooldownSeconds = 0.8f;
    private static readonly Dictionary<int, float> CooldownUntilByPlayerId = new Dictionary<int, float>();

    private bool _isExecuting;
    private float _startTime;
    private float _maxMoveDuration = 8f;
    private float _arriveDistance = 1.5f;
    private float _moveIntensity = 1f;
    private float _retargetInterval = 0.35f;
    private float _nextRetargetTime;

    private PlayerBlackboard _bb;
    private bool _motorResolved;
    private Vector3 _targetPosition;

    public MoveToDefensivePositionActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as MoveToDefensivePositionActionSO;
        if (so == null) return;

        _maxMoveDuration = so.MaxMoveDuration;
        _arriveDistance = so.ArriveDistance;
        _moveIntensity = so.MoveIntensity;
        _retargetInterval = so.RetargetInterval;
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=false reason=null_bb");
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=false reason=can_move_false", bb);
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=false reason=self_has_ball", bb);
            return false;
        }

        int playerId = ResolvePlayerId(bb);
        if (CooldownUntilByPlayerId.TryGetValue(playerId, out float cooldownUntil) && Time.time < cooldownUntil)
        {
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"CanExecute=false reason=defense_cooldown remain={(cooldownUntil - Time.time):F2}s",
                bb);
            return false;
        }

        if (!IsEnemyBallSituation(out string teamReason))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"CanExecute=false reason=enemy_ball ({teamReason})", bb);
            return false;
        }

        if (!GoapNpcMotor.TryResolve(bb, out _, out _, out _))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"CanExecute=false {GoapMovementDiagnostic.FormatMotorResolve(bb)}", bb);
            return false;
        }

        GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=true", bb);
        return true;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapNpcMotor.TryResolve(bb, out _, out _, out _);
        _targetPosition = CalculateDefensivePosition(bb, "Execute");
        _isExecuting = true;
        _startTime = Time.time;
        _nextRetargetTime = Time.time;

        float dist = Vector3.Distance(GoapNpcMotor.GetSelfWorldPosition(bb), _targetPosition);
        LogBallContext(bb, "Execute");

        if (!_motorResolved)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute aborted: motor not resolved", bb);
        }
        else
        {
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"Execute start target={GoapMovementDiagnostic.FormatVector(_targetPosition)} dist={dist:F2} arriveDist={_arriveDistance:F2}",
                bb);
        }
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null || !_motorResolved) return;
        if (!IsEnemyBallSituation(out _)) return;

        if (Time.time >= _nextRetargetTime)
        {
            Vector3 before = _targetPosition;
            _targetPosition = CalculateDefensivePosition(_bb, "Retarget");
            _nextRetargetTime = Time.time + _retargetInterval;
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"Retarget target={GoapMovementDiagnostic.FormatVector(_targetPosition)} delta={Vector3.Distance(before, _targetPosition):F2}",
                _bb);
        }

        GoapNpcMotor.MoveToward(_bb, _targetPosition, _moveIntensity, DiagCategory);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting || _bb == null) return true;

        if (!_motorResolved)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Complete reason=motor_not_resolved (instant)", _bb);
            _isExecuting = false;
            return true;
        }

        if (!IsEnemyBallSituation(out string teamReason))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"Complete reason=enemy_ball_lost ({teamReason})", _bb);
            GoapNpcMotor.Stop(_bb, DiagCategory);
            _isExecuting = false;
            return true;
        }

        float elapsed = Time.time - _startTime;
        if (elapsed < MinRunTimeBeforeComplete)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            GoapNpcMotor.Stop(_bb, DiagCategory);
            _isExecuting = false;
            return true;
        }

        Vector3 selfPos = GoapNpcMotor.GetSelfWorldPosition(_bb);
        float dist = Vector3.Distance(selfPos, _targetPosition);

        bool nearTarget = dist <= _arriveDistance;
        bool tacticalInPosition = nearTarget && IsTacticalDefensivePosition(_bb, teamBB);
        if (tacticalInPosition)
        {
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"Complete reason=tactical_in_position elapsed={elapsed:F2}s dist={dist:F2} target={GoapMovementDiagnostic.FormatVector(_targetPosition)}",
                _bb);
            ApplyDefensiveFact(_bb);
            SetReTriggerCooldown(_bb);
            GoapNpcMotor.Stop(_bb, DiagCategory);
            _isExecuting = false;
            return true;
        }

        if (elapsed >= _maxMoveDuration)
        {
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"Complete reason=timeout elapsed={elapsed:F2}s dist={dist:F2} tactical=false",
                _bb);
            SetReTriggerCooldown(_bb);
            GoapNpcMotor.Stop(_bb, DiagCategory);
            _isExecuting = false;
            return true;
        }

        GoapMovementDiagnostic.LogThrottled(
            DiagCategory,
            $"Running elapsed={elapsed:F2}s dist={dist:F2} geoArrive={dist <= _arriveDistance} tactical=false",
            _bb,
            0.5f);

        return false;
    }

    public override void Cancel()
    {
        GoapMovementDiagnostic.Log(DiagCategory, "Cancel", _bb);
        _isExecuting = false;
        if (_bb != null)
        {
            GoapNpcMotor.Stop(_bb, DiagCategory);
        }
    }

    private static bool IsTacticalDefensivePosition(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        return PlayerBlackboardCalculator.CalculateIsInDefensivePosition(
            teamBB.BallInfo.TeamHasBall,
            bb.BallState.HasBall,
            bb.ActionState.IsStunned,
            teamBB.FieldInfo.FieldLength,
            GoapNpcMotor.GetSelfWorldPosition(bb),
            teamBB.BallInfo.BallOwnerPosition,
            teamBB.BasicInfo.EnemyPositions,
            teamBB.FieldInfo.EnemyGoalPosition);
    }

    private static void ApplyDefensiveFact(PlayerBlackboard bb)
    {
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "false"), false);
    }

    private bool IsEnemyBallSituation(out string reason)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            reason = "teamBB=null";
            return false;
        }

        if (!teamBB.BallInfo.EnemyHasBall)
        {
            reason = $"enemyHasBall=false ballState={teamBB.BallInfo.BallState}";
            return false;
        }

        if (teamBB.BallInfo.TeamHasBall)
        {
            reason = $"teamHasBall=true ballState={teamBB.BallInfo.BallState}";
            return false;
        }

        reason = "ok";
        return true;
    }

    private void LogBallContext(PlayerBlackboard bb, string phase)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"{phase} ballContext=teamBB_null", bb);
            return;
        }

        var ball = teamBB.BallInfo;
        GoapMovementDiagnostic.Log(
            DiagCategory,
            $"{phase} ballContext enemyHasBall={ball.EnemyHasBall} ownerId={ball.BallOwnerID} " +
            $"ownerPos={GoapMovementDiagnostic.FormatVector(ball.BallOwnerPosition)} ballPos={GoapMovementDiagnostic.FormatVector(ball.BallPosition)}",
            bb);
    }

    private Vector3 CalculateDefensivePosition(PlayerBlackboard bb, string phase)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return GoapNpcMotor.GetSelfWorldPosition(bb);
        }

        Vector3 selfPos = GoapNpcMotor.GetSelfWorldPosition(bb);
        int slotIndex = ResolveFormationSlotIndex(bb);
        var tactical = TeammateNpcTacticalPositionCalculator.Calculate(
            selfPos,
            slotIndex,
            teamBB,
            CollectOtherTeammatePositions(selfPos));

        Vector3 target = tactical.IsValid && tactical.Mode == TeammateNpcTacticalMode.Defend
            ? tactical.TargetPosition
            : FallbackDefensivePosition(selfPos, teamBB);

        float distFromSelf = Vector3.Distance(selfPos, target);
        GoapMovementDiagnostic.Log(
            DiagCategory,
            $"{phase} CalcDefense mode={tactical.Mode} valid={tactical.IsValid} self={GoapMovementDiagnostic.FormatVector(selfPos)} " +
            $"target={GoapMovementDiagnostic.FormatVector(target)} distSelf={distFromSelf:F2}",
            bb);

        return target;
    }

    private static Vector3 FallbackDefensivePosition(Vector3 selfPos, TeamBlackboard teamBB)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = teamBB.BallInfo.BallPosition;
        }

        Vector3 toOwnGoal = (teamBB.FieldInfo.OwnGoalPosition - ownerPos).normalized;
        if (toOwnGoal.sqrMagnitude < 0.0001f)
        {
            toOwnGoal = (teamBB.FieldInfo.OwnGoalPosition - selfPos).normalized;
        }

        return ownerPos + toOwnGoal * (teamBB.FieldInfo.FieldLength * 0.12f);
    }

    private static int ResolveFormationSlotIndex(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null) return 0;
        var slot = bb.BasicData.Self.GetComponentInParent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned ? slot.Index : 0;
    }

    private static int ResolvePlayerId(PlayerBlackboard bb)
    {
        if (bb?.BasicData != null && bb.BasicData.PlayerID > 0)
        {
            return bb.BasicData.PlayerID;
        }

        return bb != null ? bb.GetInstanceID() : 0;
    }

    private static void SetReTriggerCooldown(PlayerBlackboard bb)
    {
        int playerId = ResolvePlayerId(bb);
        CooldownUntilByPlayerId[playerId] = Time.time + ReTriggerCooldownSeconds;
    }

    public static bool IsInReTriggerCooldown(PlayerBlackboard bb, out float remainingSeconds)
    {
        remainingSeconds = 0f;
        int playerId = ResolvePlayerId(bb);
        if (!CooldownUntilByPlayerId.TryGetValue(playerId, out float cooldownUntil))
        {
            return false;
        }

        remainingSeconds = Mathf.Max(0f, cooldownUntil - Time.time);
        return remainingSeconds > 0f;
    }

    private static List<Vector3> CollectOtherTeammatePositions(Vector3 selfPos)
    {
        var list = new List<Vector3>();
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null) return list;

        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK()) continue;
            Vector3 p = ally.transform.position;
            if ((p - selfPos).sqrMagnitude < 0.25f) continue;
            list.Add(p);
        }

        return list;
    }
}
