using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// TeamBallSupport サポートアクション（CSA / MoveToSupport / GetOpen）共通検証基盤。
/// 味方フィールドプレイヤーの初期配置をパターン適用し、他サポートアクションを無効化する。
///
/// 手順:
/// 1. Inspector「検証パターン」で初期配置を選択（Custom は座標を直接編集）
/// 2. Play 開始でパターン適用＋ボール付与
/// 3. 必要なら F1〜F8 でパターン切替（_enablePatternHotkeys=ON）
/// 4. GoapDiag / GoapSummary で SupportAngle ログを確認
///
/// ドライブ追従検証（_verifyRuntimeFollowDuringBatch=ON）:
/// 本番候補のまま #17/#18 でホールド＋自動ドライブ後、Retarget 追従を RuntimePass で判定。
///
/// 一括検証（_runBatchVerificationOnStart=ON）:
/// Play 1回で指定範囲のパターンを順に再配置・観測する。
/// 番号 = enum 値（#0=Baseline, #1=Custom, #2=Clustered … #3 から連続して検証可能）。
///
/// 保持者自動ドライブ（_enableBallOwnerAutoDrive=ON）:
/// ホールド観測中にボール保持者を前後/横へ動かし、翼の追従（FollowDrift / Retarget）を検証する。
///
/// 単体検証（verificationOnly=ON）:
/// GoapDiag のランタイムログで ActionUnderTest の合格を自動判定する（RuntimePassCriteria）。
///
/// 本番選出検証（_verifyProductionSelection=ON）:
/// 候補を本番どおりに戻し、パターン適用直後の最初の PlanCosts で選出を検証する。
///
/// ドライブ中本番選出（_verifyProductionSelectionDuringDrive=ON）:
/// 自動ドライブ観測後の最新 PlanCosts で選出を検証（#13〜#18。RuntimePass と併用可）。
/// </summary>
public abstract class GoapSupportActionVerificationSetup : MonoBehaviour
{
    private const float ProductionSelectionPlanCostsTimeoutSeconds = 15f;
    protected abstract string SummaryLogTag { get; }
    protected abstract GoapSupportActionUnderTest ActionUnderTest { get; }
    protected abstract IGoapProductionSelectionExpectation ProductionSelectionExpectation { get; }
    protected abstract string BatchVerificationBanner { get; }
    protected abstract string ProductionSelectionVerificationBanner { get; }

    protected virtual IGoapSupportActionRuntimePassCriteria RuntimePassCriteria => null;

    /// <summary>ドライブ中本番選出の期待表（既定は #13〜#18）。</summary>
    protected virtual IGoapProductionSelectionExpectation DriveProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.Drive;

    /// <summary>味方配置適用後の付随セットアップ（敵配置など）。</summary>
    protected virtual void ApplyCompanionVerificationState(GoapSupportLayoutPatternId pattern)
    {
    }

    [Serializable]
    private class AllySlotPlacement
    {
        [Tooltip("0=CF, 1=RW, 2=LW")]
        public int Slot = 0;
        public Vector3 Position;
        public bool Apply = true;
    }

    [Header("検証モード")]
    [Tooltip("ON=TeamBallSupport の候補を ActionUnderTest のみにする（単体動作検証）")]
    [SerializeField] private bool _verificationOnlyCreateSupportAngle = true;
    [Tooltip("ON=本番候補で選出を検証（verificationOnly は強制 OFF）")]
    [SerializeField] private bool _verifyProductionSelection;
    [Tooltip("ON=自動ドライブ観測後に最新 PlanCosts で本番選出を検証（#13〜#18、RuntimePass と併用可）")]
    [SerializeField] private bool _verifyProductionSelectionDuringDrive;
    [Tooltip("ON=本番候補のままホールド後にランタイム追従を検証（ドライブ #17/#18 用）")]
    [SerializeField] private bool _verifyRuntimeFollowDuringBatch;
    [Tooltip("適用後に味方 GoapAgent のプランを中断し再計画")]
    [SerializeField] private bool _triggerGoapReplanAfterApply = true;
    [Tooltip("CF 保持パターン適用時に slot0 も中央へ移動（操作キャラは外すこと）")]
    [SerializeField] private bool _repositionBallOwnerSlot = true;
    [Tooltip("パターン適用時に指定スロットへボールを付与（DebugPlace と同様に changeOwnership）")]
    [SerializeField] private bool _assignBallToOwnerOnApply = true;
    [SerializeField] private int _ballOwnerFormationSlot = 0;

    [Header("検証パターン（初期配置）")]
    [Tooltip("Play 開始時に適用する検証パターン")]
    [SerializeField] private GoapSupportLayoutPatternId _initialVerificationPattern = GoapSupportLayoutPatternId.CfOwner_Clustered;
    [Tooltip("Play 開始後に検証パターンを自動適用")]
    [SerializeField] private bool _applyInitialLayoutOnStart = true;
    [SerializeField] private float _initialApplyTimeoutSeconds = 90f;
    [Tooltip("Custom 選択時のみ使用する slot0/1/2 のワールド座標")]
    [SerializeField] private List<AllySlotPlacement> _customSlotPlacements = new List<AllySlotPlacement>
    {
        new AllySlotPlacement { Slot = 0, Position = new Vector3(0f, 0f, 4.8f), Apply = true },
        new AllySlotPlacement { Slot = 1, Position = new Vector3(0.8f, 0f, 6.4f), Apply = true },
        new AllySlotPlacement { Slot = 2, Position = new Vector3(-0.8f, 0f, 6.4f), Apply = true },
    };

    [Header("Pattern Keys (Play Mode)")]
    [Tooltip("ON= F1〜F12 + Home で検証パターンを切替")]
    [SerializeField] private bool _enablePatternHotkeys;
    [SerializeField] private KeyCode _keyBaseline = KeyCode.F1;
    [SerializeField] private KeyCode _keyClustered = KeyCode.F2;
    [SerializeField] private KeyCode _keyRwWrong = KeyCode.F3;
    [SerializeField] private KeyCode _keyLwWrong = KeyCode.F4;
    [SerializeField] private KeyCode _keyNearLanes = KeyCode.F5;
    [SerializeField] private KeyCode _keyAtCorrectLanes = KeyCode.F6;
    [SerializeField] private KeyCode _keyAllOverlapped = KeyCode.F7;
    [SerializeField] private KeyCode _keyCustom = KeyCode.F8;
    [SerializeField] private KeyCode _keyRwOwner = KeyCode.F9;
    [SerializeField] private KeyCode _keyLwOwner = KeyCode.F10;
    [SerializeField] private KeyCode _keyCfRightWing = KeyCode.F11;
    [SerializeField] private KeyCode _keyCfLeftWing = KeyCode.F12;
    [SerializeField] private KeyCode _keyWingsTooDeep = KeyCode.Home;
    [Tooltip("自動ドライブ専用パターン（PageUp〜End / =）")]
    [SerializeField] private KeyCode _keyDriveAtLanesForward = KeyCode.PageUp;
    [SerializeField] private KeyCode _keyDriveAtLanesForwardBack = KeyCode.PageDown;
    [SerializeField] private KeyCode _keyDriveNearLanesForward = KeyCode.Insert;
    [SerializeField] private KeyCode _keyDriveAtLanesLateralRight = KeyCode.Delete;
    [SerializeField] private KeyCode _keyDriveRwForward = KeyCode.End;
    [SerializeField] private KeyCode _keyDriveLwForward = KeyCode.Equals;

