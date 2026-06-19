using UnityEngine;
using Game.Goap;

public class MoveToFreeBallActionRuntime : GoapActionRuntime
{
    private bool _isExecuting;
    private float _startTime;
    private float _maxChaseDuration = 8f;
    private float _nearBallDistance = 1.2f;
    private float _moveIntensity = 1f;

    private PlayerBlackboard _bb;
    private bool _motorResolved;

    public MoveToFreeBallActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as MoveToFreeBallActionSO;
        if (so == null) return;

        _maxChaseDuration = so.MaxChaseDuration;
        _nearBallDistance = so.NearBallDistance;
        _moveIntensity = so.MoveIntensity;
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (bb == null) return false;
        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true) return false;
        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true) return false;
        if (!IsFreeBallSituation()) return false;
        if (TeammateNpcGoapRoleDifferentiation.Enabled
            && !TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc())
        {
            return false;
        }

        if (TeammateNpcGoapRoleDifferentiation.Enabled
            && !TeammateNpcGoapRoleDifferentiation.ShouldLeadFreeBallChase(bb))
        {
            return false;
        }

        if (TeammateNpcGoapRoleDifferentiation.GetDistanceToBall(bb)
            <= TeammateNpcGoapRoleDifferentiation.FreeBallPursueMinDistance)
        {
            return false;
        }

        return GoapNpcMotor.TryResolve(bb, out _, out _, out _);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapNpcMotor.TryResolve(bb, out _, out _, out _);
        _isExecuting = true;
        _startTime = Time.time;

        if (!_motorResolved)
        {
            DebugLogger.Log($"[{_debugName}] MoveToFreeBall: 移動コンポーネント未解決");
        }
        else
        {
            bool isLeader = !TeammateNpcGoapRoleDifferentiation.Enabled
                || TeammateNpcGoapRoleDifferentiation.ShouldLeadFreeBallChase(bb);
            float dist = TeammateNpcGoapRoleDifferentiation.GetDistanceToBall(bb);
            DebugLogger.Log($"[{_debugName}] MoveToFreeBall 開始 leader={isLeader} dist={dist:F2}");
            TeammateNpcGoapRoleDifferentiation.RegisterFreeBallChaseLeader(bb);
            GoapMovementDiagnostic.Log(
                "FreeBall",
                $"Execute leader={isLeader} dist={dist:F2} lockPlayerId={TeammateNpcGoapRoleDifferentiation.DebugLockedChaseLeaderPlayerId}",
                bb);
        }
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null || !_motorResolved) return;

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;

        GoapNpcMotor.MoveToward(_bb, teamBB.BallInfo.BallPosition, _moveIntensity, "FreeBall");
    }

    public override bool IsComplete()
    {
        if (!_isExecuting || _bb == null)
        {
            return true;
        }

        if (TryCompleteNearBall())
        {
            return true;
        }

        if (Time.time - _startTime >= _maxChaseDuration)
        {
            FinishFreeBallChase(false);
            return true;
        }

        return false;
    }

    public override void Cancel()
    {
        FinishFreeBallChase(false);
    }

    private void FinishFreeBallChase(bool setNearBallFact)
    {
        if (_bb != null && _motorResolved)
        {
            GoapNpcMotor.Stop(_bb, "FreeBall");
        }

        if (setNearBallFact && _bb != null)
        {
            _bb.SetFact(new Fact(SymbolTag.Position.NEAR_BALL, "true"), true);
            _bb.SetFact(new Fact(SymbolTag.Position.NEAR_BALL, "false"), false);
        }

        if (_bb != null)
        {
            TeammateNpcGoapRoleDifferentiation.ReleaseFreeBallChaseLeader(_bb);
        }

        _isExecuting = false;
    }

    private bool TryCompleteNearBall()
    {
        if (!_isExecuting || _bb == null || !_motorResolved)
        {
            return false;
        }

        if (!IsFreeBallSituation())
        {
            FinishFreeBallChase(false);
            return true;
        }

        if (TeammateNpcGoapRoleDifferentiation.Enabled
            && !TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc())
        {
            FinishFreeBallChase(false);
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            FinishFreeBallChase(false);
            return true;
        }

        float distance = Vector3.Distance(_bb.PhysicalState.Position, teamBB.BallInfo.BallPosition);
        if (distance <= _nearBallDistance)
        {
            FinishFreeBallChase(true);
            return true;
        }

        return false;
    }

    private bool IsFreeBallSituation()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return false;
        return teamBB.BallInfo.BallState == BallManager_State.BALL_STATE.FREE
            && !teamBB.BallInfo.TeamHasBall
            && !teamBB.BallInfo.EnemyHasBall;
    }
}
