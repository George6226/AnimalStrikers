using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 自チーム（ローカル所有の味方）に対し「人間1 + 味方NPC2 + GK NPC」を割り当てる。
/// Player / Enemy（対人時の相手側）いずれも、ローカルで所有する味方に適用する。
/// </summary>
public class SquadControlController : MonoBehaviour
{
    [Header("段階0デバッグ（頭上ラベル一括）")]
    [SerializeField] private bool _debugOverlayEnabled = true;

    [Header("人間が操作するフィールドスロット（0〜2。3はGK固定）")]
    [SerializeField] private int _humanFormationSlot = 0;

    [Header("GOAP（段階2=1体パイロット / 段階3=味方フィールド全員）")]
    [SerializeField] private bool _enableGoapPilot = true;
    [Tooltip("ON=GOAP非制御時に段階1へフォールバック。OFF=GOAPのみで移動（推奨）。")]
    [SerializeField] private bool _allowGoapIdleFallback;
    [Tooltip("ON=段階3: 味方フィールドNPC全員にGOAP。OFF=段階2: 指定スロット1体のみ")]
    [SerializeField] private bool _goapAllTeammateFieldNpcs = true;
    [Tooltip("段階2のみ: GOAPを試す編成スロット（0=CF, 1=RW, 2=LW）")]
    [SerializeField] private int _goapPilotFormationSlot = 1;
    [SerializeField] private float _goapPlanningInterval = 5f;
    [SerializeField] private List<GoapGoalSO> _goapPilotGoals = new List<GoapGoalSO>();
    [SerializeField] private List<GoapActionSO> _goapPilotActions = new List<GoapActionSO>();

    [Header("Phase M0: メイン NPC GOAP 検証")]
    [Tooltip("ON=Human操作を止め slot0 をメインNPC GOAP、slot1/2 をサブNPC GOAP として検証する")]
    [SerializeField] private bool _mainNpcGoapVerifyMode;
    [Tooltip("メイン NPC として GOAP を動かす編成スロット（検証中は 0=CF 固定推奨）")]
    [SerializeField] private int _mainNpcFormationSlot;
    [SerializeField] private List<GoapGoalSO> _goapMainNpcGoals = new List<GoapGoalSO>();
    [SerializeField] private List<GoapActionSO> _goapMainNpcActions = new List<GoapActionSO>();

    [Header("Phase M2: メイン NPC GOAP 本番")]
    [Tooltip("ON=操作キャラがボール非保持時に M2（パス後サポート・ルーズボール）GOAP を動かす")]
    [SerializeField] private bool _enableMainNpcGoapInProduction = true;

    [Header("段階4 役割分担（2体が重ならない）")]
    [SerializeField] private bool _enableStage4RoleDifferentiation = true;

    [Header("段階2/3/4 検証モニタ")]
    [SerializeField] private bool _enableStage2ValidationLog = false;
    [SerializeField] private float _validationStartDelay = 2.0f;
    [Tooltip("段階4: フィールドNPC同士の最小距離がこの値以上になったらCheck7成功")]
    [SerializeField] private float _stage4MinSeparationMeters = 3f;

    private readonly List<AnimalFacade> _pendingLocalAllies = new();
    private readonly HashSet<AnimalFacade> _goapConfiguredFacades = new();
    private AnimalFacade _fixedGoapPilotFacade;
    private float _validationStartTime;
    private bool _validatedMinimalSetup;
    private bool _validatedRoleToGoap;
    private bool _validatedLabelObservation;
    private bool _validatedFallback;
    private bool _validatedStage3AllNpcsGoap;
    private bool _validatedStage4RoleSeparation;
    private float _observedMinNpcSeparation = float.MaxValue;
    private float _lastValidationHeartbeatTime;
    private bool _printedValidationBootLog;

