using UnityEngine;

/// <summary>
/// Phase M1: PassToTeammateActionSO のランタイム（AnimalAction_Pass.pass 呼び出し）。
/// </summary>
public class PassToTeammateActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "PassToTeammate";

    private bool _isExecuting;
    private bool _started;
    private float _startTime;
    private float _timeoutSeconds;
    private PlayerBlackboard _bb;

    public PassToTeammateActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is PassToTeammateActionSO passSO)
        {
            _timeoutSeconds = passSO.ExecutionTimeoutSeconds;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        return MainNpcAttackPlanning.CanPassToTeammate(bb)
            && GoapMainNpcAttackBridge.IsHoldingBall(bb)
            && facade != null
            && !GoapBallActionGuard.IsPassInProgress(facade);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!GoapMainNpcAttackBridge.IsHoldingBall(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute skipped: not holding ball", bb);
            _isExecuting = false;
            return;
        }

        if (!GoapMainNpcAttackBridge.TryExecutePass(bb))
        {
            AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
            if (facade != null && GoapBallActionGuard.IsPassInProgress(facade))
            {
                GoapMovementDiagnostic.Log(DiagCategory, "Execute waiting: pass already in progress", bb);
                return;
            }

            GoapMovementDiagnostic.Log(DiagCategory, "Execute failed: pass unavailable", bb);
            _isExecuting = false;
            return;
        }

        _started = true;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute pass invoked", bb);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting)
        {
            return true;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(_bb);
        if (facade != null && GoapBallActionGuard.IsPassInProgress(facade))
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

        GoapMovementDiagnostic.Log(DiagCategory, "Finish pass settled", _bb);
        _isExecuting = false;
        return true;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        _bb = null;
    }
}
