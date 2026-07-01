using System;
using System.Collections;
using System.Collections.Generic;
using Game.Goap;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 相手ボール時の守備 GOAP 検証基盤（味方配置 + 敵ボール付与 + 本番選出）。
/// </summary>
public abstract class GoapDefenseActionVerificationSetup : MonoBehaviour
{
    protected virtual float ProductionSelectionPlanCostsTimeoutSeconds => 50f;

    protected abstract string SummaryLogTag { get; }
    protected abstract GoapDefenseActionUnderTest ActionUnderTest { get; }
    protected abstract IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation { get; }
    protected abstract string BatchVerificationBanner { get; }
    protected abstract string ProductionSelectionVerificationBanner { get; }

    protected virtual IGoapDefenseActionRuntimePassCriteria RuntimePassCriteria => null;

    protected virtual IGoapDefenseProductionSelectionExpectation DriveProductionSelectionExpectation =>
        ProductionSelectionExpectation;

    protected virtual GoapProductionSelectionResolveMode ProductionSelectionResolveModeAtApply =>
        GoapProductionSelectionResolveMode.FirstPlanCosts;

    protected virtual GoapProductionSelectionResolveMode ResolveProductionSelectionModeForPattern(
        GoapDefenseLayoutPatternId pattern) =>
        ProductionSelectionResolveModeAtApply;

    protected virtual bool SuppressAllyBallPickupDuringBatch => false;

    protected virtual GoapProductionSelectionResolveMode ResolveDriveProductionSelectionModeForPattern(
        GoapDefenseLayoutPatternId pattern) =>
        GoapProductionSelectionResolveMode.LastPlanCosts;

    [Header("検証モード")]
    [Tooltip("ON=DefensivePositioning の候補を ActionUnderTest のみにする（単体動作検証）")]
    [SerializeField] private bool _verificationOnlyDefenseAction = true;
    [Tooltip("ON=本番選出検証時も ActionUnderTest のみを候補にする（Phase 5 基本）")]
    [SerializeField] private bool _restrictCandidatesToActionUnderTest = true;
    [Tooltip("ON=パターン適用直後の PlanCosts で選出を検証")]
    [SerializeField] private bool _verifyProductionSelection = true;
    [SerializeField] private bool _triggerGoapReplanAfterApply = true;
    [Tooltip("パターン適用時に敵フィールドプレイヤーへボールを付与")]
    [SerializeField] private bool _assignBallToEnemyOnApply = true;
    [SerializeField] private int _enemyBallOwnerIndex;

    [Header("Layout Tuning (field ratio)")]
    [SerializeField] private GoapSupportLayoutTuning _layoutTuning = new GoapSupportLayoutTuning();
    [SerializeField] private float _ownerForwardRatio = 0.12f;
    [SerializeField] private float _clusterForwardRatio = 0.04f;
    [SerializeField] private float _clusterLateralRatio = 0.06f;

    [Header("一括検証")]
    [SerializeField] private bool _runBatchVerificationOnStart;
    [SerializeField] private float _batchHoldSecondsPerPattern = 8f;
    [SerializeField] private float _batchSettleSecondsAfterPatternApply = 3.5f;
    [SerializeField] private float _batchWaitGameStateTimeoutSeconds = 120f;
    [SerializeField] private float _batchSettleSecondsAfterGameState = 2f;
    [SerializeField] private bool _stopPlayModeWhenBatchEnds = true;
    [SerializeField] private int _batchPatternIndexStart = 2;
    [SerializeField] private int _batchPatternIndexEnd = 3;

    [Header("敵保持者自動ドライブ（追従検証）")]
    [SerializeField] private bool _enableEnemyOwnerAutoDrive;
    [SerializeField] private bool _verifyProductionSelectionDuringDrive;
    [SerializeField] private bool _verifyRuntimeFollowDuringBatch;
    [SerializeField] private GoapBallOwnerAutoDriveMode _enemyOwnerAutoDriveMode = GoapBallOwnerAutoDriveMode.ForwardBack;
    [SerializeField] private float _enemyOwnerAutoDriveIntensity = 0.85f;
    [SerializeField] private float _enemyOwnerAutoDriveAmplitudeRatio = 0.08f;
    [SerializeField] private float _enemyOwnerAutoDriveStartDelay = 3f;

    private readonly List<Snapshot> _baseline = new List<Snapshot>();
    private readonly List<EnemySnapshot> _enemyBaseline = new List<EnemySnapshot>();
    private Coroutine _batchCoroutine;
    private Coroutine _enemyDriveCoroutine;
    private int _productionSelectionSummaryOffset;
    private int _productionSelectionReadyLineCount;
    private int _productionSelectionPassCount;
    private int _productionSelectionEvalCount;
    private int _runtimePassSummaryOffset;
    private int _runtimePassDiagOffset;
    private int _runtimePassPassCount;
    private int _runtimePassEvalCount;
    private bool _lastAssignBallOwnershipChanged;

