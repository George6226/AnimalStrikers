using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 6-H P2: <see cref="GoalkeeperNpcBrain"/> 専用バッチ検証（GOAP 外）。
/// シナリオ #0 敵脅威 → 味方 GK が TrackBall になることを GkDiag で記録する。
/// </summary>
public class GoalkeeperBatchVerificationSetup : MonoBehaviour
{
    private const GoalkeeperBatchScenarioId ActiveScenario =
        GoalkeeperBatchScenarioId.EnemyThreatTrackBall;

    [SerializeField] private bool _runBatchVerificationOnStart = true;
    [SerializeField] private float _batchWaitGameStateTimeoutSeconds = 120f;
    [SerializeField] private float _batchSettleSecondsAfterGameState = 2f;
    [SerializeField] private float _batchHoldSecondsPerScenario = 6f;
    [SerializeField] private bool _stopPlayModeWhenBatchEnds = true;
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;

    private Coroutine _batchCoroutine;

    private void Start()
    {
        if (_runBatchVerificationOnStart)
        {
            _batchCoroutine = StartCoroutine(RunBatchVerificationCoroutine());
        }
    }

    private void OnDestroy()
    {
        if (_batchCoroutine != null)
        {
            StopCoroutine(_batchCoroutine);
            _batchCoroutine = null;
        }
    }

    private IEnumerator RunBatchVerificationCoroutine()
    {
        GoalkeeperDiagnosticLog.SetEnabled(true);
        GoalkeeperDiagnosticLog.ResetSession();
        GoalkeeperDiagnosticLog.WriteBanner("BATCH_START scenario=EnemyThreat count=1");

        yield return WaitForGameStateCoroutine(_batchWaitGameStateTimeoutSeconds);
        if (!IsGameState())
        {
            GoalkeeperDiagnosticLog.WriteBanner("BATCH_ABORT GAME_STATE_TIMEOUT");
            FinishBatch();
            yield break;
        }

        if (_batchSettleSecondsAfterGameState > 0f)
        {
            yield return new WaitForSeconds(_batchSettleSecondsAfterGameState);
        }

        ApplyEnemyThreatScenario();

        if (_batchHoldSecondsPerScenario > 0f)
        {
            yield return new WaitForSeconds(_batchHoldSecondsPerScenario);
        }

        bool passed = EvaluateAllyGoalkeeperMode();
        if (passed)
        {
            GoalkeeperDiagnosticLog.WriteBanner("SCENARIO_PASS 1/1 mode=TrackBall");
            GoalkeeperDiagnosticLog.WriteBanner("SELECTION_TOTAL 1/1");
        }
        else
        {
            GoalkeeperDiagnosticLog.WriteBanner("SCENARIO_FAIL 0/1 mode!=TrackBall");
            GoalkeeperDiagnosticLog.WriteBanner("SELECTION_TOTAL 0/1");
        }

        GoalkeeperDiagnosticLog.WriteBanner("BATCH_COMPLETE");
        FinishBatch();
    }

    private void ApplyEnemyThreatScenario()
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts != null)
        {
            _enemyLayouts.ApplyPattern(GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner);
            GoalkeeperDiagnosticLog.Write("[GK_BATCH] enemy_layout=PressBallOwner");
        }

        if (GoapDefenseVerificationBallHelper.TryAssignBallToEnemyIndex(0, out string reason, out _))
        {
            GoalkeeperDiagnosticLog.Write($"[GK_BATCH] ball_assigned reason={reason}");
        }
        else
        {
            GoalkeeperDiagnosticLog.Write($"[GK_BATCH] ball_assign_failed reason={reason}");
        }
    }

    private static bool EvaluateAllyGoalkeeperMode()
    {
        var expectedMode = GoalkeeperBatchVerifyRules.GetExpectedMode(ActiveScenario);
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null || regist.Allys == null)
        {
            return false;
        }

        foreach (AnimalFacade facade in regist.Allys)
        {
            if (facade == null || !facade.IsGK())
            {
                continue;
            }

            GoalkeeperNpcBrain brain = facade.GetComponent<GoalkeeperNpcBrain>();
            if (brain == null)
            {
                continue;
            }

            bool matched = brain.CurrentMode == expectedMode;
            GoalkeeperDiagnosticLog.Write(
                $"[GK_BATCH] evaluate gk={facade.name} mode={brain.CurrentMode} expected={expectedMode} pass={matched}");
            return matched;
        }

        GoalkeeperDiagnosticLog.Write("[GK_BATCH] evaluate failed: ally_gk_not_found");
        return false;
    }

    private static bool IsGameState() =>
        StateManager.Instance != null
        && StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME);

    private static IEnumerator WaitForGameStateCoroutine(float timeoutSeconds)
    {
        float end = Time.realtimeSinceStartup + Mathf.Max(1f, timeoutSeconds);
        while (Time.realtimeSinceStartup < end)
        {
            if (IsGameState())
            {
                yield break;
            }

            yield return null;
        }
    }

    private void FinishBatch()
    {
#if UNITY_EDITOR
        if (_stopPlayModeWhenBatchEnds
            && GoalkeeperBatchVerifyEnvironment.IsActive
            && EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
#endif
    }
}
