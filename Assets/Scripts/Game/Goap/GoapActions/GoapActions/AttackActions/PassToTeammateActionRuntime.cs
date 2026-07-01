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
        return MainNpcAttackPlanning.CanPassToTeammate(bb)
            && GoapMainNpcAttackBridge.ResolveFacade(bb) != null;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!GoapMainNpcAttackBridge.TryExecutePass(bb))
        {
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