    private struct Snapshot
    {
        public Transform Transform;
        public Vector3 Position;
    }

    private struct EnemySnapshot
    {
        public Transform Transform;
        public Vector3 Position;
    }

    protected bool RestrictCandidatesToActionUnderTest => _restrictCandidatesToActionUnderTest;

    private GoapDefenseActionUnderTest EffectiveVerificationOnlyDefenseAction =>
        !_verifyProductionSelection
        && !_verifyProductionSelectionDuringDrive
        && !_verifyRuntimeFollowDuringBatch
        && (_verificationOnlyDefenseAction || _restrictCandidatesToActionUnderTest)
            ? ActionUnderTest
            : GoapDefenseActionUnderTest.None;

    private bool UsesProductionSelectionAtApply =>
        _verifyProductionSelection && !_verifyProductionSelectionDuringDrive;

    private bool UsesProductionSelectionDuringDrive => _verifyProductionSelectionDuringDrive;

    private bool UsesAnyProductionSelection =>
        UsesProductionSelectionAtApply || UsesProductionSelectionDuringDrive;

    private bool ShouldEvaluateRuntimePassDuringBatch =>
        RuntimePassCriteria != null
        && (EffectiveVerificationOnlyDefenseAction != GoapDefenseActionUnderTest.None
            || _verifyRuntimeFollowDuringBatch);

    private bool IsBatchCoroutineActive => _batchCoroutine != null;

    protected virtual GoapDefenseActionUnderTest ResolveDefenseActionFilterForPattern(
        GoapDefenseLayoutPatternId pattern)
    {
        if (!_restrictCandidatesToActionUnderTest
            && !(_verificationOnlyDefenseAction && !_verifyProductionSelection))
        {
            return GoapDefenseActionUnderTest.None;
        }

        return ActionUnderTest;
    }

