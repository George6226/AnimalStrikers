using Game.Goap;
using UnityEngine;

public class MoveToReceivePassActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "ReceivePass";

    private bool _isExecuting;
    private float _startTime;
    private float _maxChaseDuration;
    private float _nearBallDistance;
    private float _moveIntensity;
    private PlayerBlackboard _bb;
    private bool _motorResolved;

    /// <summary>直近の受けアクション完了理由（received / timeout / pass_ended / cancelled）。</summary>
    public string LastFinishReason { get; private set; } = string.Empty;

    public MoveToReceivePassActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is MoveToReceivePassActionSO receiveSO)
        {
            _maxChaseDuration = receiveSO.MaxChaseDuration;
            _nearBallDistance = receiveSO.NearBallDistance;
            _moveIntensity = receiveSO.MoveIntensity;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        return IncomingPassPlanning.CanExecuteIncomingPassReceive(bb);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapNpcMotor.TryResolve(bb, out _, out _, out _);
        _isExecuting = true;
        _startTime = Time.time;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute receive pass move", bb);
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null || !_motorResolved)
        {
            return;
        }

        if (!IncomingPassPlanning.TryGetReceiveMoveTarget(_bb, out Vector3 target))
        {
            return;
        }

        GoapNpcMotor.MoveToward(_bb, target, _moveIntensity, DiagCategory);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting || _bb == null)
        {
            return true;
        }

        if (IncomingPassPlanning.HasReceivedIncomingPass(_bb)
            || MainNpcAttackPlanning.IsSelfBallOwner(_bb))
        {
            Finish("received");
            return true;
        }

        if (Time.time - _startTime >= _maxChaseDuration)
        {
            Finish("timeout");
            return true;
        }

        if (!IncomingPassPlanning.IsIncomingPassTarget(_bb))
        {
            if (IncomingPassPlanning.IsReceiveCatchPhase(_bb))
            {
                return false;
            }

            Finish("pass_ended");
            return true;
        }

        return false;
    }

    public override void Cancel()
    {
        Finish("cancelled");
    }

    private void Finish(string reason)
    {
        if (_isExecuting && _bb != null && _motorResolved)
        {
            GoapNpcMotor.Stop(_bb, DiagCategory);
        }

        if (_isExecuting && _bb != null)
        {
            // 受け切れなかった場合もトラッカーを残すと IncomingPassTarget が再発火し NoGoal 固着する。
            if (reason != "received"
                && _bb.BasicData != null
                && GoapPassFlightTracker.IsTargetPlayer(_bb.BasicData.PlayerID))
            {
                GoapPassFlightTracker.Clear();
            }

            GoapMovementDiagnostic.Log(DiagCategory, $"Finish reason={reason}", _bb);
        }

        LastFinishReason = reason;
        _isExecuting = false;
        _bb = null;
        _motorResolved = false;
    }
}
