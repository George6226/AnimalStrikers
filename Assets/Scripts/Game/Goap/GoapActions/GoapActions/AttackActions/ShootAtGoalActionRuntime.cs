using UnityEngine;

/// <summary>
/// Phase M1: ShootAtGoalActionSO のランタイム（AnimalAction_Shoot.shoot 呼び出し）。
/// </summary>
public class ShootAtGoalActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "ShootAtGoal";

    private bool _isExecuting;
    private bool _started;
    private float _startTime;
    private float _timeoutSeconds;
    private PlayerBlackboard _bb;

    public ShootAtGoalActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is ShootAtGoalActionSO shootSO)
        {
            _timeoutSeconds = shootSO.ExecutionTimeoutSeconds;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        return MainNpcAttackPlanning.CanShootAtGoal(bb)
            && GoapMainNpcAttackBridge.ResolveFacade(bb) != null;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!GoapMainNpcAttackBridge.TryExecuteShoot(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute failed: shoot unavailable", bb);
            _isExecuting = false;
            return;
        }

        _started = true;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute shoot invoked", bb);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting)
        {
            return true;
        }

        if (!_started)
        {
            return true;
        }

        if (_bb != null && !GoapMainNpcAttackBridge.IsHoldingBall(_bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish ball released", _bb);
            _isExecuting = false;
            return true;
        }

        if (Time.time - _startTime >= _timeoutSeconds)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish timeout", _bb);
            _isExecuting = false;
            return true;
        }

        return false;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        _bb = null;
    }
}