    public int HumanFormationSlot => _humanFormationSlot;
    public int GoapPilotFormationSlot => _goapPilotFormationSlot;
    public bool MainNpcGoapVerifyModeActive => _mainNpcGoapVerifyMode;
    public int MainNpcFormationSlot => _mainNpcFormationSlot;
    public bool MainNpcGoapProductionEnabled => _enableMainNpcGoapInProduction;

#if UNITY_EDITOR
    private const string DefensiveGoalAssetPath = "Assets/Scripts/Game/Goap/Goals/Goals/DefensivePositioningGoalSO.asset";
    private const string BallPossessionAttackGoalAssetPath =
        "Assets/Scripts/Game/Goap/Goals/Goals/BallPossessionAttackGoalSO.asset";
    private const string TeamBallSupportGoalAssetPath =
        "Assets/Scripts/Game/Goap/Goals/Goals/TeamBallSupportGoalSO.asset";
    private const string MarkOpponentActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/MarkOpponentActionSO.asset";
    private const string PassToTeammateActionAssetPath =
        "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/PassToTeammateActionSO.asset";
    private const string ShootAtGoalActionAssetPath =
        "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/ShootAtGoalActionSO.asset";
    private const string BlockPassLaneActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/BlockPassLaneActionSO.asset";
    private const string BlockShotLaneActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/BlockShotLaneActionSO.asset";
    private const string GetOpenActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/GetOpenActionSO.asset";
    private const string CreateSupportAngleActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/CreateSupportAngleActionSO.asset";
    private const string MakeRunBehindActionAssetPath = "Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/MakeRunBehindActionSO.asset";