    /// <summary>
    /// true のとき ready 判定で期待アクション一致まで待つ（#5/#6 など再プランで誤選出が先に出るパターン向け）。
    /// </summary>
    protected virtual bool RequiresExpectedActionMatchForReady(GoapDefenseLayoutPatternId pattern) =>
        false;

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
            return _layoutTuning;
        }
    }

    private void OnEnable()
    {
        TeammateNpcDefensePlanning.SetVerificationOnlyDefenseAction(EffectiveVerificationOnlyDefenseAction);
    }

    private void OnDisable()
    {
        StopActiveEnemyDrive();
        TeammateNpcDefensePlanning.SetVerificationOnlyDefenseAction(GoapDefenseActionUnderTest.None);
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
        }
    }

    protected virtual void ApplyCompanionVerificationState(GoapDefenseLayoutPatternId pattern)
    {
    }

    private IEnumerator RunBatchVerificationWhenReady()
    {
        try
        {
            yield return WaitUntilReadyForLayoutApply(
                GoapBatchVerifyEnvironment.ResolveTimeout(90f, 180f));
            if (!IsReadyForLayoutApply())
            {
                string blocked = GoapBatchVerifyLayoutReadiness.DescribeBlocked(LayoutTuning);
                LogLine($"RunBatchVerification aborted: layout apply not ready ({blocked})");
                GoapDiagnosticLog.WriteBanner($"BATCH_ABORT NOT_READY {blocked}");
                yield break;
            }

            ResetDebugLogSession(ProductionSelectionVerificationBanner);
            if (_baseline.Count == 0)
            {
                CaptureBaseline();
            }

            _productionSelectionPassCount = 0;
            _productionSelectionEvalCount = 0;
            _runtimePassPassCount = 0;
            _runtimePassEvalCount = 0;

            List<GoapDefenseLayoutPatternId> patterns = GoapDefenseLayoutPatternCatalog.BuildRange(
                _batchPatternIndexStart,
                _batchPatternIndexEnd);
            int total = patterns.Count;
            if (total == 0)
            {
                LogLine("RunBatchVerification aborted: no patterns in selected range");
                GoapDiagnosticLog.WriteBanner("BATCH_ABORT EMPTY_RANGE");
                yield break;
            }

            LogLine(
                $"RunBatchVerification start range={_batchPatternIndexStart}..{_batchPatternIndexEnd} " +
                $"count={total} productionSelectionAtApply={UsesProductionSelectionAtApply} " +
                $"driveSelection={UsesProductionSelectionDuringDrive} " +
                $"runtimeFollow={ShouldEvaluateRuntimePassDuringBatch} " +
                $"actionFilter={ActionUnderTest}");
            GoapDiagnosticLog.WriteBanner(
                $"BATCH_START range={_batchPatternIndexStart}-{_batchPatternIndexEnd} count={total}");

            GoapDefenseVerificationBallHelper.SuppressAllyBallPickup =
                _assignBallToEnemyOnApply && SuppressAllyBallPickupDuringBatch;

            yield return WaitForGameStateCoroutine(
                GoapBatchVerifyEnvironment.ResolveTimeout(_batchWaitGameStateTimeoutSeconds, 180f));
            if (!IsGameState())
            {
                LogLine("RunBatchVerification aborted: GAME state timeout");
                GoapDiagnosticLog.WriteBanner("BATCH_ABORT GAME_STATE_TIMEOUT");
                yield break;
            }

            if (_batchSettleSecondsAfterGameState > 0f)
            {
                yield return new WaitForSeconds(_batchSettleSecondsAfterGameState);
            }

            for (int index = 0; index < total; index++)
            {
                GoapDefenseLayoutPatternId pattern = patterns[index];
                _activeBatchPattern = pattern;
                WriteBatchPatternBoundary("BEGIN", pattern, index + 1, total);
                GoapDefenseActionUnderTest actionFilter = ResolveDefenseActionFilterForPattern(pattern);
                TeammateNpcDefensePlanning.SetVerificationOnlyDefenseAction(actionFilter);

                yield return ApplyVerificationPatternAndWait(pattern);

                if (_batchSettleSecondsAfterPatternApply > 0f && UsesProductionSelectionAtApply)
                {
                    yield return SettleAfterPatternApplyCoroutine(pattern);
                }

                if (UsesProductionSelectionAtApply)
                {
                    GoapProductionSelectionResolveMode resolveMode =
                        ResolveProductionSelectionModeForPattern(pattern);
                    yield return WaitForProductionPlanCostsCoroutine(
                        ProductionSelectionPlanCostsTimeoutSeconds,
                        ProductionSelectionExpectation,
                        resolveMode);
                    EvaluateProductionSelectionForPattern(
                        pattern,
                        index + 1,
                        total,
                        ProductionSelectionExpectation,
                        resolveMode,
                        "apply");
                }
                else
                {
                    float holdSeconds = _batchHoldSecondsPerPattern;
                    if (holdSeconds > 0f)
                    {
                        yield return HoldPatternObservationCoroutine(pattern, holdSeconds);
                    }

                    if (UsesProductionSelectionDuringDrive)
                    {
                        GoapProductionSelectionResolveMode driveResolveMode =
                            ResolveDriveProductionSelectionModeForPattern(pattern);
                        yield return WaitForProductionPlanCostsCoroutine(
                            ProductionSelectionPlanCostsTimeoutSeconds,
                            DriveProductionSelectionExpectation,
                            driveResolveMode);
                        EvaluateProductionSelectionForPattern(
                            pattern,
                            index + 1,
                            total,
                            DriveProductionSelectionExpectation,
                            driveResolveMode,
                            "drive");
                    }

                    if (ShouldEvaluateRuntimePassDuringBatch)
                    {
                        EvaluateRuntimePassForPattern(pattern, index + 1, total);
                    }
                }

                WriteBatchPatternBoundary("END", pattern, index + 1, total);
            }

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

            GoapDiagnosticLog.WriteBanner("BATCH_COMPLETE");
            LogLine("RunBatchVerification complete");
        }
        finally
        {
            StopActiveEnemyDrive();
            GoapDefenseVerificationBallHelper.SuppressAllyBallPickup = false;
            TeammateNpcDefensePlanning.SetVerificationOnlyDefenseAction(GoapDefenseActionUnderTest.None);
#if UNITY_EDITOR
            if (_stopPlayModeWhenBatchEnds && GoapBatchVerifyEnvironment.IsActive)
            {
                EditorApplication.isPlaying = false;
            }
#endif
        }
    }

    private IEnumerator ApplyVerificationPatternAndWait(GoapDefenseLayoutPatternId pattern)
    {
        if (!TryApplyVerificationPatternLayout(pattern))
        {
            yield break;
        }

        if (pattern != GoapDefenseLayoutPatternId.Baseline)
        {
            ApplyCompanionVerificationState(pattern);
            if (pattern == GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane)
            {
                ReapplyAllyTargetsAnchoredToEnemyOwner(pattern);
            }

            SyncTeamBlackboardMemberPositions();
        }

        if (pattern != GoapDefenseLayoutPatternId.Baseline && _assignBallToEnemyOnApply)
        {
            yield return AssignBallToEnemyCoroutine(
                GoapDefenseLayoutPatternLibrary.GetEnemyBallOwnerIndex(pattern));
            yield return null;
            SyncTeamBlackboardMemberPositions();
        }

        if (pattern != GoapDefenseLayoutPatternId.Baseline)
        {
            InvalidateAllyDefensivePositionFacts();
            yield return null;
            TriggerAllyGoapReplan();
            yield return null;
            TriggerAllyGoapReplan();
        }
    }

    private IEnumerator SettleAfterPatternApplyCoroutine(GoapDefenseLayoutPatternId pattern)
    {
        int enemyBallOwner = GoapDefenseLayoutPatternLibrary.GetEnemyBallOwnerIndex(pattern);
        float elapsed = 0f;
        while (elapsed < _batchSettleSecondsAfterPatternApply)
        {
            if (_assignBallToEnemyOnApply)
            {
                GoapDefenseVerificationBallHelper.TryAssignBallToEnemyIndex(
                    enemyBallOwner,
                    out _,
                    out _);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ReapplyAllyTargetsAnchoredToEnemyOwner(GoapDefenseLayoutPatternId pattern)
    {
        AnimalFacade owner = GoapDefenseVerificationBallHelper.GetEnemyByIndex(_enemyBallOwnerIndex);
        if (owner == null || !TryGetFieldContext(out GoapSupportLayoutFieldContext ctx))
        {
            return;
        }

        Vector3 anchor = owner.transform.position;
        Dictionary<int, Vector3> allyTargets = GoapDefenseLayoutPatternLibrary.ComputeAllyTargets(
            pattern,
            ctx,
            LayoutTuning,
            anchor);
        ApplyAllyTargets(allyTargets);
        ApplySecondaryEnemyPositions(pattern, ctx, anchor);
        LogLine($"ReapplyAllyTargets({pattern}) anchor={Fmt(anchor)} slot0={SlotPos(allyTargets, 0)}");
    }

    private bool TryApplyVerificationPatternLayout(GoapDefenseLayoutPatternId pattern)
    {
        GoapDefenseActionUnderTest actionFilter = ResolveDefenseActionFilterForPattern(pattern);
        TeammateNpcDefensePlanning.SetVerificationOnlyDefenseAction(actionFilter);

        if (pattern == GoapDefenseLayoutPatternId.Baseline)
        {
            RestoreBaseline();
            LogLine($"ApplyVerificationPattern({pattern}) restored allies={_baseline.Count} enemies={_enemyBaseline.Count}");
            return true;
        }

        if (!TryGetFieldContext(out GoapSupportLayoutFieldContext ctx))
        {
            LogLine($"ApplyVerificationPattern({pattern}) failed: TeamBlackboard unavailable");
            return false;
        }

        Dictionary<int, Vector3> allyTargets = GoapDefenseLayoutPatternLibrary.ComputeAllyTargets(
            pattern,
            ctx,
            LayoutTuning);
        ApplyAllyTargets(allyTargets);

        Vector3 enemyOwnerTarget = GoapDefenseLayoutPatternLibrary.ComputeEnemyOwnerTarget(
            pattern,
            ctx,
            LayoutTuning);
        ApplyEnemyOwnerPosition(enemyOwnerTarget);
        ApplySecondaryEnemyPositions(pattern, ctx);
        SyncTeamBlackboardMemberPositions();
        InvalidateAllyDefensivePositionFacts();

        LogLine(
            $"ApplyVerificationPattern({pattern}) enemyOwner={Fmt(enemyOwnerTarget)} " +
            $"filter={actionFilter} " +
            $"slot0={SlotPos(allyTargets, 0)} slot1={SlotPos(allyTargets, 1)} slot2={SlotPos(allyTargets, 2)}");
        return true;
    }

    private void EvaluateProductionSelectionForPattern(
        GoapDefenseLayoutPatternId pattern,
        int index,
        int total,
        IGoapDefenseProductionSelectionExpectation expectation,
        GoapProductionSelectionResolveMode resolveMode,
        string phaseLabel)
    {
        if (pattern == GoapDefenseLayoutPatternId.Baseline)
        {
            LogLine($"ProductionSelection SKIP {index}/{total} #{GetPatternNumber(pattern)} {pattern}");
            return;
        }

        List<string> lines = ReadProductionSelectionLines();
        GoapDefenseProductionSelectionEvaluationResult result = GoapDefenseProductionSelectionEvaluator.EvaluatePattern(
            pattern,
            expectation,
            lines,
            GoapSupportVerificationAllyHelper.ResolvePlayerIdForSlot,
            resolveMode);

        if (result.EvalCount == 0)
        {
            LogLine($"ProductionSelection SKIP {index}/{total} #{GetPatternNumber(pattern)} {pattern} (no eval slots)");
            return;
        }

        if (result.PatternPass)
        {
            _productionSelectionPassCount++;
        }

        _productionSelectionEvalCount++;
        string verdict = result.PatternPass ? "PASS" : "FAIL";
        LogLine(
            $"ProductionSelection {verdict} {index}/{total} #{GetPatternNumber(pattern)} {pattern} " +
            $"phase={phaseLabel} mode={resolveMode} {result.PassCount}/{result.EvalCount} {result.DetailText}");
        GoapDiagnosticLog.WriteBanner(
            $"SELECTION_{verdict} #{GetPatternNumber(pattern)} {pattern} {result.PassCount}/{result.EvalCount}");
    }

    private IEnumerator WaitForProductionPlanCostsCoroutine(
        float timeoutSeconds,
        IGoapDefenseProductionSelectionExpectation expectation,
        GoapProductionSelectionResolveMode resolveMode)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            List<string> lines = GoapActionVerificationSessionLog.ReadLinesSince(_productionSelectionSummaryOffset);
            if (HasReadyProductionSelection(lines, expectation, resolveMode))
            {
                _productionSelectionReadyLineCount = GoapActionVerificationSessionLog.CountLines();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        LogLine($"WaitForProductionPlanCosts timeout={timeoutSeconds:F0}s");
    }

    private List<string> ReadProductionSelectionLines()
    {
        List<string> lines = GoapActionVerificationSessionLog.ReadLinesSince(_productionSelectionSummaryOffset);
        if (_productionSelectionReadyLineCount <= _productionSelectionSummaryOffset)
        {
            return lines;
        }

        int maxCount = _productionSelectionReadyLineCount - _productionSelectionSummaryOffset;
        if (lines.Count <= maxCount)
        {
            return lines;
        }

        return lines.GetRange(0, maxCount);
    }

    private bool HasReadyProductionSelection(
        IList<string> lines,
        IGoapDefenseProductionSelectionExpectation expectation,
        GoapProductionSelectionResolveMode resolveMode)
    {
        if (expectation == null)
        {
            return HasPlanCostsForAllSlots(lines);
        }

        for (int slot = 0; slot <= 2; slot++)
        {
            if (!expectation.TryGetExpectation(
                    _activeBatchPattern,
                    slot,
                    out string expected,
                    out bool shouldEvaluate)
                || !shouldEvaluate)
            {
                continue;
            }

            string expectedForReady = RequiresExpectedActionMatchForReady(_activeBatchPattern)
                ? expected
                : null;

            if (!GoapDefenseProductionSelectionEvaluator.IsSlotSelectionReady(
                    lines,
                    slot,
                    GoapSupportVerificationAllyHelper.ResolvePlayerIdForSlot,
                    resolveMode,
                    expectedForReady))
            {
                return false;
            }
        }

        return true;
    }

    private GoapDefenseLayoutPatternId _activeBatchPattern;

    private static bool HasPlanCostsForAllSlots(IList<string> lines)
    {
        bool[] found = new bool[3];
        foreach (string line in lines)
        {
            if (!line.Contains("[GOAP_SUMMARY]") || !line.Contains("PlanCosts("))
            {
                continue;
            }

            for (int slot = 0; slot <= 2; slot++)
            {
                if (line.Contains($"slot={slot},") || line.Contains($"slot={slot} "))
                {
                    found[slot] = true;
                }
            }
        }

        return found[0] && found[1] && found[2];
    }

    private void WriteBatchPatternBoundary(string phase, GoapDefenseLayoutPatternId pattern, int index, int total)
    {
        if (phase == "BEGIN")
        {
            if (UsesAnyProductionSelection)
            {
                _productionSelectionSummaryOffset = GoapActionVerificationSessionLog.CountLines();
                _productionSelectionReadyLineCount = 0;
            }

            if (ShouldEvaluateRuntimePassDuringBatch)
            {
                _runtimePassSummaryOffset = GoapActionVerificationSessionLog.CountLines();
                _runtimePassDiagOffset = GoapDiagnosticLog.CountLines();
            }
        }

        string title = $"BATCH_{phase} {index}/{total} #{GetPatternNumber(pattern)} {pattern}";
        GoapDiagnosticLog.WriteBanner(title);
        LogLine(title);
    }

    private void EvaluateRuntimePassForPattern(
        GoapDefenseLayoutPatternId pattern,
        int index,
        int total)
    {
        if (RuntimePassCriteria == null)
        {
            return;
        }

        List<string> summaryLines = GoapActionVerificationSessionLog.ReadLinesSince(_runtimePassSummaryOffset);
        List<string> diagLines = GoapDiagnosticLog.ReadLinesSince(_runtimePassDiagOffset);
        GoapDefenseActionRuntimePassResult result = GoapDefenseActionRuntimePassEvaluator.EvaluatePattern(
            pattern,
            RuntimePassCriteria,
            diagLines,
            summaryLines,
            GoapSupportVerificationAllyHelper.ResolvePlayerIdForSlot);

        if (!result.ShouldEvaluate)
        {
            LogLine($"RuntimePass SKIP {index}/{total} #{GetPatternNumber(pattern)} {pattern} ({result.DetailText})");
            return;
        }

        _runtimePassEvalCount++;
        if (result.PatternPass)
        {
            _runtimePassPassCount++;
        }

        string verdict = result.PatternPass ? "PASS" : "FAIL";
        LogLine($"RuntimePass {verdict} {index}/{total} #{GetPatternNumber(pattern)} {pattern} {result.DetailText}");
        GoapDiagnosticLog.WriteBanner($"RUNTIME_{verdict} {index}/{total} #{GetPatternNumber(pattern)} {pattern}");
    }

    private IEnumerator HoldPatternObservationCoroutine(GoapDefenseLayoutPatternId pattern, float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        bool driveEnabled = _enableEnemyOwnerAutoDrive
            && pattern != GoapDefenseLayoutPatternId.Baseline
            && IsGameState();
        GoapBallOwnerAutoDriveMode driveMode = ResolveEnemyOwnerAutoDriveMode(pattern);

        if (driveEnabled && driveMode != GoapBallOwnerAutoDriveMode.None)
        {
            float ampRatio = ResolveEnemyOwnerAutoDriveAmplitudeRatio(pattern);
            LogLine(
                $"EnemyOwnerAutoDrive start pattern={pattern} mode={driveMode} ampRatio={ampRatio:F2} " +
                $"duration={duration:F1}s delay={_enemyOwnerAutoDriveStartDelay:F1}s");
            GoapDiagnosticLog.WriteBanner(
                $"AUTO_DRIVE_BEGIN #{GetPatternNumber(pattern)} {pattern} {driveMode} amp={ampRatio:F2}");
        }

        float elapsed = 0f;
        Vector3 driveAnchor = Vector3.zero;
        bool anchorSet = false;

        while (elapsed < duration)
        {
            if (driveEnabled
                && driveMode != GoapBallOwnerAutoDriveMode.None
                && elapsed >= _enemyOwnerAutoDriveStartDelay)
            {
                AnimalFacade enemyOwner = GoapDefenseVerificationBallHelper.GetEnemyByIndex(_enemyBallOwnerIndex);
                if (enemyOwner != null)
                {
                    if (!anchorSet)
                    {
                        driveAnchor = enemyOwner.transform.position;
                        anchorSet = true;
                    }

                    float driveElapsed = elapsed - _enemyOwnerAutoDriveStartDelay;
                    float driveDuration = Mathf.Max(duration - _enemyOwnerAutoDriveStartDelay, 0.01f);
                    Vector3 target = ComputeEnemyOwnerDriveTarget(
                        enemyOwner,
                        driveAnchor,
                        driveElapsed,
                        driveDuration,
                        driveMode,
                        pattern);
                    GoapDebugAnimalMotor.TryMoveToward(enemyOwner, target, _enemyOwnerAutoDriveIntensity);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopEnemyOwnerDrive();

        if (driveEnabled && driveMode != GoapBallOwnerAutoDriveMode.None)
        {
            LogLine($"EnemyOwnerAutoDrive end pattern={pattern}");
            GoapDiagnosticLog.WriteBanner($"AUTO_DRIVE_END #{GetPatternNumber(pattern)} {pattern}");
        }
    }

    private GoapBallOwnerAutoDriveMode ResolveEnemyOwnerAutoDriveMode(GoapDefenseLayoutPatternId pattern)
    {
        if (GoapDefenseLayoutDrivePatternLibrary.TryGetAutoDriveOverride(pattern, out GoapBallOwnerAutoDriveMode overrideMode))
        {
            return overrideMode;
        }

        return _enemyOwnerAutoDriveMode;
    }

    private float ResolveEnemyOwnerAutoDriveAmplitudeRatio(GoapDefenseLayoutPatternId pattern) =>
        GoapDefenseLayoutDrivePatternLibrary.ResolveAmplitudeRatio(
            pattern,
            _enemyOwnerAutoDriveAmplitudeRatio,
            _enemyOwnerAutoDriveAmplitudeRatio);

    private Vector3 ComputeEnemyOwnerDriveTarget(
        AnimalFacade enemyOwner,
        Vector3 anchor,
        float driveElapsed,
        float driveDuration,
        GoapBallOwnerAutoDriveMode driveMode,
        GoapDefenseLayoutPatternId pattern)
    {
        if (!TryGetFieldContext(out GoapSupportLayoutFieldContext ctx) || enemyOwner == null)
        {
            return anchor;
        }

        float amp = ctx.FieldLength * ResolveEnemyOwnerAutoDriveAmplitudeRatio(pattern);
        float phase = driveDuration > 0.01f ? driveElapsed / driveDuration : 0f;
        float wave = Mathf.Sin(phase * Mathf.PI * 2f);
        Vector3 pressDir = -ctx.ToGoal;

        return driveMode switch
        {
            GoapBallOwnerAutoDriveMode.Forward => enemyOwner.transform.position + pressDir * amp,
            GoapBallOwnerAutoDriveMode.ForwardBack => anchor + pressDir * (wave * amp),
            GoapBallOwnerAutoDriveMode.LateralRight => anchor + ctx.Right * (wave * amp),
            GoapBallOwnerAutoDriveMode.LateralLeft => anchor - ctx.Right * (wave * amp),
            _ => anchor,
        };
    }

    private void StopEnemyOwnerDrive()
    {
        AnimalFacade enemyOwner = GoapDefenseVerificationBallHelper.GetEnemyByIndex(_enemyBallOwnerIndex);
        if (enemyOwner != null)
        {
            GoapDebugAnimalMotor.Stop(enemyOwner);
        }
    }

    private void StopActiveEnemyDrive()
    {
        if (_enemyDriveCoroutine != null)
        {
            StopCoroutine(_enemyDriveCoroutine);
            _enemyDriveCoroutine = null;
        }

        StopEnemyOwnerDrive();
    }

    private IEnumerator WaitUntilReadyForLayoutApply(float timeoutSeconds)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSeconds && !IsReadyForLayoutApply())
        {
            elapsed += Time.deltaTime;
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

        if (!IsGameState()
            && GoapBatchVerifyEnvironment.IsActive
            && StateManager.Instance != null)
        {
            StateManager.Instance.changeStateLocal(StateManager.STATE_KIND.GAME);
            LogLine("WaitForGameState batch verify fallback: forced GAME");
        }
    }

    private static bool IsGameState() =>
        StateManager.Instance != null
        && StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME);

    private bool IsReadyForLayoutApply() => GoapBatchVerifyLayoutReadiness.IsReady(LayoutTuning);

    private bool TryGetFieldContext(out GoapSupportLayoutFieldContext ctx) =>
        GoapDefenseLayoutPatternLibrary.TryGetFieldContext(LayoutTuning, out ctx);

    private void CaptureBaseline()
    {
        _baseline.Clear();
        foreach (AnimalFacade ally in GoapSupportVerificationAllyHelper.GetFieldAllies())
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

        _enemyBaseline.Clear();
        foreach (AnimalFacade enemy in GoapDefenseVerificationBallHelper.GetFieldEnemies())
        {
            if (enemy == null)
            {
                continue;
            }

            _enemyBaseline.Add(new EnemySnapshot
            {
                Transform = enemy.transform,
                Position = enemy.transform.position,
            });
        }
    }

    private void RestoreBaseline()
    {
        foreach (Snapshot snap in _baseline)
        {
            if (snap.Transform != null)
            {
                snap.Transform.position = snap.Position;
            }
        }

        foreach (EnemySnapshot snap in _enemyBaseline)
        {
            if (snap.Transform != null)
            {
                snap.Transform.position = snap.Position;
            }
        }
    }

    private void ApplyAllyTargets(Dictionary<int, Vector3> targets)
    {
        foreach (GoapSupportVerificationAllyHelper.AllySlot ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            if (!targets.TryGetValue(ally.Slot, out Vector3 pos))
            {
                continue;
            }

            pos.y = ally.Facade.transform.position.y;
            ally.Facade.transform.position = pos;
        }
    }

    private void ApplyEnemyOwnerPosition(Vector3 target)
    {
        AnimalFacade owner = GoapDefenseVerificationBallHelper.GetEnemyByIndex(_enemyBallOwnerIndex);
        if (owner == null)
        {
            return;
        }

        target.y = owner.transform.position.y;
        owner.transform.position = target;
    }

    private void ApplySecondaryEnemyPositions(
        GoapDefenseLayoutPatternId pattern,
        GoapSupportLayoutFieldContext ctx,
        Vector3? enemyOwnerAnchor = null)
    {
        List<AnimalFacade> enemies = GoapDefenseVerificationBallHelper.GetFieldEnemies();
        if (enemies.Count <= 1)
        {
            return;
        }

        AnimalFacade secondary = enemies[1];
        if (secondary == null)
        {
            return;
        }

        if (!GoapDefenseLayoutPatternLibrary.TryGetSecondaryEnemyTarget(
                1,
                pattern,
                ctx,
                LayoutTuning,
                out Vector3 target,
                enemyOwnerAnchor))
        {
            AnimalFacade owner = enemies[0];
            if (owner == null)
            {
                return;
            }

            target = owner.transform.position;
            target.y = secondary.transform.position.y;
            secondary.transform.position = target;
            LogLine($"ApplySecondaryEnemy({pattern}) index=1 collapsed=owner pos={Fmt(target)}");
            return;
        }

        target.y = secondary.transform.position.y;
        secondary.transform.position = target;
        LogLine($"ApplySecondaryEnemy({pattern}) index=1 pos={Fmt(target)}");
    }

    private static void SyncTeamBlackboardMemberPositions()
    {
        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        var regist = teamFacade != null ? teamFacade.TeamRegist : null;
        if (teamBB == null || regist == null)
        {
            return;
        }

        List<Vector3> allyPositions = new();
        foreach (AnimalFacade ally in regist.Allys)
        {
            if (ally != null)
            {
                allyPositions.Add(ally.transform.position);
            }
        }

        List<Vector3> enemyPositions = new();
        foreach (AnimalFacade enemy in regist.Enemies)
        {
            if (enemy != null)
            {
                enemyPositions.Add(enemy.transform.position);
            }
        }

        teamBB.BasicInfo.Update(enemyPositions, allyPositions);

        if (teamFacade.BallManager != null
            && teamBB.BallInfo.BallOwnerID >= 0
            && teamFacade.BallManager.TryResolveBallOwnerWorldPosition(
                teamBB.BallInfo.BallOwnerID,
                out Vector3 ownerPos))
        {
            teamBB.BallInfo.updateBallOwnerPosition(ownerPos);
        }
    }

    private IEnumerator AssignBallToEnemyCoroutine(int enemyIndex)
    {
        _lastAssignBallOwnershipChanged = false;
        const float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout && !IsBallAvailable())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!GoapDefenseVerificationBallHelper.TryAssignBallToEnemyIndex(
                enemyIndex,
                out string reason,
                out bool ownershipChanged))
        {
            LogLine($"AssignEnemyBall(index={enemyIndex}) failed: {reason}");
            yield break;
        }

        _lastAssignBallOwnershipChanged = ownershipChanged;
        LogLine($"AssignEnemyBall(index={enemyIndex}) ok reason={reason}");
        SyncTeamBlackboardMemberPositions();
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

    private static void TriggerAllyGoapReplan()
    {
        if (GoapBatchVerifyEnvironment.IsActive)
        {
            var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            squad?.RefreshLocalSquadRolesForBatchVerification();
        }

        int count = 0;
        foreach (GoapSupportVerificationAllyHelper.AllySlot ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            if (ally.Facade == null)
            {
                continue;
            }

            AnimalGoapBrainComponents goap = AnimalGoapBrainComponents.Resolve(ally.Facade);
            var router = ally.Facade.GetComponent<AnimalControlBrainRouter>();
            var assignment = ally.Facade.GetComponent<AnimalControlAssignment>();
            if (router != null && assignment != null)
            {
                router.ApplyRole(assignment.Role);
            }
            else
            {
                goap.SetActive(true);
            }

            if (goap.Agent == null)
            {
                continue;
            }

            var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            squad?.ApplyGoapPilotConfiguration(goap.Agent);
            goap.Agent.ResetPlanningStateForVerification();
            count++;
        }

        GoapActionVerificationSessionLog.Append("GOAP_DEFENSE_SETUP", $"TriggerAllyGoapReplan agents={count}");
    }

    private static void InvalidateAllyDefensivePositionFacts()
    {
        foreach (AnimalFacade ally in GoapSupportVerificationAllyHelper.GetFieldAllies())
        {
            if (ally == null)
            {
                continue;
            }

            PlayerBlackboard bb = ally.GetComponentInChildren<PlayerBlackboard>();
            if (bb == null)
            {
                continue;
            }

            bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true"), false);
            bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "false"), true);
        }
    }

    private void ResetDebugLogSession(string banner)
    {
        GoapActionVerificationSessionLog.ResetSession(SummaryLogTag, banner);
        if (!string.IsNullOrEmpty(banner))
        {
            GoapDiagnosticLog.WriteBanner(banner);
        }
    }

    private static int GetPatternNumber(GoapDefenseLayoutPatternId pattern) =>
        GoapDefenseLayoutPatternCatalog.GetNumber(pattern);

    protected void LogLine(string message) => GoapActionVerificationSessionLog.Append(SummaryLogTag, message);

    private static string Fmt(Vector3 v) => $"({v.x:F1},{v.y:F1},{v.z:F1})";

    private static string SlotPos(Dictionary<int, Vector3> targets, int slot) =>
        targets.TryGetValue(slot, out Vector3 pos) ? Fmt(pos) : "n/a";
}
