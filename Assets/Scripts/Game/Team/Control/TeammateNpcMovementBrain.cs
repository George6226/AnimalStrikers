using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 味方NPCフィールドプレイヤーの移動（段階1: 攻守に応じた立ち位置へ移動）。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class TeammateNpcMovementBrain : MonoBehaviour
{
    [SerializeField] private AnimalControlAssignment _assignment;
    [SerializeField] private AnimalFacade _facade;

    [Header("移動")]
    [SerializeField] private float _stopDistance = 2.5f;
    [SerializeField] private float _moveIntensity = 1.0f;

    [Header("GOAP連携")]
    [Tooltip("ON=GOAP有効かつ非制御時に段階1へフォールバック。OFF=GOAPのみで移動（立ち止まりはGOAP側で対応）。")]
    [SerializeField] private bool _allowGoapIdleFallback;

    /// <summary>SquadControl から一括設定（シーン上の SerializeField より優先）。</summary>
    public static bool GlobalAllowGoapIdleFallback { get; set; }

    private AnimalActionSelector _actionSelector;
    private AnimalHandler _handler;
    private AnimalFormationSlot _formationSlot;
    private TeammateNpcTacticalMode _currentMode = TeammateNpcTacticalMode.Hold;
    private GoapAgent _cachedGoapAgent;
    private bool _isFallbackDriving;

    public TeammateNpcTacticalMode CurrentTacticalMode => _currentMode;
    public bool IsFallbackDriving => _isFallbackDriving;
    public string CurrentTacticalModeLabel => _currentMode switch
    {
        TeammateNpcTacticalMode.Support => "SUPPORT",
        TeammateNpcTacticalMode.Defend => "DEFEND",
        TeammateNpcTacticalMode.ChaseBall => "CHASE",
        TeammateNpcTacticalMode.Hold => "HOLD",
        _ => "-",
    };

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

        _formationSlot = GetComponent<AnimalFormationSlot>();

        if (_assignment != null)
        {
            _assignment.RoleChanged += OnRoleChanged;
        }

        CacheMovementComponents();
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
        if (role != AnimalControlRole.TeammateNpc)
        {
            _currentMode = TeammateNpcTacticalMode.Hold;
            StopMoving();
        }
    }

    private void FixedUpdate()
    {
        _isFallbackDriving = false;

        if (_assignment == null || _assignment.Role != AnimalControlRole.TeammateNpc)
        {
            return;
        }

        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            return;
        }

        var goap = GetGoapAgent();
        bool goapEnabled = goap != null && goap.enabled;

        if (goapEnabled && IsGoapControlling())
        {
            return;
        }

        if (goapEnabled && !AllowsGoapIdleFallback())
        {
            _currentMode = TeammateNpcTacticalMode.Hold;
            StopMoving();
            return;
        }

        if (goapEnabled)
        {
            // GOAPが有効でも実行中アクションがない間は段階1移動でフォールバック。
            _isFallbackDriving = true;
        }

        CacheMovementComponents();

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        if (teamBB == null || !teamBB.BallInfo.IsExistBall)
        {
            _currentMode = TeammateNpcTacticalMode.Hold;
            StopMoving();
            return;
        }

        int slotIndex = _formationSlot != null && _formationSlot.IsAssigned
            ? _formationSlot.Index
            : 0;

        var tactical = TeammateNpcTacticalPositionCalculator.Calculate(
            transform.position,
            slotIndex,
            teamBB,
            CollectOtherTeammatePositions(),
            _facade);

        if (!tactical.IsValid)
        {
            _currentMode = TeammateNpcTacticalMode.Hold;
            StopMoving();
            return;
        }

        _currentMode = tactical.Mode;
        MoveToward(tactical.TargetPosition);
    }

    public void SetAllowGoapIdleFallback(bool allow)
    {
        _allowGoapIdleFallback = allow;
    }

    private bool AllowsGoapIdleFallback()
    {
        return GlobalAllowGoapIdleFallback || _allowGoapIdleFallback;
    }

    private bool IsGoapControlling()
    {
        var goap = GetGoapAgent();
        return goap != null && goap.enabled && goap.IsActivelyControlling;
    }

    private GoapAgent GetGoapAgent()
    {
        if (_cachedGoapAgent == null)
        {
            _cachedGoapAgent = AnimalGoapBrainComponents.Resolve(gameObject).Agent;
        }

        return _cachedGoapAgent;
    }

    private List<Vector3> CollectOtherTeammatePositions()
    {
        var list = new List<Vector3>();
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return list;
        }

        Vector3 selfPos = transform.position;
        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            Vector3 p = ally.transform.position;
            if ((p - selfPos).sqrMagnitude < 0.25f)
            {
                continue;
            }

            list.Add(p);
        }

        return list;
    }

    private void MoveToward(Vector3 target)
    {
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
            _handler.move(slideScale, 1.0f);
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
