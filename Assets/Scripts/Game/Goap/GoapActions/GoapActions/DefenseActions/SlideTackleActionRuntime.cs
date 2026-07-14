using UnityEngine;

/// <summary>
/// F4: SlideTackleActionSO のランタイム（AnimalAction_Sliding.Execute 呼び出し）。
/// </summary>
public class SlideTackleActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "SlideTackle";

    private bool _isExecuting;
    private bool _started;
    private float _startTime;
    private float _timeoutSeconds = 0.85f;
    private PlayerBlackboard _bb;

    public SlideTackleActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is SlideTackleActionSO slideSO)
        {
            _timeoutSeconds = slideSO.ExecutionTimeoutSeconds;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.CanSlideTackle(bb)
            && GoapSlideTackleBridge.HasSlidingAction(bb);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _started = false;
        _startTime = Time.time;

        if (!CanExecute(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute skipped: not slide-ready", bb);
            _isExecuting = false;
            return;
        }

        if (!GoapSlideTackleBridge.TryExecuteSliding(bb))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Execute failed: sliding unavailable", bb);
            _isExecuting = false;
            return;
        }

        _started = true;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute sliding invoked", bb);
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

        if (Time.time - _startTime < _timeoutSeconds)
        {
            return false;
        }

        GoapMovementDiagnostic.Log(DiagCategory, "Finish sliding timeout", _bb);
        _isExecuting = false;
        return true;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        _bb = null;
    }
}
