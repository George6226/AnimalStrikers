using UnityEngine;
using Game.Goap;

/// <summary>
/// 支持角度アクションのランタイム実装。
/// 保持者の攻撃軸に追従し、前方翼レーンへ移動してパスオプションを確保する。
/// </summary>
public class CreateSupportAngleActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "SupportAngle";
    private const float FollowDriftThreshold = 0.6f;

    private bool _isExecuting;
    private float _executionStartTime;
    private float _executionTime;
    private float _movementSpeed;
    private float _retargetInterval;
    private CreateSupportAnglePositioning.Settings _positioningSettings;

    private PlayerBlackboard _playerBlackboard;
    private bool _motorResolved;
    private Vector3 _targetPosition;
    private Vector3 _lastOwnerPosition;
    private bool _hasLastOwnerPosition;
    private float _moveIntensity = 1f;
    private float _nextRetargetTime;
    private CreateSupportAnglePositioning.WingChannelState _wingChannelState;

    public CreateSupportAngleActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var createSupportAngleSO = origin as CreateSupportAngleActionSO;
        if (createSupportAngleSO == null)
        {
            return;
        }

        _executionTime = createSupportAngleSO.ExecutionTime;
        _movementSpeed = createSupportAngleSO.MovementSpeed;
        _retargetInterval = Mathf.Max(0.1f, createSupportAngleSO.RetargetInterval);
        _positioningSettings = new CreateSupportAnglePositioning.Settings
        {
            ForwardLeadRatio = createSupportAngleSO.ForwardLeadRatio,
            WingLaneRatio = createSupportAngleSO.WingLaneRatio,
            OptimalDistanceRatio = createSupportAngleSO.OptimalDistanceRatio,
            MinDistanceRatio = createSupportAngleSO.MinDistanceRatio,
            MaxDistanceRatio = createSupportAngleSO.MaxDistanceRatio,
            AngleTolerance = createSupportAngleSO.AngleTolerance,
        };
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (!TeammateNpcSupportPlanning.PassesTacticalAttackRuntimeGate(bb))
        {
            return false;
        }

        if (TeammateNpcSupportPlanning.BlocksCreateSupportAngleForCentralWidthLayout(bb))
        {
            return false;
        }

        return GoapTacticalMoveHelper.TryResolveMotor(bb);
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _motorResolved = GoapTacticalMoveHelper.TryResolveMotor(bb);
        _moveIntensity = Mathf.Clamp(_movementSpeed / 5f, 0.5f, 1f);
        _hasLastOwnerPosition = false;
        _wingChannelState = default;
        _targetPosition = CalculateTargetPosition(bb, "Execute", 0f);
        _isExecuting = true;
        _executionStartTime = Time.time;
        _nextRetargetTime = Time.time;
        LogBallContext(bb, "Execute");
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null || !_motorResolved)
        {
            return;
        }

        if (Time.time >= _nextRetargetTime)
        {
            Vector3 before = _targetPosition;
            _targetPosition = CalculateTargetPosition(
                _playerBlackboard,
                "Retarget",
                Vector3.Distance(before, _targetPosition));
            _nextRetargetTime = Time.time + _retargetInterval;
        }

        GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
    }

    public override bool IsComplete()
    {
        if (!_isExecuting)
        {
            return true;
        }

        bool arrived = _motorResolved
            && GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
        if (arrived)
        {
            Vector3 refreshedTarget = CalculateTargetPosition(_playerBlackboard, "ArrivedCheck", 0f);
            float drift = Vector3.Distance(
                GoapNpcMotor.GetSelfWorldPosition(_playerBlackboard),
                refreshedTarget);
            if (drift > FollowDriftThreshold)
            {
                _targetPosition = refreshedTarget;
                _executionStartTime = Time.time;
                GoapMovementDiagnostic.Log(
                    DiagCategory,
                    $"ContinueMoving drift={drift:F2} threshold={FollowDriftThreshold:F2} " +
                    $"newTarget={GoapMovementDiagnostic.FormatVector(refreshedTarget)}",
                    _playerBlackboard);
                arrived = false;
            }
            else
            {
                var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
                if (teamBB != null
                    && !TeammateNpcSupportPlanning.EvaluateMaintainingSupportRelationship(_playerBlackboard, teamBB))
                {
                    _targetPosition = refreshedTarget;
                    _executionStartTime = Time.time;
                    arrived = false;
                }
                else if (teamBB != null && !IsTacticalPassReceivePosition(_playerBlackboard, teamBB))
                {
                    _targetPosition = refreshedTarget;
                    arrived = false;
                }
            }
        }

        bool timedOut = Time.time - _executionStartTime >= _executionTime;
        if (!arrived && !timedOut)
        {
            return false;
        }

        Finish(timedOut ? "timeout" : "arrived");
        return true;
    }

    public override void Cancel()
    {
        Finish("cancel");
    }

    private void Finish(string reason)
    {
        if (_playerBlackboard != null)
        {
            CalculateTargetPosition(_playerBlackboard, $"Finish({reason})", 0f);
            GoapTacticalMoveHelper.Stop(_playerBlackboard, DiagCategory);
            TryApplyPassReceiveFactIfTactical(_playerBlackboard);
        }

        _isExecuting = false;
        _hasLastOwnerPosition = false;
        _wingChannelState = default;
    }

    private void TryApplyPassReceiveFactIfTactical(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return;
        }

        if (!TeammateNpcSupportPlanning.EvaluatePassReceivePosition(bb, teamBB))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish passReceive=false (tactical check failed)", bb);
            return;
        }

        if (!TeammateNpcSupportPlanning.EvaluateMaintainingSupportRelationship(bb, teamBB))
        {
            GoapMovementDiagnostic.Log(DiagCategory, "Finish passReceive=false (support relationship lost)", bb);
            return;
        }

        GoapMovementDiagnostic.Log(DiagCategory, "Finish passReceive=true (tactical check passed)", bb);
        GoapTacticalMoveHelper.ApplyPassReceiveFact(bb);
    }

    private static bool IsTacticalPassReceivePosition(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        return TeammateNpcSupportPlanning.EvaluatePassReceivePosition(bb, teamBB);
    }

    private Vector3 CalculateTargetPosition(PlayerBlackboard bb, string phase, float targetDelta)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return GoapNpcMotor.GetSelfWorldPosition(bb);
        }

        Vector3 selfPos = GoapNpcMotor.GetSelfWorldPosition(bb);
        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        Vector3 best = CreateSupportAnglePositioning.SelectBestPosition(
            selfPos,
            slot,
            teamBB,
            _positioningSettings,
            ref _wingChannelState,
            out CreateSupportAnglePositioning.PositioningSnapshot snapshot);

        float ownerDelta = 0f;
        Vector3 ownerPos = snapshot.OwnerPosition;
        if (_hasLastOwnerPosition)
        {
            ownerDelta = Vector3.Distance(ownerPos, _lastOwnerPosition);
        }

        _lastOwnerPosition = ownerPos;
        _hasLastOwnerPosition = true;

        GoapMovementDiagnostic.Log(
            DiagCategory,
            CreateSupportAnglePositioning.FormatDiagnosticLine(phase, snapshot, targetDelta, ownerDelta),
            bb);

        return best;
    }

    private static void LogBallContext(PlayerBlackboard bb, string phase)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            GoapMovementDiagnostic.Log(DiagCategory, $"{phase} ballContext=teamBB_null", bb);
            return;
        }

        var ball = teamBB.BallInfo;
        Vector3 selfPos = GoapNpcMotor.GetSelfWorldPosition(bb);
        GoapMovementDiagnostic.Log(
            DiagCategory,
            $"{phase} ballContext teamHasBall={ball.TeamHasBall} ownerId={ball.BallOwnerID} " +
            $"ownerPos={GoapMovementDiagnostic.FormatVector(ball.BallOwnerPosition)} " +
            $"ballPos={GoapMovementDiagnostic.FormatVector(ball.BallPosition)} " +
            $"selfPos={GoapMovementDiagnostic.FormatVector(selfPos)} ballDist={bb.BallState.BallDistance:F2}",
            bb);
    }
}
