using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

public class BlockPassLaneActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "BlockPassLane";

    private bool _isExecuting;
    private float _startTime;
    private float _executionTime = 2.5f;
    private float _moveIntensity = 1f;

    private PlayerBlackboard _bb;
    private bool _motorResolved;
    private Vector3 _target;

    public BlockPassLaneActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as BlockPassLaneActionSO;
        if (so != null)
        {
            _executionTime = so.ExecutionTime;
            _moveIntensity = Mathf.Clamp(so.MoveSpeed / 4f, 0.5f, 1f);
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (bb.GetFact(new Fact(SymbolTag.Tactical.TEAM_HAS_BALL, "true")) == true) return false;
        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true) return false;
        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true) return false;
        if (TeammateNpcDefensePlanning.BlocksWhenAlreadyInDefensivePosition(bb)
            && bb.GetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true")) == true)
        {
            return false;
        }
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
            && GoapTacticalMoveHelper.MoveToward(_bb, _target, _moveIntensity, DiagCategory);
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

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 playerPos = bb.PhysicalState.Position;
        List<Vector3> enemiesWithoutBall = new List<Vector3>();
        foreach (var enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(enemyPos, ownerPos) > 0.1f)
            {
                enemiesWithoutBall.Add(enemyPos);
            }
        }

        if (enemiesWithoutBall.Count == 0)
        {
            Vector3 fallbackTarget = playerPos;
            ClampToField(ref fallbackTarget);
            return fallbackTarget;
        }

        Vector3 bestTarget = ownerPos;
        float minDistance = float.MaxValue;
        foreach (var enemyPos in enemiesWithoutBall)
        {
            Vector3 midpoint = (ownerPos + enemyPos) * 0.5f;
            float distance = Vector3.Distance(playerPos, midpoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestTarget = midpoint;
            }
        }

        ClampToField(ref bestTarget);
        return bestTarget;
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
