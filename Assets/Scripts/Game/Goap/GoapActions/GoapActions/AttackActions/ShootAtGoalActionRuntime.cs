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
        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        return MainNpcAttackPlanning.CanShootAtGoal(bb)
            && facade != null
            && !GoapBallActionGuard.IsShootInProgress(facade);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!GoapMainNpcAttackBridge.TryExecuteShoot(bb))
        {
            AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
            if (facade != null && GoapBallActionGuard.IsShootInProgress(facade))
            {
                GoapMovementDiagnostic.Log(DiagCategory, "Execute waiting: shoot already in progress", bb);
                return;
            }

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

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(_bb);
        if (facade != null && GoapBallActionGuard.IsShootInProgress(facade))
        {
            return false;
        }

        if (!_started)
        {
            return true;
        }

        if (Time.time - _startTime >= _timeoutSeconds)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish timeout", _bb);
            _isExecuting = false;
            return true;
        }

        GoapMovementDiagnostic.Log(DiagCategory, "Finish shoot settled", _bb);
        _isExecuting = false;
        return true;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        _bb = null;
    }
}