    [Header("Layout Tuning (field ratio)")]
    [SerializeField] private GoapSupportLayoutTuning _layoutTuning = new GoapSupportLayoutTuning();
    [SerializeField] private float _ownerForwardRatio = 0.12f;
    [SerializeField] private float _clusterForwardRatio = 0.04f;
    [SerializeField] private float _clusterLateralRatio = 0.06f;
    [SerializeField] private float _wrongSideLateralRatio = 0.22f;
    [SerializeField] private float _nearLaneBackOffsetRatio = 0.10f;
    [Tooltip("AllOverlapped: 保持者中心からの横ずれ（field ratio）。0.03〜0.05 推奨。小さすぎると WM 誤判定")]
    [SerializeField] private float _overlapMicroLateralRatio = 0.04f;
    [Tooltip("AllOverlapped: 保持者中心からの前ずれ（field ratio）")]
    [SerializeField] private float _overlapMicroForwardRatio = 0.02f;
    [Tooltip("翼保持/サイドCF: 保持者の横位置（field ratio）。OwnerWingEnter(0.12) より大きく")]
    [SerializeField] private float _wingSideOwnerLateralRatio = 0.16f;
    [Tooltip("WingsTooDeepBehind: 翼を保持者より後方へずらす距離（field ratio）")]
    [SerializeField] private float _behindOwnerBackOffsetRatio = 0.08f;

    [Header("一括検証（Play 1回で全パターン）")]
    [Tooltip("ON= Play 開始後に指定範囲のパターンを順に再配置してログ取得")]
    [SerializeField] private bool _runBatchVerificationOnStart;
    [Tooltip("各パターン適用後、GAME 状態でこの秒数ログを蓄積")]
    [SerializeField] private float _batchHoldSecondsPerPattern = 12f;
    [Tooltip("キックオフ（GAME 状態）待ちの上限秒")]
    [SerializeField] private float _batchWaitGameStateTimeoutSeconds = 120f;
    [Tooltip("初回 GAME 遷移後の安定待ち（秒）")]
    [SerializeField] private float _batchSettleSecondsAfterGameState = 2f;
    [Tooltip("ON= 一括検証終了時に Play モードを停止（Editor）")]
    [SerializeField] private bool _stopPlayModeWhenBatchEnds = true;
    [Tooltip("定番セット（ContiguousRange 以外は Start/End を無視）")]
    [SerializeField] private GoapSupportLayoutBatchPreset _batchPreset = GoapSupportLayoutBatchPreset.ContiguousRange;
    [Tooltip("一括検証の開始番号（enum 値。ContiguousRange 時のみ。例: #3=RwWrong, #8=RwOwner）")]
    [SerializeField] private int _batchPatternIndexStart = 3;
    [Tooltip("一括検証の終了番号（enum 値。ContiguousRange 時のみ）")]
    [SerializeField] private int _batchPatternIndexEnd = 18;

    [Header("保持者自動ドライブ（追従検証）")]
    [Tooltip("ON= 観測ホールド中にボール保持者を自動移動（Human/NPC 共通）")]
    [SerializeField] private bool _enableBallOwnerAutoDrive = true;
    [SerializeField] private GoapBallOwnerAutoDriveMode _ballOwnerAutoDriveMode = GoapBallOwnerAutoDriveMode.ForwardBack;
    [Tooltip("移動入力強度（0〜1）")]
    [SerializeField] private float _ballOwnerAutoDriveIntensity = 0.85f;
    [Tooltip("往復・前進の振幅（フィールド長に対する比率）")]
    [SerializeField] private float _ballOwnerAutoDriveAmplitudeRatio = 0.12f;
    [Tooltip("#17/#18 翼保持ドライブ専用振幅（field ratio）。連続前進より小さめ推奨")]
    [SerializeField] private float _wingOwnerAutoDriveAmplitudeRatio = 0.06f;
    [Tooltip("パターン適用後、ドライブ開始までの待ち（秒）。翼の初動を観測する余裕")]
    [SerializeField] private float _ballOwnerAutoDriveStartDelay = 3f;
    [Tooltip("一括検証 OFF 時、手動パターン適用後にドライブする秒数（0=無効）")]
    [SerializeField] private float _ballOwnerAutoDriveManualHoldSeconds = 12f;

    private readonly List<Snapshot> _baseline = new List<Snapshot>();
    private GoapSupportLayoutPatternId _activePattern = GoapSupportLayoutPatternId.Baseline;
    private bool _lastAssignBallOwnershipChanged;
    private Coroutine _batchCoroutine;
    private Coroutine _ownerDriveCoroutine;
    private int _productionSelectionSummaryOffset;
    private int _productionSelectionPassCount;
    private int _productionSelectionEvalCount;
    private int _runtimePassSummaryOffset;
    private int _runtimePassDiagOffset;
    private int _runtimePassPassCount;
    private int _runtimePassEvalCount;
    private struct Snapshot
    {
        public Transform Transform;
        public Vector3 Position;
    }

    private GoapSupportActionUnderTest EffectiveVerificationOnlyAction =>
        !_verifyProductionSelection
        && !_verifyProductionSelectionDuringDrive
        && !_verifyRuntimeFollowDuringBatch
        && _verificationOnlyCreateSupportAngle
            ? ActionUnderTest
            : GoapSupportActionUnderTest.None;

    private bool UsesProductionSelectionAtApply =>
        _verifyProductionSelection && !_verifyProductionSelectionDuringDrive;

    private bool UsesProductionSelectionDuringDrive => _verifyProductionSelectionDuringDrive;

    private bool UsesAnyProductionSelection =>
        UsesProductionSelectionAtApply || UsesProductionSelectionDuringDrive;

    private bool ShouldEvaluateRuntimePassDuringBatch =>
        RuntimePassCriteria != null
        && (EffectiveVerificationOnlyAction != GoapSupportActionUnderTest.None
            || _verifyRuntimeFollowDuringBatch);

    private void OnEnable()
    {
        TeammateNpcSupportPlanning.SetVerificationOnlySupportAction(EffectiveVerificationOnlyAction);
    }

    private void OnDisable()
    {
        StopActiveOwnerDrive();
        TeammateNpcSupportPlanning.SetVerificationOnlySupportAction(GoapSupportActionUnderTest.None);
    }


    private GoapSupportLayoutTuning LayoutTuning
    {
        get
        {
            if (_layoutTuning == null)
            {
                _layoutTuning = new GoapSupportLayoutTuning();
            }

            _layoutTuning.OwnerForwardRatio = _ownerForwardRatio;
            _layoutTuning.ClusterForwardRatio = _clusterForwardRatio;
            _layoutTuning.ClusterLateralRatio = _clusterLateralRatio;
            _layoutTuning.WrongSideLateralRatio = _wrongSideLateralRatio;
            _layoutTuning.NearLaneBackOffsetRatio = _nearLaneBackOffsetRatio;
            _layoutTuning.OverlapMicroLateralRatio = _overlapMicroLateralRatio;
            _layoutTuning.OverlapMicroForwardRatio = _overlapMicroForwardRatio;
            _layoutTuning.WingSideOwnerLateralRatio = _wingSideOwnerLateralRatio;
            _layoutTuning.BehindOwnerBackOffsetRatio = _behindOwnerBackOffsetRatio;
            return _layoutTuning;
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            CaptureBaseline();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_runBatchVerificationOnStart)
        {
            _batchCoroutine = StartCoroutine(RunBatchVerificationWhenReady());
            return;
        }

        if (!_applyInitialLayoutOnStart)
        {
            return;
        }

        if (_initialVerificationPattern == GoapSupportLayoutPatternId.Baseline)
        {
            return;
        }

        StartCoroutine(ApplyInitialVerificationLayoutWhenReady(_initialVerificationPattern));
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!_enablePatternHotkeys)
        {
            return;
        }

