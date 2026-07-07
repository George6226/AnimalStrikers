using UnityEngine;

/// <summary>
/// ボール保持のまま攻撃ゴール方向へ短時間ドリブルし、再計画で Pass/Shoot を選び直す。
/// </summary>
public class DribbleTowardGoalActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "Dribble";

    private bool _isExecuting;
    private bool _motorResolved;
    private float _startTime;
    private float _burstDuration;
    private float _moveIntensity;
    private PlayerBlackboard _bb;

    public DribbleTowardGoalActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is DribbleTowardGoalActionSO dribbleSO)
        {
            _burstDuration = dribbleSO.BurstDurationSeconds;
            _moveIntensity = dribbleSO.MoveIntensity;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        return MainNpcAttackPlanning.CanDribbleTowardGoal(bb)
            && GoapNpcMotor.TryResolve(bb, out _, out _, out _);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapNpcMotor.TryResolve(bb, out _, out _, out _);
        _isExecuting = true;
        _startTime = Time.time;
        GoapMovementDiagnostic.Log(DiagCategory, "Execute burst start", bb);
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _bb == null || !_motorResolved)
        {
            return;
        }

        if (!MainNpcAttackPlanning.IsSelfBallOwner(_bb))
        {
            return;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return;
        }

        bool mirrored = GoapFieldNpcPerspective.IsMirrored(_bb);
        Vector3 attackGoal = GoapFieldNpcPerspective.GetAttackGoalPosition(teamBB, mirrored);
        GoapNpcMotor.MoveToward(_bb, attackGoal, _moveIntensity, DiagCategory);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting || _bb == null)
        {
            return true;
        }

        if (!MainNpcAttackPlanning.IsSelfBallOwner(_bb))
        {
            FinishDribble("lost_ball");
            return true;
        }

        if (MainNpcAttackPlanning.CanShootAtGoal(_bb))
        {
            FinishDribble("shoot_ready");
            return true;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(_bb);
        if (facade != null && GoapBallActionGuard.IsAnyInProgress(facade))
        {
            FinishDribble("ball_action_started");
            return true;
        }

        if (Time.time - _startTime >= _burstDuration)
        {
            FinishDribble("burst_timeout");
            return true;
        }

        return false;
    }

    public override void Cancel()
    {
        FinishDribble("cancelled");
    }

    private void FinishDribble(string reason)
    {
        if (_isExecuting && _bb != null && _motorResolved)
        {
            GoapNpcMotor.Stop(_bb, DiagCategory);
        }

        if (_isExecuting && _bb != null)
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"Finish reason={reason}", _bb);
        }

        _isExecuting = false;
        _bb = null;
        _motorResolved = false;
    }
}
