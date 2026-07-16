using UnityEngine;
using Game.Goap;

public class MoveToFreeBallActionRuntime : GoapActionRuntime
{
    private bool _isExecuting;
    private float _startTime;
    private float _maxChaseDuration = 8f;
    private float _nearBallDistance = 0.55f;
    private float _moveIntensity = 1f;
    private const float PickupAssistDistance = 0.65f;

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
            && !TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc(bb))
        {
            return false;
        }

        if (TeammateNpcGoapRoleDifferentiation.Enabled
            && !TeammateNpcGoapRoleDifferentiation.ShouldLeadFreeBallChase(bb))
        {
            return false;
        }

        // 近傍でもピックアップ補助のため実行可（旧: minDistance 未満で CanExecute=false → NoGoal）
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

        TryPickupBallIfClose();
        Vector3 ballPos = teamBB.BallInfo.BallPosition;
        float dist = Vector3.Distance(GoapNpcMotor.GetSelfWorldPosition(_bb), ballPos);
        bool useDash = GoapDashPlanning.ResolveDashForFreeBall(_bb, dist);
        GoapNpcMotor.MoveToward(_bb, ballPos, _moveIntensity, "FreeBall", useDash);
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

        if (_bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            FinishFreeBallChase(false);
            return true;
        }

        if (!IsFreeBallSituation())
        {
            FinishFreeBallChase(false);
            return true;
        }

        if (TeammateNpcGoapRoleDifferentiation.Enabled
            && !TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc(_bb))
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
        if (distance > _nearBallDistance)
        {
            return false;
        }

        _bb.SetFact(new Fact(SymbolTag.Position.NEAR_BALL, "true"), true);
        _bb.SetFact(new Fact(SymbolTag.Position.NEAR_BALL, "false"), false);

        // 近傍で拾えない場合は一度完了して再計画（同一アクション滞留で固着しない）
        if (Time.time - _startTime >= Mathf.Min(1.25f, _maxChaseDuration * 0.25f))
        {
            FinishFreeBallChase(true);
            return true;
        }

        return false;
    }

    private void TryPickupBallIfClose()
    {
        if (_bb == null
            || _bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true
            || !IsFreeBallSituation())
        {
            return;
        }

        var ballManager = TeamFacade.Instance != null ? TeamFacade.Instance.BallManager : null;
        var hBall = ballManager != null ? ballManager.Ball : null;
        if (hBall == null)
        {
            return;
        }

        float distance = Vector3.Distance(_bb.PhysicalState.Position, hBall.transform.position);
        if (distance > PickupAssistDistance)
        {
            return;
        }

        if (_bb.BasicData?.Self == null)
        {
            return;
        }

        var body = _bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?.GetComponentInChildren<AnimalCollider_Body>(true);
        body?.TryAcquireBall(hBall);
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