        if (Input.GetKeyDown(_keyBaseline)) ApplyVerificationPattern(GoapSupportLayoutPatternId.Baseline);
        if (Input.GetKeyDown(_keyClustered)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);
        if (Input.GetKeyDown(_keyRwWrong)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_RwWrongSide);
        if (Input.GetKeyDown(_keyLwWrong)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide);
        if (Input.GetKeyDown(_keyNearLanes)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes);
        if (Input.GetKeyDown(_keyAtCorrectLanes)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);
        if (Input.GetKeyDown(_keyAllOverlapped)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AllOverlapped);
        if (Input.GetKeyDown(_keyCustom)) ApplyVerificationPattern(GoapSupportLayoutPatternId.Custom);
        if (Input.GetKeyDown(_keyRwOwner)) ApplyVerificationPattern(GoapSupportLayoutPatternId.RwOwner_WingHold);
        if (Input.GetKeyDown(_keyLwOwner)) ApplyVerificationPattern(GoapSupportLayoutPatternId.LwOwner_WingHold);
        if (Input.GetKeyDown(_keyCfRightWing)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_OnRightWing);
        if (Input.GetKeyDown(_keyCfLeftWing)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_OnLeftWing);
        if (Input.GetKeyDown(_keyWingsTooDeep)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_WingsTooDeepBehind);
        if (Input.GetKeyDown(_keyDriveAtLanesForward)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForward);
        if (Input.GetKeyDown(_keyDriveAtLanesForwardBack)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack);
        if (Input.GetKeyDown(_keyDriveNearLanesForward)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward);
        if (Input.GetKeyDown(_keyDriveAtLanesLateralRight)) ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveLateralRight);
        if (Input.GetKeyDown(_keyDriveRwForward)) ApplyVerificationPattern(GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward);
        if (Input.GetKeyDown(_keyDriveLwForward)) ApplyVerificationPattern(GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward);
    }

    [ContextMenu("Capture Baseline (現在位置を保存)")]
    public void CaptureBaseline()
    {
        _baseline.Clear();
        foreach (var ally in GoapSupportVerificationAllyHelper.GetFieldAllies())
        {
            if (ally == null)
            {
                continue;
            }

            _baseline.Add(new Snapshot
            {
                Transform = ally.transform,
                Position = ally.transform.position,
            });
        }

        LogLine($"CaptureBaseline allies={_baseline.Count}");
    }

    [ContextMenu("Test Ball Owner Auto Drive (8s)")]
    private void ContextMenuTestBallOwnerAutoDrive()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GoapSupportLayoutPatternId pattern = _activePattern;
        if (pattern == GoapSupportLayoutPatternId.Baseline)
        {
            pattern = GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes;
        }

        StartOwnerDriveObservation(pattern, 8f);
    }

    [ContextMenu("Apply Baseline")]
    public void ApplyBaseline() => ApplyVerificationPattern(GoapSupportLayoutPatternId.Baseline);

    [ContextMenu("Apply CfOwner_Clustered")]
    public void ApplyClustered() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);

    [ContextMenu("Apply CfOwner_RwWrongSide")]
    public void ApplyRwWrong() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_RwWrongSide);

    [ContextMenu("Apply CfOwner_LwOnWrongSide")]
    public void ApplyLwWrong() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide);

    [ContextMenu("Apply CfOwner_NearCorrectLanes")]
    public void ApplyNearLanes() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes);

    [ContextMenu("Apply CfOwner_AtCorrectLanes")]
    public void ApplyAtCorrectLanes() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);

    [ContextMenu("Apply CfOwner_AllOverlapped")]
    public void ApplyAllOverlapped() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AllOverlapped);

    [ContextMenu("Apply Custom")]
    public void ApplyCustomLayout() => ApplyVerificationPattern(GoapSupportLayoutPatternId.Custom);

    [ContextMenu("Apply RwOwner_WingHold")]
    public void ApplyRwOwnerWingHold() => ApplyVerificationPattern(GoapSupportLayoutPatternId.RwOwner_WingHold);

    [ContextMenu("Apply LwOwner_WingHold")]
    public void ApplyLwOwnerWingHold() => ApplyVerificationPattern(GoapSupportLayoutPatternId.LwOwner_WingHold);

    [ContextMenu("Apply CfOwner_OnRightWing")]
    public void ApplyCfOwnerOnRightWing() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_OnRightWing);

    [ContextMenu("Apply CfOwner_OnLeftWing")]
    public void ApplyCfOwnerOnLeftWing() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_OnLeftWing);

    [ContextMenu("Apply CfOwner_WingsTooDeepBehind")]
    public void ApplyCfOwnerWingsTooDeepBehind() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_WingsTooDeepBehind);

    [ContextMenu("Apply Drive: AtCorrectLanes + Forward")]
    public void ApplyDriveAtCorrectLanesForward() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForward);

    [ContextMenu("Apply Drive: AtCorrectLanes + ForwardBack")]
    public void ApplyDriveAtCorrectLanesForwardBack() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack);

    [ContextMenu("Apply Drive: NearCorrectLanes + Forward")]
    public void ApplyDriveNearCorrectLanesForward() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward);

    [ContextMenu("Apply Drive: AtCorrectLanes + LateralRight")]
    public void ApplyDriveAtCorrectLanesLateralRight() => ApplyVerificationPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveLateralRight);

    [ContextMenu("Apply Drive: RwOwner + ForwardBack")]
    public void ApplyDriveRwOwnerForward() => ApplyVerificationPattern(GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward);

    [ContextMenu("Apply Drive: LwOwner + ForwardBack")]
    public void ApplyDriveLwOwnerForward() => ApplyVerificationPattern(GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward);

    [ContextMenu("Run Batch Verification (Play Mode)")]
    public void RunBatchVerificationFromMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{SummaryLogTag}] Run Batch Verification requires Play Mode");
            return;
        }

        if (_batchCoroutine != null)
        {
            StopCoroutine(_batchCoroutine);
        }

        _batchCoroutine = StartCoroutine(RunBatchVerificationWhenReady());
    }

    [ContextMenu("Capture Scene Positions → Custom Slots")]
    public void CaptureScenePositionsToCustom()
    {
        EnsureDefaultCustomSlots();
        foreach (var ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            AllySlotPlacement entry = FindOrCreateCustomPlacement(ally.Slot);
            entry.Position = ally.Facade.transform.position;
            entry.Apply = true;
        }

        _initialVerificationPattern = GoapSupportLayoutPatternId.Custom;
        LogLine($"CaptureScenePositionsToCustom slots={_customSlotPlacements.Count} pattern=Custom");
    }

    public void ApplyVerificationPattern(GoapSupportLayoutPatternId pattern)
    {
        if (!TryApplyVerificationPatternLayout(pattern))
        {
            return;
        }

        AfterApply(pattern);
    }

    private Dictionary<int, Vector3> BuildCustomTargets()
    {
        var map = new Dictionary<int, Vector3>();
        if (_customSlotPlacements == null)
        {
            return map;
        }

        foreach (AllySlotPlacement entry in _customSlotPlacements)
        {
            if (entry == null || !entry.Apply)
            {
                continue;
            }

            if (entry.Slot < 0 || entry.Slot > 2)
            {
                continue;
            }

            map[entry.Slot] = entry.Position;
        }

        return map;
    }

    private void EnsureDefaultCustomSlots()
    {
        if (_customSlotPlacements == null)
        {
            _customSlotPlacements = new List<AllySlotPlacement>();
        }

        for (int slot = 0; slot <= 2; slot++)
        {
            FindOrCreateCustomPlacement(slot);
        }
    }

    private AllySlotPlacement FindOrCreateCustomPlacement(int slot)
    {
        foreach (AllySlotPlacement entry in _customSlotPlacements)
        {
            if (entry != null && entry.Slot == slot)
            {
                return entry;
            }
        }

        var created = new AllySlotPlacement { Slot = slot, Apply = true };
        _customSlotPlacements.Add(created);
        return created;
    }

    private IEnumerator RunBatchVerificationWhenReady()
    {
        try
        {
            yield return WaitUntilReadyForLayoutApply(
                GoapBatchVerifyEnvironment.ResolveTimeout(_initialApplyTimeoutSeconds, 180f));
            if (!IsReadyForLayoutApply())
            {
                string blocked = GoapBatchVerifyLayoutReadiness.DescribeBlocked(LayoutTuning);
                LogLine($"RunBatchVerification aborted: layout apply not ready ({blocked})");
                GoapDiagnosticLog.WriteBanner($"BATCH_ABORT NOT_READY {blocked}");
                yield break;
            }

            ResetDebugLogSession(
                UsesProductionSelectionAtApply
                    ? ProductionSelectionVerificationBanner
                    : UsesProductionSelectionDuringDrive
                        ? "Drive production selection during owner auto-drive (#13-#18)"
                        : BatchVerificationBanner);
            if (_baseline.Count == 0)
            {
                CaptureBaseline();
            }

            _productionSelectionPassCount = 0;
            _productionSelectionEvalCount = 0;
            _runtimePassPassCount = 0;
            _runtimePassEvalCount = 0;

            var patterns = BuildBatchPatternList();
            int total = patterns.Count;
            if (total == 0)
            {
                LogLine("RunBatchVerification aborted: no patterns in selected range");
                GoapDiagnosticLog.WriteBanner("BATCH_ABORT EMPTY_RANGE");
                yield break;
            }

            int rangeStart = _batchPreset == GoapSupportLayoutBatchPreset.ContiguousRange
                ? GetClampedBatchRangeStart()
                : -1;
            int rangeEnd = _batchPreset == GoapSupportLayoutBatchPreset.ContiguousRange
                ? GetClampedBatchRangeEnd()
                : -1;
            string rangeLabel = _batchPreset == GoapSupportLayoutBatchPreset.ContiguousRange
                ? $"range={rangeStart}..{rangeEnd}"
                : $"preset={_batchPreset}";
            LogLine(
                $"RunBatchVerification start {rangeLabel} count={total} " +
                $"holdSec={_batchHoldSecondsPerPattern:F1} " +
                $"productionSelection={UsesProductionSelectionAtApply} " +
                $"driveSelection={UsesProductionSelectionDuringDrive} " +
                $"verificationOnly={EffectiveVerificationOnlyAction}");
            GoapDiagnosticLog.WriteBanner(
                _batchPreset == GoapSupportLayoutBatchPreset.ContiguousRange
                    ? $"BATCH_START range={rangeStart}-{rangeEnd} count={total}"
                    : $"BATCH_START preset={_batchPreset} count={total}");

            LogLine("RunBatchVerification waiting for GAME state (kickoff)...");
            float gameStateTimeout = GoapBatchVerifyEnvironment.ResolveTimeout(
                _batchWaitGameStateTimeoutSeconds,
                180f);
            yield return WaitForGameStateCoroutine(gameStateTimeout);
            if (!IsGameState())
            {
                LogLine("RunBatchVerification aborted: GAME state timeout before first pattern");
                GoapDiagnosticLog.WriteBanner("BATCH_ABORT GAME_TIMEOUT");
                yield break;
            }

            if (_batchSettleSecondsAfterGameState > 0f)
            {
                yield return new WaitForSeconds(_batchSettleSecondsAfterGameState);
            }

            for (int i = 0; i < total; i++)
            {
                GoapSupportLayoutPatternId pattern = patterns[i];
                int index = i + 1;
                WriteBatchPatternBoundary("BEGIN", pattern, index, total);

                yield return ApplyVerificationPatternAndWait(pattern);

                if (UsesProductionSelectionAtApply)
                {
                    yield return WaitForFirstProductionPlanCostsCoroutine(
                        pattern,
                        ProductionSelectionPlanCostsTimeoutSeconds,
                        ProductionSelectionExpectation);
                    LogAtCorrectLanesPlanningDiag(pattern, "postApply");
                    EvaluateProductionSelectionForPattern(
                        pattern,
                        index,
                        total,
                        ProductionSelectionExpectation,
                        GoapProductionSelectionResolveMode.FirstPlanCosts);
                }
                else
                {
                    float holdSeconds = ResolveBatchHoldSeconds(pattern);
                    if (holdSeconds > 0f)
                    {
                        yield return HoldPatternObservationCoroutine(pattern, holdSeconds);
                    }

                    if (UsesProductionSelectionDuringDrive)
                    {
                        yield return WaitForDriveProductionPlanCostsCoroutine(
                            pattern,
                            ProductionSelectionPlanCostsTimeoutSeconds,
                            DriveProductionSelectionExpectation);
                        EvaluateProductionSelectionForPattern(
                            pattern,
                            index,
                            total,
                            DriveProductionSelectionExpectation,
                            GoapProductionSelectionResolveMode.LastPlanCosts,
                            "drive");
                    }

                    if (ShouldEvaluateRuntimePassDuringBatch)
                    {
                        EvaluateRuntimePassForPattern(pattern, index, total);
                    }
                }

                WriteBatchPatternBoundary("END", pattern, index, total);
            }

            LogLine("RunBatchVerification complete");
            if (UsesAnyProductionSelection)
            {
                LogLine(
                    $"ProductionSelection TOTAL {_productionSelectionPassCount}/{_productionSelectionEvalCount} patterns PASS");
                GoapDiagnosticLog.WriteBanner(
                    $"SELECTION_TOTAL {_productionSelectionPassCount}/{_productionSelectionEvalCount}");
            }

            if (ShouldEvaluateRuntimePassDuringBatch)
            {
                LogLine($"RuntimePass TOTAL {_runtimePassPassCount}/{_runtimePassEvalCount} patterns PASS");
                GoapDiagnosticLog.WriteBanner($"RUNTIME_TOTAL {_runtimePassPassCount}/{_runtimePassEvalCount}");
            }

            if (!UsesAnyProductionSelection && !ShouldEvaluateRuntimePassDuringBatch)
            {
                LogLine("RunBatchVerification complete (no automated verdict configured)");
            }
            GoapDiagnosticLog.WriteBanner("BATCH_COMPLETE");
        }
        finally
        {
            StopActiveOwnerDrive();
            _batchCoroutine = null;
            if (_stopPlayModeWhenBatchEnds)
            {
                StopPlayModeAfterBatch();
            }
        }
    }

    private void StopPlayModeAfterBatch()
    {
        LogLineStatic("StopPlayModeAfterBatch");
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
#else
        Application.Quit();
#endif
    }

    private List<GoapSupportLayoutPatternId> BuildBatchPatternList()
    {
        return _batchPreset switch
        {
            GoapSupportLayoutBatchPreset.CfOwnerStatic =>
                GoapSupportLayoutPatternCatalog.BuildCfOwnerStaticSuite(),
            GoapSupportLayoutBatchPreset.CfOwnerStaticGetOpen =>
                GoapSupportLayoutPatternCatalog.BuildCfOwnerStaticGetOpenSuite(),
            GoapSupportLayoutBatchPreset.WingOwner =>
                GoapSupportLayoutPatternCatalog.BuildWingOwnerSuite(),
            GoapSupportLayoutBatchPreset.WingOwnerDrive =>
                GoapSupportLayoutPatternCatalog.BuildWingOwnerDriveSuite(),
            GoapSupportLayoutBatchPreset.CfOwnerDrive =>
                GoapSupportLayoutPatternCatalog.BuildCfOwnerDriveSuite(),
            GoapSupportLayoutBatchPreset.AllDrive =>
                GoapSupportLayoutPatternCatalog.BuildAllDriveSuite(),
            GoapSupportLayoutBatchPreset.CsaRegression =>
                GoapSupportLayoutPatternCatalog.BuildCsaRegressionSuite(),
            GoapSupportLayoutBatchPreset.CombinedSupportRegression =>
                GoapSupportLayoutPatternCatalog.BuildCombinedSupportRegressionSuite(),
            _ => GoapSupportLayoutPatternCatalog.BuildRange(
                _batchPatternIndexStart,
                _batchPatternIndexEnd),
        };
    }

    private int GetClampedBatchRangeStart() =>
        Mathf.Clamp(_batchPatternIndexStart, GoapSupportLayoutPatternCatalog.NumberMin, GoapSupportLayoutPatternCatalog.NumberMax);

    private int GetClampedBatchRangeEnd() =>
        Mathf.Clamp(_batchPatternIndexEnd, GoapSupportLayoutPatternCatalog.NumberMin, GoapSupportLayoutPatternCatalog.NumberMax);

    private static int GetBatchPatternNumber(GoapSupportLayoutPatternId pattern) =>
        GoapSupportLayoutPatternCatalog.GetNumber(pattern);

    private IEnumerator WaitUntilReadyForLayoutApply(float timeoutSeconds)
    {
        if (!GoapDebugPlayBootstrap.IsSpawnReady)
        {
            bool signaled = false;
            void OnSpawnReady() => signaled = true;
            GoapDebugPlayBootstrap.SpawnReady += OnSpawnReady;

            float elapsed = 0f;
            while (elapsed < timeoutSeconds
                && !GoapDebugPlayBootstrap.IsSpawnReady
                && !signaled
                && !IsReadyForLayoutApply())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            GoapDebugPlayBootstrap.SpawnReady -= OnSpawnReady;
        }

        float waitElapsed = 0f;
        while (waitElapsed < timeoutSeconds && !IsReadyForLayoutApply())
        {
            waitElapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForGameStateCoroutine(float timeoutSeconds)
    {
        if (IsGameState())
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < timeoutSeconds && !IsGameState())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsGameState())
        {
            LogLine($"WaitForGameState timeout={timeoutSeconds:F0}s");
            if (GoapBatchVerifyEnvironment.IsActive && StateManager.Instance != null)
            {
                StateManager.Instance.changeStateLocal(StateManager.STATE_KIND.GAME);
                LogLine("WaitForGameState batch verify fallback: forced GAME");
            }
        }
    }

    private static bool IsGameState()
    {
        return StateManager.Instance != null
            && StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME);
    }

    private IEnumerator ApplyVerificationPatternAndWait(GoapSupportLayoutPatternId pattern)
    {
        if (!TryApplyVerificationPatternLayout(pattern))
        {
            yield break;
        }

        ApplyCompanionVerificationState(pattern);

        if (pattern != GoapSupportLayoutPatternId.Baseline && _assignBallToOwnerOnApply)
        {
            yield return AssignBallToFormationSlotCoroutine(GetBallOwnerSlotForPattern(pattern));
            if (_triggerGoapReplanAfterApply)
            {
                yield return null;
                if (_lastAssignBallOwnershipChanged)
                {
                    LogLine("TriggerAllyGoapReplan skipped: ball ownership changed (BallContext replan)");
                }
                else
                {
                    TriggerAllyGoapReplan();
                }
            }
        }
        else if (_triggerGoapReplanAfterApply)
        {
            TriggerAllyGoapReplan();
        }
    }

    private bool TryApplyVerificationPatternLayout(GoapSupportLayoutPatternId pattern)
    {
        TeammateNpcSupportPlanning.SetVerificationOnlySupportAction(EffectiveVerificationOnlyAction);

        if (pattern == GoapSupportLayoutPatternId.Baseline)
        {
            if (_baseline.Count == 0)
            {
                CaptureBaseline();
            }

            RestoreBaseline();
            _activePattern = pattern;
            LogLine($"ApplyVerificationPattern({pattern}) restored={_baseline.Count}");
            return true;
        }

        if (pattern == GoapSupportLayoutPatternId.Custom)
        {
            Dictionary<int, Vector3> customTargets = BuildCustomTargets();
            if (customTargets.Count == 0)
            {
                LogLine($"ApplyVerificationPattern({pattern}) failed: no custom slot positions");
                return false;
            }

            ApplyTargets(customTargets, GetBallOwnerSlotForPattern(pattern));
            _activePattern = pattern;

            if (TryGetFieldContext(out GoapSupportLayoutFieldContext customCtx))
            {
                LogExpectedLanes(customCtx);
            }

            LogLine(
                $"ApplyVerificationPattern({pattern}) verificationOnly={EffectiveVerificationOnlyAction} " +
                $"ballOwnerSlot={GetBallOwnerSlotForPattern(pattern)} " +
                $"slot0={SlotPos(customTargets, 0)} slot1={SlotPos(customTargets, 1)} slot2={SlotPos(customTargets, 2)}");
            return true;
        }

        if (!TryGetFieldContext(out GoapSupportLayoutFieldContext ctx))
        {
            LogLine($"ApplyVerificationPattern({pattern}) failed: TeamBlackboard unavailable");
            return false;
        }

        var targets = GoapSupportLayoutPatternLibrary.ComputeTargets(pattern, ctx, LayoutTuning);
        int ballOwnerSlot = GetBallOwnerSlotForPattern(pattern);
        ApplyTargets(targets, ballOwnerSlot);
        _activePattern = pattern;
        LogExpectedLanes(ctx);
        string driveHint = GoapSupportLayoutDrivePatternLibrary.TryGetAutoDriveOverride(pattern, out GoapBallOwnerAutoDriveMode driveMode)
            ? $" drive={driveMode} amp={ResolveBallOwnerAutoDriveAmplitudeRatio(pattern):F2}"
            : string.Empty;
        LogLine(
            $"ApplyVerificationPattern({pattern}) owner={Fmt(GoapSupportLayoutPatternLibrary.ResolvePatternOwnerPosition(pattern, ctx, LayoutTuning))} " +
            $"ballOwnerSlot={ballOwnerSlot} verificationOnly={EffectiveVerificationOnlyAction}{driveHint} " +
            $"slot0={SlotPos(targets, 0)} slot1={SlotPos(targets, 1)} slot2={SlotPos(targets, 2)}");
        return true;
    }

    private int GetBallOwnerSlotForPattern(GoapSupportLayoutPatternId pattern) =>
        GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, _ballOwnerFormationSlot);

    private GoapBallOwnerAutoDriveMode ResolveBallOwnerAutoDriveMode(GoapSupportLayoutPatternId pattern)
    {
        if (GoapSupportLayoutDrivePatternLibrary.TryGetAutoDriveOverride(pattern, out GoapBallOwnerAutoDriveMode overrideMode))
        {
            return overrideMode;
        }

        return _ballOwnerAutoDriveMode;
    }

    private float ResolveBallOwnerAutoDriveAmplitudeRatio(GoapSupportLayoutPatternId pattern) =>
        GoapSupportLayoutDrivePatternLibrary.ResolveAmplitudeRatio(
            pattern, _ballOwnerAutoDriveAmplitudeRatio, _wingOwnerAutoDriveAmplitudeRatio);

    private void WriteBatchPatternBoundary(string phase, GoapSupportLayoutPatternId pattern, int index, int total)
    {
        if (phase == "BEGIN")
        {
            if (UsesAnyProductionSelection)
            {
                _productionSelectionSummaryOffset = GoapActionVerificationSessionLog.CountLines();
            }

            if (ShouldEvaluateRuntimePassDuringBatch)
            {
                _runtimePassSummaryOffset = GoapActionVerificationSessionLog.CountLines();
                _runtimePassDiagOffset = GoapDiagnosticLog.CountLines();
            }
        }

        int patternNumber = GetBatchPatternNumber(pattern);
        string title = $"BATCH_{phase} {index}/{total} #{patternNumber} {pattern}";
        GoapDiagnosticLog.WriteBanner(title);
        LogLine(title);
    }

    private void LogAtCorrectLanesPlanningDiag(GoapSupportLayoutPatternId pattern, string phase)
    {
        if (pattern != GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes)
        {
            return;
        }

        var csaAction = Resources.FindObjectsOfTypeAll<CreateSupportAngleActionSO>().FirstOrDefault();
        var getOpenAction = Resources.FindObjectsOfTypeAll<GetOpenActionSO>().FirstOrDefault();

        foreach (GoapSupportVerificationAllyHelper.AllySlot ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            if (ally.Slot != 1 && ally.Slot != 2)
            {
                continue;
            }

            PlayerBlackboard bb = ally.Facade != null
                ? ally.Facade.GetComponentInChildren<PlayerBlackboard>()
                : null;
            if (bb == null)
            {
                continue;
            }

            string diag = TeammateNpcSupportPlanning.BuildAtCorrectLanesPlanningDiag(bb, csaAction, getOpenAction);
            string line = $"AtCorrectLanesDiag phase={phase} {diag}";
            LogLine(line);
            GoapDiagnosticLog.Write(line);
        }

        GoapDiagnosticLog.WriteBanner($"AT_CORRECT_LANES_DIAG phase={phase}");
    }

    private float ResolveBatchHoldSeconds(GoapSupportLayoutPatternId pattern)
    {
        if (_batchHoldSecondsPerPattern > 0f)
        {
            return _batchHoldSecondsPerPattern;
        }

        if (UsesProductionSelectionDuringDrive
            && GoapSupportLayoutDrivePatternLibrary.TryGetAutoDriveOverride(
                pattern,
                out GoapBallOwnerAutoDriveMode driveMode)
            && driveMode != GoapBallOwnerAutoDriveMode.None)
        {
            return Mathf.Max(
                _ballOwnerAutoDriveManualHoldSeconds,
                _ballOwnerAutoDriveStartDelay + 4f);
        }

        return 0f;
    }

    private IEnumerator WaitForFirstProductionPlanCostsCoroutine(
        GoapSupportLayoutPatternId pattern,
        float timeoutSeconds,
        IGoapProductionSelectionExpectation expectation)
    {
        yield return WaitForProductionPlanCostsCoroutine(
            pattern,
            timeoutSeconds,
            expectation,
            "first");
    }

    private IEnumerator WaitForDriveProductionPlanCostsCoroutine(
        GoapSupportLayoutPatternId pattern,
        float timeoutSeconds,
        IGoapProductionSelectionExpectation expectation)
    {
        yield return WaitForProductionPlanCostsCoroutine(
            pattern,
            timeoutSeconds,
            expectation,
            "drive-last");
    }

    private IEnumerator WaitForProductionPlanCostsCoroutine(
        GoapSupportLayoutPatternId pattern,
        float timeoutSeconds,
        IGoapProductionSelectionExpectation expectation,
        string phaseLabel)
    {
        var requiredSlots = new List<int>();
        for (int slot = 0; slot <= 2; slot++)
        {
            if (expectation != null
                && expectation.TryGetExpectation(pattern, slot, out _, out bool shouldEvaluate)
                && shouldEvaluate)
            {
                requiredSlots.Add(slot);
            }
        }

        if (requiredSlots.Count == 0)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            List<string> lines = GoapActionVerificationSessionLog.ReadLinesSince(_productionSelectionSummaryOffset);
            if (AllSlotsHavePlanCosts(lines, requiredSlots))
            {
                LogLine(
                    $"ProductionSelection ready {phaseLabel} PlanCosts slots=[{string.Join(",", requiredSlots)}] " +
                    $"elapsed={elapsed:F2}s");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        LogLine(
            $"ProductionSelection WARN {phaseLabel} PlanCosts timeout after {timeoutSeconds:F0}s " +
            $"slots=[{string.Join(",", requiredSlots)}]");
    }

    private static bool AllSlotsHavePlanCosts(IList<string> lines, List<int> slots)
    {
        foreach (int slot in slots)
        {
            if (!HasPlanCostsForSlot(lines, slot))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasPlanCostsForSlot(IList<string> lines, int slot)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (!line.Contains("[GOAP_SUMMARY]") || !line.Contains("PlanCosts("))
            {
                continue;
            }

            if (line.Contains($"slot={slot},") || line.Contains($"slot={slot} "))
            {
                return true;
            }
        }

        return false;
    }

    private void EvaluateProductionSelectionForPattern(
        GoapSupportLayoutPatternId pattern,
        int index,
        int total,
        IGoapProductionSelectionExpectation expectation,
        GoapProductionSelectionResolveMode resolveMode,
        string phaseLabel = "apply")
    {
        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom)
        {
            LogLine($"ProductionSelection SKIP {index}/{total} #{GetBatchPatternNumber(pattern)} {pattern}");
            return;
        }

        List<string> lines = GoapActionVerificationSessionLog.ReadLinesSince(_productionSelectionSummaryOffset);
        GoapProductionSelectionEvaluationResult result = GoapProductionSelectionEvaluator.EvaluatePattern(
            pattern,
            expectation,
            lines,
            GoapSupportVerificationAllyHelper.ResolvePlayerIdForSlot,
            resolveMode);

        if (result.EvalCount == 0)
        {
            LogLine(
                $"ProductionSelection SKIP {index}/{total} #{GetBatchPatternNumber(pattern)} {pattern} " +
                $"(no eval slots, phase={phaseLabel})");
            return;
        }

        if (result.PatternPass)
        {
            _productionSelectionPassCount++;
        }

        _productionSelectionEvalCount++;

        int patternNumber = GetBatchPatternNumber(pattern);
        string verdict = result.PatternPass ? "PASS" : "FAIL";
        LogLine(
            $"ProductionSelection {verdict} {index}/{total} #{patternNumber} {pattern} " +
            $"phase={phaseLabel} mode={resolveMode} ({result.PassCount}/{result.EvalCount}) {result.DetailText}");
        GoapDiagnosticLog.WriteBanner(
            $"SELECTION_{verdict} {index}/{total} #{patternNumber} {pattern} {result.PassCount}/{result.EvalCount}");
    }

    private void EvaluateRuntimePassForPattern(
        GoapSupportLayoutPatternId pattern,
        int index,
        int total)
    {
        if (RuntimePassCriteria == null)
        {
            return;
        }

        List<string> summaryLines = GoapActionVerificationSessionLog.ReadLinesSince(_runtimePassSummaryOffset);
        List<string> diagLines = GoapDiagnosticLog.ReadLinesSince(_runtimePassDiagOffset);
        GoapSupportActionRuntimePassResult result = GoapSupportActionRuntimePassEvaluator.EvaluatePattern(
            pattern,
            RuntimePassCriteria,
            diagLines,
            summaryLines,
            GoapSupportVerificationAllyHelper.ResolvePlayerIdForSlot);

        if (!result.ShouldEvaluate)
        {
            LogLine($"RuntimePass SKIP {index}/{total} #{GetBatchPatternNumber(pattern)} {pattern} ({result.DetailText})");
            return;
        }

        _runtimePassEvalCount++;
        if (result.PatternPass)
        {
            _runtimePassPassCount++;
        }

        int patternNumber = GetBatchPatternNumber(pattern);
        string verdict = result.PatternPass ? "PASS" : "FAIL";
        LogLine($"RuntimePass {verdict} {index}/{total} #{patternNumber} {pattern} {result.DetailText}");
        GoapDiagnosticLog.WriteBanner($"RUNTIME_{verdict} {index}/{total} #{patternNumber} {pattern}");
    }

    private void ResetDebugLogSession(string banner)
    {
        GoapActionVerificationSessionLog.ResetSession(SummaryLogTag, banner);
        if (!string.IsNullOrEmpty(banner))
        {
            GoapDiagnosticLog.WriteBanner(banner);
        }
    }

    private IEnumerator ApplyInitialVerificationLayoutWhenReady(GoapSupportLayoutPatternId pattern)
    {
        yield return WaitUntilReadyForLayoutApply(_initialApplyTimeoutSeconds);

        if (!IsReadyForLayoutApply())
        {
            LogLine(
                $"ApplyInitialVerificationLayout({pattern}) skipped: timeout " +
                $"fieldAllies={GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot().Count} spawnReady={GoapDebugPlayBootstrap.IsSpawnReady}");
            yield break;
        }

        if (_baseline.Count == 0)
        {
            CaptureBaseline();
        }

        ApplyVerificationPattern(pattern);
    }

    private void ApplyTargets(Dictionary<int, Vector3> targets, int ballOwnerSlot)
    {
        foreach (var ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            if (!targets.TryGetValue(ally.Slot, out Vector3 pos))
            {
                continue;
            }

            if (ally.Slot == ballOwnerSlot && !_repositionBallOwnerSlot)
            {
                continue;
            }

            pos.y = ally.Facade.transform.position.y;
            ally.Facade.transform.position = pos;
        }
    }

    private void LogExpectedLanes(GoapSupportLayoutFieldContext ctx)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return;
        }

        int ownerSlot = CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB);
        var zone = CreateSupportAnglePositioning.ResolveOwnerZone(teamBB);
        for (int slot = 0; slot <= 2; slot++)
        {
            var lane = CreateSupportAnglePositioning.ResolveSlotLaneRole(slot, zone, ownerSlot);
            Vector3 target = CreateSupportAnglePositioning.SelectBestPosition(
                ctx.OwnerAnchor,
                slot,
                teamBB,
                CreateSupportAnglePositioning.CreateDefaultSettings());
            LogLine($"Expected slot={slot} lane={lane} target={Fmt(target)} ownerSlot={ownerSlot}");
        }
    }

    private void AfterApply(GoapSupportLayoutPatternId pattern)
    {
        if (pattern != GoapSupportLayoutPatternId.Baseline && _assignBallToOwnerOnApply)
        {
            StartCoroutine(AfterApplyCoroutine(pattern));
            return;
        }

        if (_triggerGoapReplanAfterApply)
        {
            TriggerAllyGoapReplan();
        }

        TryStartManualOwnerDrive(pattern);
    }

    private bool IsBatchCoroutineActive => _batchCoroutine != null;

    private void TryStartManualOwnerDrive(GoapSupportLayoutPatternId pattern)
    {
        if (!_enableBallOwnerAutoDrive
            || pattern == GoapSupportLayoutPatternId.Baseline
            || IsBatchCoroutineActive
            || _ballOwnerAutoDriveManualHoldSeconds <= 0f)
        {
            return;
        }

        StartOwnerDriveObservation(pattern, _ballOwnerAutoDriveManualHoldSeconds);
    }

    private void StartOwnerDriveObservation(GoapSupportLayoutPatternId pattern, float duration)
    {
        if (!Application.isPlaying || duration <= 0f)
        {
            return;
        }

        if (_ownerDriveCoroutine != null)
        {
            StopCoroutine(_ownerDriveCoroutine);
            _ownerDriveCoroutine = null;
        }

        _ownerDriveCoroutine = StartCoroutine(HoldPatternObservationCoroutine(pattern, duration));
    }

    private void StopActiveOwnerDrive()
    {
        if (_ownerDriveCoroutine != null)
        {
            StopCoroutine(_ownerDriveCoroutine);
            _ownerDriveCoroutine = null;
        }

        if (_activePattern != GoapSupportLayoutPatternId.Baseline)
        {
            StopBallOwnerDrive(GetBallOwnerSlotForPattern(_activePattern));
        }
    }

    private IEnumerator HoldPatternObservationCoroutine(GoapSupportLayoutPatternId pattern, float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        int ownerSlot = GetBallOwnerSlotForPattern(pattern);
        GoapBallOwnerAutoDriveMode driveMode = ResolveBallOwnerAutoDriveMode(pattern);
        bool driveEnabled = _enableBallOwnerAutoDrive
            && driveMode != GoapBallOwnerAutoDriveMode.None
            && pattern != GoapSupportLayoutPatternId.Baseline
            && IsGameState();

        if (driveEnabled)
        {
            float ampRatio = ResolveBallOwnerAutoDriveAmplitudeRatio(pattern);
            LogLine(
                $"BallOwnerAutoDrive start pattern={pattern} mode={driveMode} ampRatio={ampRatio:F2} " +
                $"slot={ownerSlot} duration={duration:F1}s delay={_ballOwnerAutoDriveStartDelay:F1}s");
            GoapDiagnosticLog.WriteBanner(
                $"AUTO_DRIVE_BEGIN #{GetBatchPatternNumber(pattern)} {pattern} {driveMode} amp={ampRatio:F2}");
        }

        float elapsed = 0f;
        Vector3 driveAnchor = Vector3.zero;
        bool anchorSet = false;

        while (elapsed < duration)
        {
            if (driveEnabled && elapsed >= _ballOwnerAutoDriveStartDelay)
            {
                AnimalFacade owner = GetAllyFacadeBySlot(ownerSlot);
                if (owner != null)
                {
                    if (!anchorSet)
                    {
                        driveAnchor = owner.transform.position;
                        anchorSet = true;
                    }

                    float driveElapsed = elapsed - _ballOwnerAutoDriveStartDelay;
                    float driveDuration = Mathf.Max(duration - _ballOwnerAutoDriveStartDelay, 0.01f);
                    Vector3 target = ComputeBallOwnerDriveTarget(
                        owner,
                        driveAnchor,
                        driveElapsed,
                        driveDuration,
                        driveMode,
                        pattern);
                    GoapDebugAnimalMotor.TryMoveToward(owner, target, _ballOwnerAutoDriveIntensity);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopBallOwnerDrive(ownerSlot);

        if (driveEnabled)
        {
            LogLine($"BallOwnerAutoDrive end pattern={pattern} slot={ownerSlot}");
            GoapDiagnosticLog.WriteBanner($"AUTO_DRIVE_END #{GetBatchPatternNumber(pattern)} {pattern}");
        }

        _ownerDriveCoroutine = null;
    }

    private Vector3 ComputeBallOwnerDriveTarget(
        AnimalFacade owner,
        Vector3 anchor,
        float driveElapsed,
        float driveDuration,
        GoapBallOwnerAutoDriveMode driveMode,
        GoapSupportLayoutPatternId pattern)
    {
        if (!TryGetFieldContext(out GoapSupportLayoutFieldContext ctx) || owner == null)
        {
            return anchor;
        }

        float amp = ctx.FieldLength * ResolveBallOwnerAutoDriveAmplitudeRatio(pattern);
        float phase = driveDuration > 0.01f ? driveElapsed / driveDuration : 0f;
        float wave = Mathf.Sin(phase * Mathf.PI * 2f);

        return driveMode switch
        {
            GoapBallOwnerAutoDriveMode.Forward => owner.transform.position + ctx.ToGoal * amp,
            GoapBallOwnerAutoDriveMode.ForwardBack => anchor + ctx.ToGoal * (wave * amp),
            GoapBallOwnerAutoDriveMode.LateralRight => anchor + ctx.Right * (wave * amp),
            GoapBallOwnerAutoDriveMode.LateralLeft => anchor - ctx.Right * (wave * amp),
            _ => anchor,
        };
    }

    private void StopBallOwnerDrive(int ownerSlot)
    {
        AnimalFacade owner = GetAllyFacadeBySlot(ownerSlot);
        if (owner != null)
        {
            GoapDebugAnimalMotor.Stop(owner);
        }
    }

    private IEnumerator AfterApplyCoroutine(GoapSupportLayoutPatternId pattern)
    {
        yield return AssignBallToFormationSlotCoroutine(GetBallOwnerSlotForPattern(pattern));
        if (!_triggerGoapReplanAfterApply)
        {
            TryStartManualOwnerDrive(pattern);
            yield break;
        }

        // changeOwnership 直後は BallContextChanged が再計画する。Abort 付き replan は競合して NoGoal 化しやすい。
        yield return null;
        if (_lastAssignBallOwnershipChanged)
        {
            LogLine("TriggerAllyGoapReplan skipped: ball ownership changed (BallContext replan)");
            TryStartManualOwnerDrive(pattern);
            yield break;
        }

        TriggerAllyGoapReplan();
        TryStartManualOwnerDrive(pattern);
    }

    private bool IsReadyForLayoutApply()
    {
        if (!TryGetFieldContext(out _))
        {
            return false;
        }

        if (GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot().Count < 3)
        {
            return false;
        }

        for (int slot = 0; slot <= 2; slot++)
        {
            if (GetAllyFacadeBySlot(slot) == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBallAvailable()
    {
        var teamFacade = TeamFacade.Instance;
        return teamFacade != null
            && teamFacade.BallManager != null
            && teamFacade.BallManager.Ball != null
            && teamFacade.TeamBlackboard != null
            && teamFacade.TeamBlackboard.BallInfo.IsExistBall;
    }

    private IEnumerator AssignBallToFormationSlotCoroutine(int slot)
    {
        _lastAssignBallOwnershipChanged = false;
        const float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout && !IsBallAvailable())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!TryAssignBallToFormationSlot(slot, out string reason, out bool ownershipChanged))
        {
            LogLine($"AssignBall(slot={slot}) failed: {reason}");
            yield break;
        }

        _lastAssignBallOwnershipChanged = ownershipChanged;

        var ball = TeamFacade.Instance.BallManager.Ball;
        if (ball != null && ball.SynchronizedNow)
        {
            yield return new WaitUntil(() => ball == null || !ball.SynchronizedNow);
        }

        if (ball != null)
        {
            ball.stop();
        }

        LogLine($"AssignBall(slot={slot}) ok reason={reason}");
    }

    private bool TryAssignBallToFormationSlot(int slot, out string reason, out bool ownershipChanged)
    {
        ownershipChanged = false;
        reason = "unknown";
        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null || teamFacade.BallManager == null)
        {
            reason = "BallManager_unavailable";
            return false;
        }

        BallHandler ball = teamFacade.BallManager.Ball;
        if (ball == null)
        {
            reason = "Ball_null";
            return false;
        }

        AnimalFacade owner = GetAllyFacadeBySlot(slot);
        if (owner == null)
        {
            reason = $"slot{slot}_not_found";
            return false;
        }

        PhotonAvatarContainerChild avatar = owner.GetAvatar();
        if (avatar == null)
        {
            reason = "avatar_null";
            return false;
        }

        int ownerViewId = avatar.ViewID;
        if (teamFacade.BallManager.isHoldBall(ownerViewId)
            && teamFacade.BallManager.State.BallState == BallManager_State.BALL_STATE.HOLD)
        {
            reason = $"already_owned viewId={ownerViewId}";
            return true;
        }

        if (!teamFacade.BallManager.changeOwnership(ownerViewId, BallManager_State.BALL_STATE.HOLD))
        {
            reason = "changeOwnership_failed";
            return false;
        }

        ownershipChanged = true;

        if (avatar.tag.Equals(ConstData.PLAYER_TAG))
        {
            AnimalSelector_Manager selector = teamFacade.AnimalSelectorManager;
            if (selector != null)
            {
                selector.SetSelectAnimal(owner, avatar.tag);
            }
        }

        reason = $"viewId={avatar.ViewID}";
        return true;
    }

    private AnimalFacade GetAllyFacadeBySlot(int slot) => GoapSupportVerificationAllyHelper.GetFacadeBySlot(slot);

    private void TriggerAllyGoapReplan()
    {
        var agents = FindObjectsByType<GoapAgent>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var agent in agents)
        {
            if (agent == null)
            {
                continue;
            }

            agent.AbortCurrentPlan();
            count++;
        }

        LogLineStatic($"TriggerAllyGoapReplan agents={count}");
    }

    private void RestoreBaseline()
    {
        foreach (var snap in _baseline)
        {
            if (snap.Transform == null)
            {
                continue;
            }

            snap.Transform.position = snap.Position;
        }
    }

    private bool TryGetFieldContext(out GoapSupportLayoutFieldContext ctx) =>
        GoapSupportLayoutPatternLibrary.TryGetFieldContext(LayoutTuning, out ctx);

    private void OnDrawGizmosSelected()
    {
        if (_initialVerificationPattern == GoapSupportLayoutPatternId.Custom
            && _customSlotPlacements != null)
        {
            foreach (AllySlotPlacement entry in _customSlotPlacements)
            {
                if (entry == null || !entry.Apply)
                {
                    continue;
                }

                Gizmos.color = entry.Slot switch
                {
                    0 => Color.yellow,
                    1 => Color.cyan,
                    _ => Color.magenta,
                };
                Gizmos.DrawWireSphere(entry.Position, 0.45f);
                Gizmos.DrawSphere(entry.Position, 0.2f);
            }
        }

        if (!Application.isPlaying || !TryGetFieldContext(out GoapSupportLayoutFieldContext ctx))
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(ctx.OwnerAnchor, 0.35f);

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        for (int slot = 0; slot <= 2; slot++)
        {
            Vector3 target = CreateSupportAnglePositioning.SelectBestPosition(
                ctx.OwnerAnchor,
                slot,
                teamBB,
                CreateSupportAnglePositioning.CreateDefaultSettings());
            Gizmos.DrawWireSphere(target, 0.45f);
        }

        Gizmos.color = Color.white;
        foreach (var ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            Gizmos.DrawSphere(ally.Facade.transform.position, 0.25f);
        }
    }

    protected void LogLine(string message) => GoapActionVerificationSessionLog.Append(SummaryLogTag, message);

    private void LogLineStatic(string message) => GoapActionVerificationSessionLog.Append(SummaryLogTag, message);

    private static string Fmt(Vector3 v) => $"({v.x:F1},{v.y:F1},{v.z:F1})";

    private static string SlotPos(Dictionary<int, Vector3> map, int slot) =>
        map.TryGetValue(slot, out Vector3 pos) ? Fmt(pos) : "-";
}
