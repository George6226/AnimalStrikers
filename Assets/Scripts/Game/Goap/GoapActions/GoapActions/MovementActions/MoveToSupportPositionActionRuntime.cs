using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

public class MoveToSupportPositionActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "Support";
    private const float MinRunTimeBeforeComplete = 0.35f;

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

    public MoveToSupportPositionActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as MoveToSupportPositionActionSO;
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

        if (TeammateNpcSupportPlanning.BlocksMoveToSupportForWingLayout(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=false reason=wing_layout_requires_support_angle", bb);
            return false;
        }

        if (TeammateNpcSupportPlanning.BlocksWhenAlreadyInPassReceivePosition(bb)
            && bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "CanExecute=false reason=already_in_receive_position", bb);
            return false;
        }

        if (!IsTeamBallSituation(out string teamReason))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"CanExecute=false reason=team_ball ({teamReason})", bb);
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
        _targetPosition = CalculateSupportPosition(bb, "Execute");
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
        if (!IsTeamBallSituation(out _)) return;

        if (Time.time >= _nextRetargetTime)
        {
            Vector3 before = _targetPosition;
            _targetPosition = CalculateSupportPosition(_bb, "Retarget");
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

        if (!IsTeamBallSituation(out string teamReason))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"Complete reason=team_ball_lost ({teamReason})", _bb);
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

        // 目標地点の近く（arriveDistance以内）に達してから戦術判定を行う
        bool nearTarget = dist <= _arriveDistance;
        bool tacticalInPosition = nearTarget && IsTacticalPassReceivePosition(_bb, teamBB);

        if (tacticalInPosition)
        {
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"Complete reason=tactical_in_position elapsed={elapsed:F2}s dist={dist:F2} target={GoapMovementDiagnostic.FormatVector(_targetPosition)}",
                _bb);
            ApplyPassReceiveFact(_bb);
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

    private static bool IsTacticalPassReceivePosition(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        return TeammateNpcSupportPlanning.EvaluatePassReceivePosition(bb, teamBB);
    }

    private static void ApplyPassReceiveFact(PlayerBlackboard bb)
    {
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "false"), false);
    }

    private bool IsTeamBallSituation(out string reason)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            reason = "teamBB=null";
            return false;
        }

        if (!teamBB.BallInfo.TeamHasBall)
        {
            reason = $"teamHasBall=false ballState={teamBB.BallInfo.BallState}";
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
            $"{phase} ballContext teamHasBall={ball.TeamHasBall} ownerId={ball.BallOwnerID} " +
            $"ownerPos={GoapMovementDiagnostic.FormatVector(ball.BallOwnerPosition)} ballPos={GoapMovementDiagnostic.FormatVector(ball.BallPosition)}",
            bb);
    }

    private Vector3 CalculateSupportPosition(PlayerBlackboard bb, string phase)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return GoapNpcMotor.GetSelfWorldPosition(bb);
        }

        Vector3 selfPos = GoapNpcMotor.GetSelfWorldPosition(bb);
        int slotIndex = ResolveFormationSlotIndex(bb);

        if (TeammateNpcSupportPlanning.ShouldUseWidthLayoutSupportPosition(bb))
        {
            var settings = CreateSupportAnglePositioning.CreateDefaultSettings();
            Vector3 widthTarget = CreateSupportAnglePositioning.SelectBestPosition(
                selfPos,
                slotIndex,
                teamBB,
                settings,
                out var snap);
            float widthDist = Vector3.Distance(selfPos, widthTarget);
            GoapMovementDiagnostic.Log(
                DiagCategory,
                $"{phase} CalcSupport mode=WidthLayout " +
                CreateSupportAnglePositioning.FormatDiagnosticLine(phase, snap) +
                $" distSelf={widthDist:F2}",
                bb);
            return widthTarget;
        }

        var tactical = TeammateNpcTacticalPositionCalculator.Calculate(
            selfPos,
            slotIndex,
            teamBB,
            CollectOtherTeammatePositions(selfPos));

        Vector3 target = tactical.IsValid && tactical.Mode == TeammateNpcTacticalMode.Support
            ? tactical.TargetPosition
            : FallbackSupportPosition(selfPos, teamBB);

        float distFromSelf = Vector3.Distance(selfPos, target);
        GoapMovementDiagnostic.Log(
            DiagCategory,
            $"{phase} CalcSupport mode={tactical.Mode} valid={tactical.IsValid} self={GoapMovementDiagnostic.FormatVector(selfPos)} " +
            $"target={GoapMovementDiagnostic.FormatVector(target)} distSelf={distFromSelf:F2}",
            bb);

        return target;
    }

    private static Vector3 FallbackSupportPosition(Vector3 selfPos, TeamBlackboard teamBB)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = teamBB.BallInfo.BallPosition;
        }

        Vector3 toGoal = (teamBB.FieldInfo.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = (teamBB.FieldInfo.EnemyGoalPosition - selfPos).normalized;
        }

        return ownerPos + toGoal * (teamBB.FieldInfo.FieldLength * 0.18f);
    }

    private static int ResolveFormationSlotIndex(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null) return 0;
        var slot = bb.BasicData.Self.GetComponentInParent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned ? slot.Index : 0;
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
