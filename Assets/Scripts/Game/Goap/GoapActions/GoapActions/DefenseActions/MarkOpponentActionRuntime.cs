using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

public class MarkOpponentActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "MarkOpponent";

    private bool _isExecuting;
    private float _startTime;
    private float _executionTime = 2.5f;
    private float _moveIntensity = 1f;
    private float _markDistanceRatio = 0.08f;

    private PlayerBlackboard _bb;
    private bool _motorResolved;
    private Vector3 _target;

    public MarkOpponentActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as MarkOpponentActionSO;
        if (so != null)
        {
            _executionTime = so.ExecutionTime;
            _moveIntensity = Mathf.Clamp(so.MoveSpeed / 4f, 0.5f, 1f);
            _markDistanceRatio = so.MarkDistanceRatio;
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

    public override void Cancel()
    {
        Finish();
    }

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
        if (teamBB == null)
        {
            return bb.PhysicalState.Position;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 ownGoal = teamBB.FieldInfo.OwnGoalPosition;
        Vector3 playerPos = bb.PhysicalState.Position;
        float fieldLen = teamBB.FieldInfo.FieldLength;

        Vector3 bestEnemy = SelectTargetEnemy(ownerPos, playerPos, fieldLen);

        float ideal = fieldLen * _markDistanceRatio;
        Vector3 dir = (ownGoal - bestEnemy).normalized;
        Vector3 target = bestEnemy + dir * ideal;
        ClampToField(ref target);
        return target;
    }

    private Vector3 SelectTargetEnemy(Vector3 ownerPos, Vector3 playerPos, float fieldLen)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return playerPos;

        float pressureThreshold = fieldLen * 0.15f;
        int pressureCount = 0;
        foreach (var allyPos in teamBB.BasicInfo.TeammatePositions)
        {
            if (Vector3.Distance(allyPos, playerPos) < 0.1f) continue;

            if (Vector3.Distance(allyPos, ownerPos) <= pressureThreshold)
            {
                pressureCount++;
            }
        }

        if (pressureCount <= 1)
        {
            return ownerPos;
        }

        List<Vector3> enemiesWithoutBall = new List<Vector3>();
        foreach (var e in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(e, ownerPos) > 0.1f)
            {
                enemiesWithoutBall.Add(e);
            }
        }

        if (enemiesWithoutBall.Count == 0)
        {
            return ownerPos;
        }

        float minDistance = float.MaxValue;
        Vector3 bestEnemy = ownerPos;
        foreach (var e in enemiesWithoutBall)
        {
            float distance = Vector3.Distance(e, playerPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestEnemy = e;
            }
        }

        return bestEnemy;
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
