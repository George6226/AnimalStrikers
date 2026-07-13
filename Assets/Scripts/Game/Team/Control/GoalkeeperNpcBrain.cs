using UnityEngine;

/// <summary>
/// 味方/敵 GK 用 NPC 思考（F3）。ゴールライン位置取りとボール追従。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class GoalkeeperNpcBrain : MonoBehaviour
{
    private const string ReceiveLayerName = "Animal_Receive";
    private const float ProximityContactDistance = 0.45f;
    private const float ProximityHandleCooldownSeconds = 0.15f;

    [SerializeField] private AnimalControlAssignment _assignment;
    [SerializeField] private AnimalFacade _facade;

    [Header("移動")]
    [SerializeField] private float _stopDistance = 0.6f;
    [SerializeField] private float _moveIntensity = 1f;
    [SerializeField] private float _lineDepth = 3.5f;
    [SerializeField] private float _goalMouthHalfWidth = 3.5f;
    [SerializeField] private float _rushLooseBallDistance = 8f;
    [SerializeField] private float _saveReachDistance = 3.5f;

    private AnimalHandler _handler;
    private Collider _ballReceiveCollider;
    private AnimalCollider_Body _ballReceiveBody;
    private GoalkeeperPositioning.Mode _currentMode = GoalkeeperPositioning.Mode.HoldLine;
    private bool _diagSessionStarted;
    private float _lastProximityHandleTime = -999f;

    public GoalkeeperPositioning.Mode CurrentMode => _currentMode;
    public float SaveReachDistance => _saveReachDistance;

    private void Awake()
    {
        if (_assignment == null)
        {
            _assignment = GetComponent<AnimalControlAssignment>();
        }

        if (_facade == null)
        {
            _facade = GetComponent<AnimalFacade>();
        }

        if (_assignment != null)
        {
            _assignment.RoleChanged += OnRoleChanged;
        }

        CacheMovementComponents();
        if (_assignment != null && _assignment.Role == AnimalControlRole.GoalkeeperNpc)
        {
            EnsureGoalkeeperBallCollider();
        }
    }

    private void OnDestroy()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= OnRoleChanged;
        }
    }

    private void OnRoleChanged(AnimalControlRole role)
    {
        if (role != AnimalControlRole.GoalkeeperNpc)
        {
            _currentMode = GoalkeeperPositioning.Mode.HoldLine;
            StopMoving();
            return;
        }

        EnsureGoalkeeperBallCollider();
    }

    private void FixedUpdate()
    {
        MaybeStartDiagnosticSession();

        if (_assignment == null || _assignment.Role != AnimalControlRole.GoalkeeperNpc)
        {
            return;
        }

        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            StopMoving();
            return;
        }

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        if (teamBB == null || !teamBB.BallInfo.IsExistBall)
        {
            StopMoving();
            return;
        }

        bool mirrored = GoalkeeperPositioning.IsMirroredGoalkeeper(_facade);
        var ball = teamBB.BallInfo;
        var result = GoalkeeperPositioning.Compute(
            teamBB,
            mirrored,
            ball.BallPosition,
            ball.BallState,
            GoapFieldNpcPerspective.EffectiveEnemyHasBall(teamBB, mirrored),
            GoapFieldNpcPerspective.EffectiveTeamHasBall(teamBB, mirrored),
            _lineDepth,
            _goalMouthHalfWidth,
            _rushLooseBallDistance);

        if (!result.IsValid)
        {
            StopMoving();
            return;
        }

        _currentMode = result.Mode;
        MoveLaterally(result.TargetPosition);
        TryProximityBallContact();
    }

    private void MaybeStartDiagnosticSession()
    {
        GoalkeeperDiagnosticLog.SyncFromEnvironmentAndGoap();
        if (_diagSessionStarted || !GoalkeeperDiagnosticLog.Enabled)
        {
            return;
        }

        _diagSessionStarted = true;
        GoalkeeperDiagnosticLog.ResetSession();
    }

    /// <summary>ゴールライン上の X 方向のみ移動（Z は位置取りロジックのホームラインを維持）。</summary>
    private void MoveLaterally(Vector3 target)
    {
        CacheMovementComponents();
        if (_handler == null)
        {
            return;
        }

        float deltaX = target.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= _stopDistance)
        {
            StopMoving();
            return;
        }

        float direction = Mathf.Sign(deltaX) * _moveIntensity;
        _handler.moveGoalkeeperLateral(direction);
    }

    private void CacheMovementComponents()
    {
        if (_facade == null)
        {
            return;
        }

        if (_handler == null)
        {
            _handler = _facade.GetAnimalHandler();
        }
    }

    private void StopMoving()
    {
        _handler?.keeperStand();
    }

    private void EnsureGoalkeeperBallCollider()
    {
        int receiveLayer = LayerMask.NameToLayer(ReceiveLayerName);
        var bodyColliders = GetComponentsInChildren<AnimalCollider_Body>(true);
        foreach (var body in bodyColliders)
        {
            if (body == null)
            {
                continue;
            }

            var col = body.GetComponent<Collider>();
            if (col == null)
            {
                continue;
            }

            if (receiveLayer >= 0)
            {
                body.gameObject.layer = receiveLayer;
            }

            col.isTrigger = true;
            col.enabled = true;

            if (body.gameObject.name.Contains("BallReceive"))
            {
                _ballReceiveCollider = col;
                _ballReceiveBody = body;
            }

            GoalkeeperDiagnosticLog.SyncFromEnvironmentAndGoap();
            GoalkeeperDiagnosticLog.Write(
                $"[GK_COLLIDER] name={col.name} layer={LayerMask.LayerToName(body.gameObject.layer)} " +
                $"isTrigger={col.isTrigger} enabled={col.enabled} center={col.bounds.center} size={col.bounds.size}");
        }
    }

    private void TryProximityBallContact()
    {
        if (_ballReceiveCollider == null || _ballReceiveBody == null)
        {
            return;
        }

        var ballManager = TeamFacade.Instance != null ? TeamFacade.Instance.BallManager : null;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        var ball = ballManager != null ? ballManager.Ball : null;
        if (ball == null || teamBB == null)
        {
            return;
        }

        var ballState = teamBB.BallInfo.BallState;
        if (ballState != BallManager_State.BALL_STATE.SHOOT
            && ballState != BallManager_State.BALL_STATE.FREE)
        {
            return;
        }

        var ballCol = ball.GetComponent<Collider>();
        bool ballColEnabled = ballCol != null && ballCol.enabled;
        Vector3 ballPos = ball.transform.position;
        Vector3 closest = _ballReceiveCollider.ClosestPoint(ballPos);
        float dist = Vector3.Distance(closest, ballPos);
        bool near = dist <= ProximityContactDistance;
        bool withinSaveReach = dist <= _saveReachDistance;

        if (GoalkeeperDiagnosticLog.Enabled && (near || ballState == BallManager_State.BALL_STATE.SHOOT))
        {
            GoalkeeperDiagnosticLog.WriteProximityThrottled(
                $"[GK_PROBE] mode={_currentMode} ballState={ballState} dist={dist:F2} probe_near={near} " +
                $"saveReach={withinSaveReach} ballColEnabled={ballColEnabled} ballPos={ballPos} gkPos={transform.position}");
        }

        if (!near)
        {
            return;
        }

        if (ballState == BallManager_State.BALL_STATE.SHOOT && !withinSaveReach)
        {
            return;
        }

        if (!ballColEnabled)
        {
            GoalkeeperDiagnosticLog.Write(
                $"[GK_SKIP] source=proximity_probe reason=ball_collider_disabled state={ballState}");
            return;
        }

        if (Time.time - _lastProximityHandleTime < ProximityHandleCooldownSeconds)
        {
            return;
        }

        _lastProximityHandleTime = Time.time;
        if (_ballReceiveBody.TryGoalkeeperBallContact(ball, "proximity_probe"))
        {
            GoalkeeperDiagnosticLog.Write($"[GK_PROBE] proximity_contact_handled state={ballState}");
        }
    }
}
