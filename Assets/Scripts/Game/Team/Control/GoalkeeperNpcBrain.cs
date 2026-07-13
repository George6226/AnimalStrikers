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

    private AnimalActionSelector _actionSelector;
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
    }

    private void OnEnable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged += OnRoleChanged;
            OnRoleChanged(_assignment.Role);
        }
    }

    private void OnDisable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= OnRoleChanged;
        }
    }

    private void OnRoleChanged(AnimalControlRole role)
    {
        enabled = role == AnimalControlRole.GoalkeeperNpc;
        if (!enabled)
        {
            _currentMode = GoalkeeperPositioning.Mode.HoldLine;
            StopMoving();
        }
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
        MoveToward(result.TargetPosition);
    }

    private void MoveToward(Vector3 target)
    {
        CacheMovementComponents();

        Vector3 pos = transform.position;
        Vector3 toTarget = target - pos;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= _stopDistance * _stopDistance)
        {
            StopMoving();
            return;
        }

        float radian = Mathf.Atan2(-toTarget.x, toTarget.z);
        Move(_moveIntensity, radian);
    }

    private void CacheMovementComponents()
    {
        if (_facade == null)
        {
            return;
        }

        if (_actionSelector == null)
        {
            _actionSelector = _facade.GetActionSelector();
        }

        if (_handler == null)
        {
            _handler = _facade.GetAnimalHandler();
        }
    }

    private void Move(float slideScale, float radian)
    {
        if (_actionSelector != null)
        {
            _actionSelector.ExecuteMoveAction(slideScale, radian);
            return;
        }

        if (_handler != null)
        {
            _handler.move(slideScale, 1f);
            _handler.rotate(radian);
        }
    }

    private void StopMoving()
    {
        if (_actionSelector != null)
        {
            _actionSelector.ExecuteMoveAction(0f, 0f);
            return;
        }

        _handler?.stand();
    }
}
