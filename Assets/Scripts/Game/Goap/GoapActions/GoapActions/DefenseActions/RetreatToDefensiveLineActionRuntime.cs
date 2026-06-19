using UnityEngine;
using Game.Goap;

public class RetreatToDefensiveLineActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "RetreatLine";

    private bool _isExecuting;
    private float _startTime;
    private float _executionTime = 2.0f;
    private float _moveIntensity = 1f;
    private float _retreatDepthRatio = 0.28f;
    private float _centralBias = 0.6f;

    private PlayerBlackboard _bb;
    private bool _motorResolved;
    private Vector3 _target;

    public RetreatToDefensiveLineActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as RetreatToDefensiveLineActionSO;
        if (so != null)
        {
            _executionTime = so.ExecutionTime;
            _moveIntensity = Mathf.Clamp(so.MoveSpeed / 4f, 0.5f, 1f);
            _retreatDepthRatio = so.RetreatDepthRatio;
            _centralBias = so.CentralBias;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (bb.GetFact(new Fact(SymbolTag.Tactical.TEAM_HAS_BALL, "true")) == true) return false;
        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true) return false;
        return GoapTacticalMoveHelper.TryResolveMotor(bb);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapTacticalMoveHelper.TryResolveMotor(bb);
        _target = CalculateTarget(bb);
        _isExecuting = true;
        _startTime = Time.time;
        GoapMovementDiagnostic.Log(DiagCategory, $"Execute target={GoapMovementDiagnostic.FormatVector(_target)}", bb);
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null || !_motorResolved) return;
        GoapTacticalMoveHelper.MoveToward(_bb, _target, _moveIntensity, DiagCategory);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting) return true;

        bool arrived = _motorResolved
            && GoapTacticalMoveHelper.MoveToward(_bb, _target, _moveIntensity, DiagCategory, 0.75f);
        bool timedOut = Time.time - _startTime >= _executionTime;
        if (!arrived && !timedOut) return false;

        Finish();
        return true;
    }

    public override void Cancel() => Finish();

    private void Finish()
    {
        if (_bb != null)
        {
            GoapTacticalMoveHelper.Stop(_bb, DiagCategory);
            GoapTacticalMoveHelper.ApplyDefensivePositionFact(_bb);
        }

        _isExecuting = false;
    }

    private Vector3 CalculateTarget(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return bb.PhysicalState.Position;

        var field = teamBB.FieldInfo;
        Vector3 ownGoal = field.OwnGoalPosition;
        Vector3 center = field.FieldCenter;
        float L = field.FieldLength;
        float depth = L * _retreatDepthRatio;

        float sign = Mathf.Sign(center.z - ownGoal.z);
        float lineZ = ownGoal.z + depth * sign;
        Vector3 linePoint = new Vector3(center.x, center.y, lineZ);

        Vector3 ball = teamBB.BallInfo.BallPosition;
        Vector3 lateral = Vector3.ProjectOnPlane(ball - linePoint, Vector3.up);
        if (lateral.sqrMagnitude > 0.01f)
        {
            linePoint += lateral.normalized * lateral.magnitude * (1f - _centralBias) * 0.5f;
        }

        ClampToField(ref linePoint);
        return linePoint;
    }

    private static void ClampToField(ref Vector3 p)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;

        float L = teamBB.FieldInfo.FieldLength;
        float W = teamBB.FieldInfo.FieldWidth;
        p.x = Mathf.Clamp(p.x, -W * 0.5f, W * 0.5f);
        p.z = Mathf.Clamp(p.z, -L * 0.5f, L * 0.5f);
    }
}
