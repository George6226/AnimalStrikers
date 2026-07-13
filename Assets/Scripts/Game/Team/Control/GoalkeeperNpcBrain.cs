using UnityEngine;

/// <summary>
/// 味方/敵 GK 用 NPC 思考（F3）。ゴールライン位置取りとボール追従。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class GoalkeeperNpcBrain : MonoBehaviour
{
    [SerializeField] private AnimalControlAssignment _assignment;
    [SerializeField] private AnimalFacade _facade;

    [Header("移動")]
    [SerializeField] private float _stopDistance = 0.6f;
    [SerializeField] private float _moveIntensity = 1f;
    [SerializeField] private float _lineDepth = 3.5f;
    [SerializeField] private float _goalMouthHalfWidth = 3.5f;
    [SerializeField] private float _rushLooseBallDistance = 8f;

    private AnimalHandler _handler;
    private GoalkeeperPositioning.Mode _currentMode = GoalkeeperPositioning.Mode.HoldLine;

    public GoalkeeperPositioning.Mode CurrentMode => _currentMode;

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
        var bodyColliders = GetComponentsInChildren<AnimalCollider_Body>(true);
        foreach (var body in bodyColliders)
        {
            if (body == null)
            {
                continue;
            }

            var col = body.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.enabled = true;
            }
        }
    }
}
