using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 敵チーム（ローカル所有 NPC）に Main×1 + Sub×2 + GK を割り当て、GOAP を注入する。
/// </summary>
public class EnemySquadControlController : MonoBehaviour
{
    [Header("Phase B: 敵フィールド GOAP")]
    [SerializeField] private bool _enableEnemyGoap = true;
    [SerializeField] private bool _goapAllEnemyFieldNpcs = true;
    [SerializeField] private int _enemyMainFormationSlot;
    [SerializeField] private float _goapPlanningInterval = 5f;
    [SerializeField] private List<GoapGoalSO> _goapSubNpcGoals = new List<GoapGoalSO>();
    [SerializeField] private List<GoapActionSO> _goapSubNpcActions = new List<GoapActionSO>();
    [SerializeField] private List<GoapGoalSO> _goapMainNpcGoals = new List<GoapGoalSO>();
    [SerializeField] private List<GoapActionSO> _goapMainNpcActions = new List<GoapActionSO>();

    private readonly List<AnimalFacade> _pendingLocalEnemies = new();
    private readonly HashSet<AnimalFacade> _goapConfiguredFacades = new();

    public int EnemyMainFormationSlot => _enemyMainFormationSlot;
    public bool EnemyGoapEnabled => _enableEnemyGoap;

#if UNITY_EDITOR
    private const string DefensiveGoalAssetPath = "Assets/Scripts/Game/Goap/Goals/Goals/DefensivePositioningGoalSO.asset";
    private const string BallPossessionAttackGoalAssetPath =
        "Assets/Scripts/Game/Goap/Goals/Goals/BallPossessionAttackGoalSO.asset";
    private const string TeamBallSupportGoalAssetPath =
        "Assets/Scripts/Game/Goap/Goals/Goals/TeamBallSupportGoalSO.asset";
    private const string MarkOpponentActionAssetPath =
        "Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/MarkOpponentActionSO.asset";
    private const string PassToTeammateActionAssetPath =
        "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/PassToTeammateActionSO.asset";
    private const string ShootAtGoalActionAssetPath =
        "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/ShootAtGoalActionSO.asset";

