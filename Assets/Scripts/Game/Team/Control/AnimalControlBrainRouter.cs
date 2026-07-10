using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 操作ロールに応じて子階層の GOAP（GoapAgent / AIContextSwitcher）の有効/無効を切り替える。
/// </summary>
[RequireComponent(typeof(AnimalControlAssignment))]
public class AnimalControlBrainRouter : MonoBehaviour
{
    private enum LocalGoapDebugSide
    {
        Both = 0,
        AllyOnly = 1,
        EnemyOnly = 2,
    }

    [SerializeField] private AnimalControlAssignment _assignment;
    [Header("Local Debug")]
    [SerializeField] private bool _enableLocalGoapFilter;
    [SerializeField] private LocalGoapDebugSide _localGoapDebugSide = LocalGoapDebugSide.Both;
    [Tooltip("空の場合はサイド条件のみ。指定すると PlayerID 一致のみ GOAP 有効。")]
    [SerializeField] private List<int> _localGoapAllowedPlayerIds = new();

    private AnimalFacade _facade;
    private AnimalGoapBrainComponents _goap;
    private bool _goapConfigured;
    private bool _productionGoapActive;
    private bool _enemyMainGoapActive;
    private bool _matchPlayWasActive;

    public bool IsProductionMainGoapActive => _productionGoapActive;

    private void Awake()
    {
        if (_assignment == null)
        {
            _assignment = GetComponent<AnimalControlAssignment>();
        }

        _facade = GetComponent<AnimalFacade>();
        _goap = AnimalGoapBrainComponents.Resolve(gameObject);
        _goap.SetActive(false);
    }

