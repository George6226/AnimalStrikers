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
            // AnimEvent が来ない場合でも全体フラグを下ろす（全員 move 停止の防止）。
            GoapMovementDiagnostic.Log(DiagCategory, "Finish timeout — ForceFinishSpecial", _bb);
            GoapSpecialBridge.ForceFinishSpecial(_bb);
        }
        else
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish special settled", _bb);
            if (AnimalAction_Special.IsSpecialActive)
            {
                GoapSpecialBridge.ForceFinishSpecial(_bb);
            }
        }

        _isExecuting = false;
        return true;
    }

    public override void Cancel()
    {
        if (_started)
        {
            GoapSpecialBridge.ForceFinishSpecial(_bb);
        }

        _isExecuting = false;
        _bb = null;
    }
}