    private void Reset()
    {
        EnsurePilotAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
    }
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        EnsurePilotAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
#endif
        SyncMainNpcVerifyEnvironment();
        SyncMainNpcProductionEnvironment();
        AnimalDebugOverlay.Enabled = _debugOverlayEnabled;
        TeammateNpcGoapRoleDifferentiation.Enabled = _enableStage4RoleDifferentiation;
        TeammateNpcMovementBrain.GlobalAllowGoapIdleFallback = _allowGoapIdleFallback;
        _validationStartTime = Time.time;
        _lastValidationHeartbeatTime = _validationStartTime;
        _validatedMinimalSetup = false;
        _validatedRoleToGoap = false;
        _validatedLabelObservation = false;
        _validatedFallback = false;
        _validatedStage3AllNpcsGoap = false;
        _validatedStage4RoleSeparation = false;
        _observedMinNpcSeparation = float.MaxValue;
        _printedValidationBootLog = false;
    }

    private void Update()
    {
        if (!_printedValidationBootLog)
        {
            _printedValidationBootLog = true;
            Debug.Log($"[Stage2/Monitor] started enabled={_enableStage2ValidationLog} delay={_validationStartDelay:F1}s " +
                      $"stage3AllNpcs={_goapAllTeammateFieldNpcs} stage4Roles={_enableStage4RoleDifferentiation} " +
                      $"pilotSlot={_goapPilotFormationSlot} mainNpcVerify={_mainNpcGoapVerifyMode} mainSlot={_mainNpcFormationSlot}");
        }

        if (!_enableStage2ValidationLog)
        {
            if (Time.time - _lastValidationHeartbeatTime > 5f)
            {
                _lastValidationHeartbeatTime = Time.time;
                Debug.LogWarning("[Stage2/Monitor] validation log is disabled. Enable 'Enable Stage2 Validation Log' to run checks.");
            }
            return;
        }

        if (Time.time - _validationStartTime < _validationStartDelay)
        {
            return;
        }

        ValidateStage2Progress();
        ValidateStage3Progress();
        ValidateStage4Progress();

        if (Time.time - _lastValidationHeartbeatTime > 5f)
        {
            _lastValidationHeartbeatTime = Time.time;
            PrintValidationHeartbeat();
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        EnsurePilotAssetsAssigned();
        EnsureMainNpcAssetsAssigned();
#endif
        SyncMainNpcVerifyEnvironment();
        SyncMainNpcProductionEnvironment();
        AnimalDebugOverlay.Enabled = _debugOverlayEnabled;
        TeammateNpcGoapRoleDifferentiation.Enabled = _enableStage4RoleDifferentiation;
        TeammateNpcMovementBrain.GlobalAllowGoapIdleFallback = _allowGoapIdleFallback;
    }

    /// <summary>段階0デバッグ表示のON/OFF（実行中も可）。</summary>
    public void SetDebugOverlayEnabled(bool enabled)
    {
        _debugOverlayEnabled = enabled;
        AnimalDebugOverlay.Enabled = enabled;
    }

    /// <summary>ローカル味方の登録。AnimalTeamRegistrar から呼ぶ。</summary>
    public void OnLocalAllyRegistered(AnimalFacade facade)
    {
        if (facade == null || !IsLocalAlly(facade))
        {
            return;
        }

        EnsureControlComponents(facade);

        if (!_pendingLocalAllies.Contains(facade))
        {
            _pendingLocalAllies.Add(facade);
        }

        TryAssignFixedGoapPilot(facade);
        RefreshLocalSquadRoles();
        TryConfigureGoapPilot(facade);
    }

    public void OnLocalAllyUnregistered(AnimalFacade facade)
    {
        _pendingLocalAllies.Remove(facade);
        _goapConfiguredFacades.Remove(facade);
        if (_fixedGoapPilotFacade == facade)
        {
            _fixedGoapPilotFacade = null;
            RebuildFixedGoapPilot();
        }
    }

    /// <summary>この味方フィールドプレイヤーに GOAP を使うか（段階3=全員 / 段階2=1スロットのみ）。</summary>
    public bool ShouldUseGoapFor(AnimalFacade facade)
    {
        if (!_enableGoapPilot || facade == null || facade.IsGK())
        {
            return false;
        }

        if (!_mainNpcGoapVerifyMode && (_goapPilotGoals == null || _goapPilotGoals.Count == 0))
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            return false;
        }

        if (GoapBatchVerifyEnvironment.IsActive)
        {
            return assignment.Role == AnimalControlRole.TeammateNpc
                || assignment.Role == AnimalControlRole.Human;
        }

        if (GoapMainNpcVerifyEnvironment.IsActive)
        {
            return assignment.Role == AnimalControlRole.TeammateNpc;
        }

        if (GoapMainNpcProductionEnvironment.IsActive
            && assignment.Role == AnimalControlRole.Human)
        {
            return true;
        }

        if (assignment.Role != AnimalControlRole.TeammateNpc)
        {
            return false;
        }

        if (_goapAllTeammateFieldNpcs)
        {
            return true;
        }

        if (_fixedGoapPilotFacade == null)
        {
            RebuildFixedGoapPilot();
        }

        return _fixedGoapPilotFacade == facade;
    }

    /// <summary>段階2互換: 指定スロット1体のみ。</summary>
    public bool ShouldUseGoapPilotFor(AnimalFacade facade) => ShouldUseGoapFor(facade);

    /// <summary>段階2: GoapAgent にゴール・アクションを注入（1回だけ）。</summary>
    public void ApplyGoapPilotConfiguration(GoapAgent agent, AnimalFacade facade = null)
    {
        if (agent == null)
        {
            return;
        }

        GoapNpcTier tier = facade != null ? ResolveNpcTier(facade) : GoapNpcTier.Sub;
        IReadOnlyList<GoapGoalSO> goals = tier == GoapNpcTier.Main ? _goapMainNpcGoals : _goapPilotGoals;
        IReadOnlyList<GoapActionSO> actions = tier == GoapNpcTier.Main ? _goapMainNpcActions : _goapPilotActions;

        if (!_mainNpcGoapVerifyMode && tier == GoapNpcTier.Sub
            && (goals == null || goals.Count == 0))
        {
            return;
        }

        agent.ConfigurePilot(
            goals ?? new List<GoapGoalSO>(),
            actions ?? new List<GoapActionSO>(),
            _goapPlanningInterval,
            tier);
    }

    public GoapNpcTier ResolveNpcTier(AnimalFacade facade)
    {
        if (GoapMainNpcVerifyEnvironment.IsActive)
        {
            return GoapMainNpcVerifyEnvironment.ResolveTier(facade);
        }

        if (GoapMainNpcProductionEnvironment.IsActive)
        {
            return GoapMainNpcProductionEnvironment.ResolveTier(facade);
        }

        return GoapNpcTier.Sub;
    }

    public AnimalFacade GetGoapPilotFacade()
    {
        if (_fixedGoapPilotFacade == null)
        {
            RebuildFixedGoapPilot();
        }

        return _fixedGoapPilotFacade;
    }

    private void ValidateStage2Progress()
    {
        AnimalFacade pilot = GetGoapPilotFacade();
        AnimalGoapBrainComponents goap = AnimalGoapBrainComponents.Resolve(pilot);
        AnimalControlAssignment assignment = pilot != null ? pilot.GetComponent<AnimalControlAssignment>() : null;
        AnimalControlRoleDebugLabel label = pilot != null ? pilot.GetComponent<AnimalControlRoleDebugLabel>() : null;
        TeammateNpcMovementBrain movement = pilot != null ? pilot.GetComponent<TeammateNpcMovementBrain>() : null;

        if (!_validatedMinimalSetup && ValidateMinimalSetup())
        {
            _validatedMinimalSetup = true;
            Debug.Log("[Stage2/Check2] OK: GOAP最小構成（pilot slot/goals/actions/planning）が設定済み。");
        }

        if (!_validatedRoleToGoap && ValidateRoleToGoapConnectivity(assignment, goap.Agent))
        {
            _validatedRoleToGoap = true;
            Debug.Log("[Stage2/Check3] OK: TeammateNpc ロール時に GOAP が有効化されている。");
        }

        if (!_validatedLabelObservation && ValidateLabelObservation(label))
        {
            _validatedLabelObservation = true;
            Debug.Log($"[Stage2/Check4] OK: ラベル観測できた -> {label.LastRenderedText.Replace('\n', ' ')}");
        }

        if (!_validatedFallback && _allowGoapIdleFallback && ValidateFallback(movement, goap.Agent))
        {
            _validatedFallback = true;
            Debug.Log("[Stage2/Check5] OK: GOAP非制御時の段階1フォールバック移動が動作。");
        }
        else if (!_validatedFallback && !_allowGoapIdleFallback)
        {
            _validatedFallback = true;
            Debug.Log("[Stage2/Check5] SKIP: GOAPアイドル時フォールバックは無効（Allow Goap Idle Fallback=OFF）。");
        }
    }

    private void ValidateStage3Progress()
    {
        if (!_goapAllTeammateFieldNpcs || !_enableGoapPilot)
        {
            return;
        }

        if (_validatedStage3AllNpcsGoap)
        {
            return;
        }

        int npcCount = 0;
        int goapEnabledCount = 0;
        foreach (var facade in GetTeammateNpcFieldPlayers())
        {
            if (facade == null)
            {
                continue;
            }

            npcCount++;
            var assignment = facade.GetComponent<AnimalControlAssignment>();
            var goap = AnimalGoapBrainComponents.Resolve(facade).Agent;
            if (assignment != null && assignment.Role == AnimalControlRole.TeammateNpc
                && goap != null && goap.enabled)
            {
                goapEnabledCount++;
            }
        }

        if (npcCount >= 2 && goapEnabledCount >= npcCount)
        {
            _validatedStage3AllNpcsGoap = true;
            Debug.Log($"[Stage3/Check6] OK: 味方フィールドNPC全員にGOAP有効 (npc={npcCount}, goapEnabled={goapEnabledCount}).");
        }
    }

    private void ValidateStage4Progress()
    {
        if (!_enableStage4RoleDifferentiation || !_goapAllTeammateFieldNpcs)
        {
            return;
        }

        float sep = TeammateNpcGoapRoleDifferentiation.MeasureNpcFieldSeparation();
        if (sep > 0f && sep < _observedMinNpcSeparation)
        {
            _observedMinNpcSeparation = sep;
        }

        if (_validatedStage4RoleSeparation)
        {
            return;
        }

        if (_observedMinNpcSeparation >= _stage4MinSeparationMeters)
        {
            _validatedStage4RoleSeparation = true;
            Debug.Log($"[Stage4/Check7] OK: フィールドNPCの最小間隔が確保 (minSep={_observedMinNpcSeparation:F2}m, threshold={_stage4MinSeparationMeters:F1}m).");
        }
    }

    private bool ValidateMinimalSetup()
    {
        if (!_enableGoapPilot)
        {
            return false;
        }

        if (_goapPilotFormationSlot < 0 || _goapPilotFormationSlot > 2)
        {
            return false;
        }

        if (_goapPlanningInterval < 0.5f)
        {
            return false;
        }

        if (_goapPilotGoals == null || _goapPilotGoals.Count == 0)
        {
            return false;
        }

        if (_goapPilotActions == null || _goapPilotActions.Count == 0)
        {
            return false;
        }

        return _goapPilotGoals.All(g => g != null) && _goapPilotActions.All(a => a != null);
    }

    private static bool ValidateRoleToGoapConnectivity(AnimalControlAssignment assignment, GoapAgent agent)
    {
        if (assignment == null || agent == null)
        {
            return false;
        }

        return assignment.Role == AnimalControlRole.TeammateNpc && agent.enabled;
    }

    private static bool ValidateLabelObservation(AnimalControlRoleDebugLabel label)
    {
        if (label == null || !label.IsLabelVisible)
        {
            return false;
        }

        string t = label.LastRenderedText;
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        return t.Contains("›") || t.Contains("Planning") || t.Contains("Queued") || t.Contains("FB:");
    }

    private static bool ValidateFallback(TeammateNpcMovementBrain movement, GoapAgent agent)
    {
        if (movement == null || agent == null)
        {
            return false;
        }

        return agent.enabled && !agent.IsActivelyControlling && movement.IsFallbackDriving;
    }

    private void PrintValidationHeartbeat()
    {
        AnimalFacade pilot = GetGoapPilotFacade();
        AnimalGoapBrainComponents goap = AnimalGoapBrainComponents.Resolve(pilot);
        AnimalControlAssignment assignment = pilot != null ? pilot.GetComponent<AnimalControlAssignment>() : null;
        AnimalControlRoleDebugLabel label = pilot != null ? pilot.GetComponent<AnimalControlRoleDebugLabel>() : null;
        TeammateNpcMovementBrain movement = pilot != null ? pilot.GetComponent<TeammateNpcMovementBrain>() : null;

        string pilotName = pilot != null ? pilot.name : "null";
        string roleText = assignment != null ? assignment.Role.ToString() : "null";
        string goapText = goap.Agent != null ? $"enabled={goap.Agent.enabled}, active={goap.Agent.IsActivelyControlling}" : "null";
        string labelText = label != null ? label.LastRenderedText.Replace('\n', ' ') : "null";
        string fallbackText = movement != null ? movement.IsFallbackDriving.ToString() : "null";
        int goalCount = _goapPilotGoals != null ? _goapPilotGoals.Count : 0;
        int actionCount = _goapPilotActions != null ? _goapPilotActions.Count : 0;

        int npcFieldCount = 0;
        int npcGoapCount = 0;
        foreach (var f in GetTeammateNpcFieldPlayers())
        {
            if (f == null) continue;
            npcFieldCount++;
            var g = AnimalGoapBrainComponents.Resolve(f).Agent;
            if (g != null && g.enabled) npcGoapCount++;
        }

        Debug.Log(
            $"[Stage2/Monitor] progress setup={_validatedMinimalSetup} roleToGoap={_validatedRoleToGoap} " +
            $"label={_validatedLabelObservation} fallback={_validatedFallback} stage3AllGoap={_validatedStage3AllNpcsGoap} " +
            $"stage4Sep={_validatedStage4RoleSeparation} minNpcDist={(_observedMinNpcSeparation < float.MaxValue ? _observedMinNpcSeparation.ToString("F1") : "-")} " +
            $"goals={goalCount} actions={actionCount} mode={( _goapAllTeammateFieldNpcs ? (_enableStage4RoleDifferentiation ? "Stage4" : "Stage3") : "Stage2")} " +
            $"npcField={npcFieldCount} npcGoapEnabled={npcGoapCount} " +
            $"pilot={pilotName} role={roleText} goap({goapText}) labelText='{labelText}' fallbackDriving={fallbackText}");
    }