    private void OnEnable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged += ApplyRole;
            ApplyRole(_assignment.Role);
        }
    }

    private void OnDisable()
    {
        if (_assignment != null)
        {
            _assignment.RoleChanged -= ApplyRole;
        }

        _goap.SetActive(false);
    }

    public void ApplyRole(AnimalControlRole role)
    {
        if (!PassesLocalGoapDebugFilter(role))
        {
            _productionGoapActive = false;
            _enemyMainGoapActive = false;
            _goap.SetActive(false);
            return;
        }

        bool useGoap = role == AnimalControlRole.TeammateNpc && ShouldUseGoapPilot();
        if (role == AnimalControlRole.EnemyFieldNpc)
        {
            var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
            useGoap = enemySquad != null && _facade != null && enemySquad.ShouldUseGoapFor(_facade);
        }
        else if ((GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive) && _facade != null)
        {
            var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            useGoap = squad != null && squad.ShouldUseGoapFor(_facade);
        }
        else if (GoapMainNpcProductionEnvironment.IsActive
            && role == AnimalControlRole.Human
            && _facade != null
            && GoapMainNpcProductionEnvironment.IsProductionMainPlayer(_facade))
        {
            useGoap = false;
            _productionGoapActive = false;
        }

        if (GoapMainNpcVerifyEnvironment.RequiresBootstrap)
        {
            useGoap = false;
        }

        if (useGoap && !GoapMatchPlayGate.IsMatchPlayActive())
        {
            useGoap = false;
        }

        if (useGoap)
        {
            TryConfigureGoapPilot();
        }

        _goap.SetActive(useGoap);
    }

    private void LateUpdate()
    {
        bool matchPlayActive = GoapMatchPlayGate.IsMatchPlayActive();
        if (matchPlayActive != _matchPlayWasActive)
        {
            _matchPlayWasActive = matchPlayActive;
            if (_assignment != null)
            {
                // READY→GAME / GAME→RESULT でフィールド NPC の GOAP 有効を切り替える。
                ApplyRole(_assignment.Role);
            }
        }

        RefreshProductionMainNpcGoap();
        RefreshEnemyMainNpcGoap();
    }

    private void RefreshProductionMainNpcGoap()
    {
        if (!GoapMainNpcProductionEnvironment.IsActive
            || _facade == null
            || _assignment == null
            || !_assignment.IsHumanControlled
            || !PassesLocalGoapDebugFilter(_assignment.Role))
        {
            _productionGoapActive = false;
            return;
        }

        bool wantGoap = GoapMainNpcProductionEnvironment.ShouldEnableGoap(_goap.Blackboard, _facade)
            || (_goap.Agent != null && _goap.Agent.HasUnfinishedCommittedBallAction);
        if (wantGoap == _productionGoapActive)
        {
            return;
        }

        _productionGoapActive = wantGoap;
        if (wantGoap)
        {
            _goapConfigured = false;
            TryConfigureGoapPilot();
            _goap.SetActive(true);
            return;
        }

        _goap.SetActive(false);
    }

    private void RefreshEnemyMainNpcGoap()
    {
        if (_facade == null
            || _assignment == null
            || _assignment.Role != AnimalControlRole.EnemyFieldNpc
            || !PassesLocalGoapDebugFilter(_assignment.Role))
        {
            return;
        }

        var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
        if (enemySquad == null || !enemySquad.ShouldUseGoapFor(_facade))
        {
            return;
        }

        if (enemySquad.ResolveNpcTier(_facade) != GoapNpcTier.Main)
        {
            return;
        }

        bool wantGoap = GoapEnemyMainNpcPlanning.ShouldEnableGoap(_goap.Blackboard, _facade);
        if (wantGoap == _enemyMainGoapActive)
        {
            return;
        }

        _enemyMainGoapActive = wantGoap;

        if (wantGoap)
        {
            _goapConfigured = false;
            TryConfigureEnemyGoap();
            _goap.SetActive(true);
            return;
        }

        _goap.SetActive(false);
    }

    public void ResetGoapConfiguration()
    {
        _goapConfigured = false;
    }

    private bool ShouldUseGoapPilot()
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        return squad != null && _facade != null && squad.ShouldUseGoapPilotFor(_facade);
    }

    private void TryConfigureGoapPilot()
    {
        if (_goapConfigured || !_goap.HasAgent)
        {
            return;
        }

        if (_assignment != null && _assignment.Role == AnimalControlRole.EnemyFieldNpc)
        {
            TryConfigureEnemyGoap();
            return;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        if (squad == null)
        {
            return;
        }

        squad.ApplyGoapPilotConfiguration(_goap.Agent, _facade);
        _goapConfigured = true;
    }

    private void TryConfigureEnemyGoap()
    {
        if (_goapConfigured || !_goap.HasAgent)
        {
            return;
        }

        var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
        if (enemySquad == null)
        {
            return;
        }

        enemySquad.ApplyGoapConfiguration(_goap.Agent, _facade);
        _goapConfigured = true;
    }

    private bool PassesLocalGoapDebugFilter(AnimalControlRole role)
    {
        if (!_enableLocalGoapFilter)
        {
            return true;
        }

        bool isAllyRole = role == AnimalControlRole.TeammateNpc || role == AnimalControlRole.Human;
        bool isEnemyRole = role == AnimalControlRole.EnemyFieldNpc;
        if (!isAllyRole && !isEnemyRole)
        {
            return false;
        }

        if (_localGoapDebugSide == LocalGoapDebugSide.AllyOnly && !isAllyRole)
        {
            return false;
        }

        if (_localGoapDebugSide == LocalGoapDebugSide.EnemyOnly && !isEnemyRole)
        {
            return false;
        }

        if (_localGoapAllowedPlayerIds == null || _localGoapAllowedPlayerIds.Count == 0)
        {
            return true;
        }

        int playerId = ResolveCurrentPlayerId();
        return playerId > 0 && _localGoapAllowedPlayerIds.Contains(playerId);
    }

    private int ResolveCurrentPlayerId()
    {
        if (_goap.Blackboard?.BasicData != null && _goap.Blackboard.BasicData.PlayerID > 0)
        {
            return _goap.Blackboard.BasicData.PlayerID;
        }

        var avatar = _facade != null ? _facade.GetAvatar() : null;
        return avatar != null ? avatar.ViewID : -1;
    }
}
