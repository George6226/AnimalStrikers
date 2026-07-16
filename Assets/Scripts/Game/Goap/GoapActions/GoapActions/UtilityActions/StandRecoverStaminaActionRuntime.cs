using UnityEngine;

/// <summary>
/// 6-C P0: StandRecoverStaminaActionSO のランタイム。
/// </summary>
public class StandRecoverStaminaActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "StandRecoverStamina";

    private bool _isExecuting;
    private float _startTime;
    private float _timeoutSeconds = 8f;
    private PlayerBlackboard _bb;

    public StandRecoverStaminaActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is StandRecoverStaminaActionSO recoverSO)
        {
            _timeoutSeconds = recoverSO.ExecutionTimeoutSeconds;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true")) == true)
        {
            return false;
        }

        return bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) == true;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _isExecuting = true;
        _startTime = Time.time;
        GoapNpcMotor.Stop(bb, DiagCategory);
        GoapMovementDiagnostic.Log(DiagCategory, "Execute stand recover", bb);
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null)
        {
            return;
        }

        GoapNpcMotor.Stop(_bb, DiagCategory);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting)
        {
            return true;
        }

        if (_bb != null && _bb.GetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true")) == true)
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish hasStamina", _bb);
            _isExecuting = false;
            return true;
        }

        if (GoapStaminaPlanning.TryReadStaminaRatio(_bb, out float ratio)
            && GoapStaminaPlanning.HasSufficientStamina(ratio))
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"Finish ratio={ratio:F2}", _bb);
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