#if UNITY_EDITOR
    private void EnsurePilotAssetsAssigned()
    {
        if (_goapPilotGoals == null)
        {
            _goapPilotGoals = new List<GoapGoalSO>();
        }

        if (_goapPilotActions == null)
        {
            _goapPilotActions = new List<GoapActionSO>();
        }

        _goapPilotGoals.RemoveAll(g => g == null);
        _goapPilotActions.RemoveAll(a => a == null);

        if (_goapPilotGoals.Count == 0)
        {
            AddIfNotNull(_goapPilotGoals, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(DefensiveGoalAssetPath));
        }

        if (_goapPilotActions.Count == 0)
        {
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(MarkOpponentActionAssetPath));
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(BlockPassLaneActionAssetPath));
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(BlockShotLaneActionAssetPath));
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(GetOpenActionAssetPath));
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(CreateSupportAngleActionAssetPath));
            AddIfNotNull(_goapPilotActions, UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(MakeRunBehindActionAssetPath));
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
            AddIfNotNull(
                _goapMainNpcGoals,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(BallPossessionAttackGoalAssetPath));
            AddIfNotNull(
                _goapMainNpcGoals,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapGoalSO>(TeamBallSupportGoalAssetPath));
        }

        if (_goapMainNpcActions.Count == 0)
        {
            AddIfNotNull(
                _goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(PassToTeammateActionAssetPath));
            AddIfNotNull(
                _goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(ShootAtGoalActionAssetPath));
            AddIfNotNull(
                _goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(GetOpenActionAssetPath));
            AddIfNotNull(
                _goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(CreateSupportAngleActionAssetPath));
            AddIfNotNull(
                _goapMainNpcActions,
                UnityEditor.AssetDatabase.LoadAssetAtPath<GoapActionSO>(MakeRunBehindActionAssetPath));
        }
    }

    private static void AddIfNotNull<T>(List<T> list, T value) where T : class
    {
        if (value != null && !list.Contains(value))
        {
            list.Add(value);
        }
    }