    private void Reset()
    {
        EnsureSubNpcAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
    }
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        EnsureSubNpcAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
#endif
    }

    public void OnLocalEnemyRegistered(AnimalFacade facade)
    {
        if (facade == null || !IsLocalEnemy(facade))
        {
            return;
        }

        EnsureControlComponents(facade);

        if (!_pendingLocalEnemies.Contains(facade))
        {
            _pendingLocalEnemies.Add(facade);
        }

        RefreshEnemySquadRoles();
        TryConfigureGoap(facade);
    }

    public void OnLocalEnemyUnregistered(AnimalFacade facade)
    {
        _pendingLocalEnemies.Remove(facade);
        _goapConfiguredFacades.Remove(facade);
    }

    public bool ShouldUseGoapFor(AnimalFacade facade)
    {
        if (!_enableEnemyGoap || facade == null || facade.IsGK())
        {
            return false;
        }

        if (_goapSubNpcGoals == null || _goapSubNpcGoals.Count == 0)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null || assignment.Role != AnimalControlRole.EnemyFieldNpc)
        {
            return false;
        }

        return _goapAllEnemyFieldNpcs;
    }

    public GoapNpcTier ResolveNpcTier(AnimalFacade facade)
    {
        if (facade == null)
        {
            return GoapNpcTier.Sub;
        }

        return GetFormationSlot(facade) == _enemyMainFormationSlot
            ? GoapNpcTier.Main
            : GoapNpcTier.Sub;
    }

    public void ApplyGoapConfiguration(GoapAgent agent, AnimalFacade facade = null)
    {
        if (agent == null)
        {
            return;
        }

        GoapNpcTier tier = facade != null ? ResolveNpcTier(facade) : GoapNpcTier.Sub;
        IReadOnlyList<GoapGoalSO> goals = tier == GoapNpcTier.Main ? _goapMainNpcGoals : _goapSubNpcGoals;
        IReadOnlyList<GoapActionSO> actions = tier == GoapNpcTier.Main ? _goapMainNpcActions : _goapSubNpcActions;

        if (tier == GoapNpcTier.Sub && (goals == null || goals.Count == 0))
        {
            return;
        }

        agent.ConfigurePilot(
            goals ?? new List<GoapGoalSO>(),
            actions ?? new List<GoapActionSO>(),
            _goapPlanningInterval,
            tier);
    }

    private void RefreshEnemySquadRoles()
    {
        foreach (var facade in _pendingLocalEnemies.ToList())
        {
            if (facade == null)
            {
                _pendingLocalEnemies.Remove(facade);
                continue;
            }

            AnimalControlRole role = facade.IsGK()
                ? AnimalControlRole.GoalkeeperNpc
                : AnimalControlRole.EnemyFieldNpc;
            ApplyRole(facade, role);
            TryConfigureGoap(facade);
        }
    }

    private void TryConfigureGoap(AnimalFacade facade)
    {
        if (!ShouldUseGoapFor(facade) || _goapConfiguredFacades.Contains(facade))
        {
            return;
        }

        var agent = AnimalGoapBrainComponents.Resolve(facade).Agent;
        if (agent == null)
        {
            return;
        }

        ApplyGoapConfiguration(agent, facade);
        _goapConfiguredFacades.Add(facade);
    }

    private static void ApplyRole(AnimalFacade facade, AnimalControlRole role)
    {
        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            assignment = facade.gameObject.AddComponent<AnimalControlAssignment>();
        }

        assignment.SetRole(role);

        var router = facade.GetComponent<AnimalControlBrainRouter>();
        router?.ApplyRole(role);
    }

    private static void EnsureControlComponents(AnimalFacade facade)
    {
        if (facade.GetComponent<AnimalControlAssignment>() == null)
        {
            facade.gameObject.AddComponent<AnimalControlAssignment>();
        }

        if (facade.GetComponent<AnimalControlBrainRouter>() == null)
        {
            facade.gameObject.AddComponent<AnimalControlBrainRouter>();
        }

        if (facade.IsGK() && facade.GetComponent<GoalkeeperNpcBrain>() == null)
        {
            facade.gameObject.AddComponent<GoalkeeperNpcBrain>();
        }

        if (!facade.IsGK() && facade.GetComponent<TeammateNpcMovementBrain>() == null)
        {
            facade.gameObject.AddComponent<TeammateNpcMovementBrain>();
        }

        if (facade.GetComponent<AnimalControlRoleDebugLabel>() == null)
        {
            facade.gameObject.AddComponent<AnimalControlRoleDebugLabel>();
        }
    }

    private static bool IsLocalEnemy(AnimalFacade facade)
    {
        var avatar = facade.GetAvatar();
        if (avatar == null || !avatar.IsMine)
        {
            return false;
        }

        string tag = avatar.CurrentTag;
        if (string.IsNullOrEmpty(tag))
        {
            tag = avatar.tag;
        }

        return tag == ConstData.NPC_TAG || tag == ConstData.ENEMY_TAG;
    }

    private static int GetFormationSlot(AnimalFacade facade)
    {
        var slot = facade.GetComponent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned ? slot.Index : -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSubNpcAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
    }

    private void EnsureSubNpcAssetsAssigned()
    {
        if (_goapSubNpcGoals == null)
        {
            _goapSubNpcGoals = new List<GoapGoalSO>();
        }

        if (_goapSubNpcActions == null)
        {
            _goapSubNpcActions = new List<GoapActionSO>();
        }

        _goapSubNpcGoals.RemoveAll(g => g == null);
        _goapSubNpcActions.RemoveAll(a => a == null);

        if (_goapSubNpcGoals.Count == 0)
        {
            AddIfNotNull(_goapSubNpcGoals, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(DefensiveGoalAssetPath));
            AddIfNotNull(_goapSubNpcGoals, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(TeamBallSupportGoalAssetPath));
        }
    }

    private void EnsureMainNpcAssetsAssigned()
    {
        if (_goapMainNpcGoals == null)
        {
            _goapMainNpcGoals = new List<GoapGoalSO>();
        }

        if (_goapMainNpcActions == null)
        {
            _goapMainNpcActions = new List<GoapActionSO>();
        }

        _goapMainNpcGoals.RemoveAll(g => g == null);
        _goapMainNpcActions.RemoveAll(a => a == null);

        if (_goapMainNpcGoals.Count == 0)
        {
            AddIfNotNull(_goapMainNpcGoals,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(BallPossessionAttackGoalAssetPath));
            AddIfNotNull(_goapMainNpcGoals,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(TeamBallSupportGoalAssetPath));
        }

        if (_goapMainNpcActions.Count == 0)
        {
            AddIfNotNull(_goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(PassToTeammateActionAssetPath));
            AddIfNotNull(_goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(ShootAtGoalActionAssetPath));
            AddIfNotNull(_goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(MarkOpponentActionAssetPath));
        }
    }

    private static void AddIfNotNull<T>(List<T> list, T item) where T : Object
    {
        if (item != null && !list.Contains(item))
        {
            list.Add(item);
        }
    }
#endif
}
