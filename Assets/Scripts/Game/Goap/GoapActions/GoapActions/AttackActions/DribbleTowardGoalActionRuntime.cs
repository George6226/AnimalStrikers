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
    private const float MinBurstBeforeShootReadySeconds = 0.25f;
    private const float StuckProgressWindowSeconds = 0.85f;
    private const float StuckProgressMinDelta = 0.35f;
    private PlayerBlackboard _bb;
    private Vector3 _progressSamplePosition;
    private float _nextProgressSampleTime;

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
        return MainNpcAttackPlanning.CanExecuteDribbleTowardGoal(bb)
            && GoapNpcMotor.TryResolve(bb, out _, out _, out _);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _bb = bb;
        _motorResolved = GoapNpcMotor.TryResolve(bb, out _, out _, out _);
        _isExecuting = true;
        _startTime = Time.time;
        _progressSamplePosition = GoapNpcMotor.GetSelfWorldPosition(bb);
        _nextProgressSampleTime = Time.time + StuckProgressWindowSeconds;
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

        if (Time.time >= _nextProgressSampleTime)
        {
            Vector3 now = GoapNpcMotor.GetSelfWorldPosition(_bb);
            float moved = Vector3.Distance(
                new Vector3(now.x, 0f, now.z),
                new Vector3(_progressSamplePosition.x, 0f, _progressSamplePosition.z));
            if (moved < StuckProgressMinDelta)
            {
                FinishDribble("stuck_no_progress");
                return;
            }

            _progressSamplePosition = now;
            _nextProgressSampleTime = Time.time + StuckProgressWindowSeconds;
        }
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

        if (MainNpcAttackPlanning.CanShootAtGoal(_bb)
            && Time.time - _startTime >= MinBurstBeforeShootReadySeconds)
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