#endif

    /// <summary>人間操作対象のフィールドプレイヤー一覧（通常1体）。</summary>
    public IEnumerable<AnimalFacade> GetHumanControllableFieldPlayers()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            yield break;
        }

        foreach (var facade in regist.Allys)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.IsHumanControlled)
            {
                yield return facade;
            }
        }
    }

    /// <summary>味方NPCフィールドプレイヤー（2体想定）。</summary>
    public IEnumerable<AnimalFacade> GetTeammateNpcFieldPlayers()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            yield break;
        }

        foreach (var facade in regist.Allys)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.IsTeammateNpc)
            {
                yield return facade;
            }
        }
    }

    /// <summary>味方GK（NPC操作）。</summary>
    public AnimalFacade GetGoalkeeperNpc()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        foreach (var facade in regist.Allys)
        {
            if (facade == null || !facade.IsGK())
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.IsGoalkeeperNpc)
            {
                return facade;
            }
        }

        return regist.Allys.FirstOrDefault(f => f != null && f.IsGK());
    }

    /// <summary>
    /// 現在プレイヤーが操作するフィールドプレイヤーに合わせてロールを更新する。
    /// AnimalSelector がキャラ切り替えしたときに呼ぶ（ラベル・NPC移動の同期用）。
    /// </summary>
    public void SetActiveHumanPlayer(AnimalFacade activeHuman)
    {
        if (GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive)
        {
            return;
        }

        if (activeHuman != null && (activeHuman.IsGK() || !IsLocalAlly(activeHuman)))
        {
            return;
        }

        foreach (var facade in _pendingLocalAllies.ToList())
        {
            if (facade == null)
            {
                _pendingLocalAllies.Remove(facade);
                continue;
            }

            if (facade.IsGK())
            {
                ApplyRole(facade, AnimalControlRole.GoalkeeperNpc);
                continue;
            }

            AnimalControlRole role = facade == activeHuman
                ? AnimalControlRole.Human
                : AnimalControlRole.TeammateNpc;
            ApplyRole(facade, role);
            TryConfigureGoapPilot(facade);
        }
    }

    /// <summary>操作キャラを別スロットに切り替える（将来のUI用）。</summary>
    public void SetHumanFormationSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2)
        {
            Debug.LogWarning($"[SquadControlController] 無効なスロット: {slotIndex}");
            return;
        }

        _humanFormationSlot = slotIndex;
        SyncMainNpcProductionEnvironment();
        RefreshLocalSquadRoles();
    }

    /// <summary>GOAP バッチ検証中: AnimalSelector の Human 割当を抑止し全フィールド味方を TeammateNpc に固定する。</summary>
    public void RefreshLocalSquadRolesForBatchVerification()
    {
        if (!GoapBatchVerifyEnvironment.IsActive)
        {
            return;
        }

        RefreshLocalSquadRoles();
    }

    /// <summary>Phase M0: Human 操作を止め slot0=Main / slot1-2=Sub GOAP 検証ロールへ切り替える。</summary>
    public void RefreshLocalSquadRolesForMainNpcVerify()
    {
        if (!_mainNpcGoapVerifyMode)
        {
            return;
        }

        ResetGoapPilotConfigurations();
        RefreshLocalSquadRoles();
    }

    private void SyncMainNpcVerifyEnvironment()
    {
        bool mainNpcVerifyActive = _mainNpcGoapVerifyMode && !GoapBatchVerifyEnvironment.IsActive;
        GoapMainNpcVerifyEnvironment.Sync(mainNpcVerifyActive, _mainNpcFormationSlot);
        if (mainNpcVerifyActive)
        {
            GoapMainNpcProductionEnvironment.Sync(false);
        }

        if (mainNpcVerifyActive && Application.isPlaying)
        {
            RefreshLocalSquadRolesForMainNpcVerify();
        }
    }

    private void SyncMainNpcProductionEnvironment()
    {
        if (_mainNpcGoapVerifyMode || GoapBatchVerifyEnvironment.IsActive)
        {
            GoapMainNpcProductionEnvironment.Sync(false);
            return;
        }

        GoapMainNpcProductionEnvironment.Sync(_enableMainNpcGoapInProduction);
    }

    private void ResetGoapPilotConfigurations()
    {
        _goapConfiguredFacades.Clear();
        foreach (var facade in _pendingLocalAllies)
        {
            if (facade == null)
            {
                continue;
            }

            var router = facade.GetComponent<AnimalControlBrainRouter>();
            router?.ResetGoapConfiguration();
        }
    }

    private void RefreshLocalSquadRoles()
    {
        foreach (var facade in _pendingLocalAllies.ToList())
        {
            if (facade == null)
            {
                _pendingLocalAllies.Remove(facade);
                continue;
            }

            AnimalControlRole role = ShouldForceTeammateNpcForGoapVerify(facade)
                ? AnimalControlRole.TeammateNpc
                : ResolveRole(facade);
            ApplyRole(facade, role);
            TryConfigureGoapPilot(facade);
        }
    }

    private static bool ShouldForceTeammateNpcForGoapVerify(AnimalFacade facade)
    {
        if (facade == null || facade.IsGK())
        {
            return false;
        }

        return GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive;
    }

    private AnimalControlRole ResolveRole(AnimalFacade facade)
    {
        if (facade.IsGK())
        {
            return AnimalControlRole.GoalkeeperNpc;
        }

        int slot = GetFormationSlot(facade);
        return slot == _humanFormationSlot
            ? AnimalControlRole.Human
            : AnimalControlRole.TeammateNpc;
    }

    private void TryConfigureGoapPilot(AnimalFacade facade)
    {
        if (!ShouldUseGoapFor(facade) || _goapConfiguredFacades.Contains(facade))
        {
            return;
        }

        var goap = AnimalGoapBrainComponents.Resolve(facade).Agent;
        if (goap == null)
        {
            return;
        }

        ApplyGoapPilotConfiguration(goap, facade);
        _goapConfiguredFacades.Add(facade);
    }

    private void TryAssignFixedGoapPilot(AnimalFacade facade)
    {
        if (_fixedGoapPilotFacade != null || facade == null || facade.IsGK())
        {
            return;
        }

        if (GetFormationSlot(facade) == _goapPilotFormationSlot)
        {
            _fixedGoapPilotFacade = facade;
        }
    }

    private void RebuildFixedGoapPilot()
    {
        _fixedGoapPilotFacade = _pendingLocalAllies
            .FirstOrDefault(f => f != null && !f.IsGK() && GetFormationSlot(f) == _goapPilotFormationSlot);
    }

    private static int GetFormationSlot(AnimalFacade facade)
    {
        var formationSlot = facade.GetComponent<AnimalFormationSlot>();
        return formationSlot != null && formationSlot.IsAssigned
            ? formationSlot.Index
            : -1;
    }

    private static void ApplyRole(AnimalFacade facade, AnimalControlRole role)
    {
        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            return;
        }

        assignment.SetRole(role);

        var router = facade.GetComponent<AnimalControlBrainRouter>();
        if (router != null)
        {
            router.ApplyRole(role);
        }
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

    private static bool IsLocalAlly(AnimalFacade facade)
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

        return tag == ConstData.PLAYER_TAG;
    }
}
