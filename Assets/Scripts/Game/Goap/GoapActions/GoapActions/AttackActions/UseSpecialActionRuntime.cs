using UnityEngine;

/// <summary>
/// F5: UseSpecialActionSO のランタイム（AnimalAction_Special.Execute）。
/// </summary>
public class UseSpecialActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "UseSpecial";

    private bool _isExecuting;
    private bool _started;
    private float _startTime;
    private float _timeoutSeconds = 5f;
    private PlayerBlackboard _bb;

    public UseSpecialActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is UseSpecialActionSO specialSO)
        {
            _timeoutSeconds = specialSO.ExecutionTimeoutSeconds;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        return MainNpcAttackPlanning.CanUseSpecial(bb);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!GoapSpecialBridge.TryExecuteSpecial(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute failed: special unavailable", bb);
            _isExecuting = false;
            return;
        }

        _started = true;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute special invoked", bb);
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

        if (AnimalAction_Special.IsSpecialActive
            && Time.time - _startTime < _timeoutSeconds)
        {
            return false;
        }

        if (Time.time - _startTime >= _timeoutSeconds)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish timeout", _bb);
        }
        else
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish special settled", _bb);
        }

        _isExecuting = false;
        return true;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        _bb = null;
    }
}
