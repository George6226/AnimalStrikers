using System.Collections;
using UnityEngine;

/// <summary>
/// Phase M1: Main NPC 検証 Bootstrap のボール付与先。
/// </summary>
public enum GoapMainNpcVerifyBootstrapBallTarget
{
    /// <summary>M0: 敵へボール → サブ守備 GOAP 検証。</summary>
    EnemyForDefenseVerify = 0,

    /// <summary>M1: slot0 メイン NPC へボール → Pass/Shoot GOAP 検証。</summary>
    MainNpcForAttackVerify = 1,
}

/// <summary>
/// Phase M0/M1: Main NPC 検証 Play 開始時にボールを配置し GOAP 検証をすぐ始められるようにする。
/// </summary>
[DefaultExecutionOrder(-50)]
public class GoapMainNpcVerifyBootstrap : MonoBehaviour
{
    private const string LogTag = "GoapMainNpcVerifyBootstrap";

    [SerializeField] private bool _enabled = true;
    [Tooltip("OFF のときは SquadControl の Main Npc Goap Verify Mode が ON のときだけ動く")]
    [SerializeField] private bool _requireMainNpcVerifyMode = true;
    [SerializeField] private GoapMainNpcVerifyBootstrapBallTarget _ballTarget =
        GoapMainNpcVerifyBootstrapBallTarget.EnemyForDefenseVerify;
    [SerializeField] private int _enemyBallOwnerIndex;
    [SerializeField] private bool _applyEnemyLayoutOnStart = true;
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _enemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;
    [SerializeField] private float _spawnWaitTimeoutSeconds = 90f;
    [SerializeField] private float _ballAssignTimeoutSeconds = 10f;
    [SerializeField] private float _settleSecondsAfterAssign = 1.5f;
    [SerializeField] private bool _invalidateDefensivePositionFacts = true;
    [SerializeField] private bool _triggerGoapReplanAfterAssign = true;

    private Coroutine _bootstrapCoroutine;
    private bool _started;

    private void OnEnable()
    {
        if (GoapBatchVerifyEnvironment.IsActive)
        {
            GoapMainNpcVerifyEnvironment.MarkBootstrapComplete();
            return;
        }

        Log("component enabled");
        GoapDebugPlayBootstrap.SpawnReady += HandleSpawnReady;
        TryStartBootstrap();
    }

    private void OnDisable()
    {
        GoapDebugPlayBootstrap.SpawnReady -= HandleSpawnReady;
        if (_bootstrapCoroutine != null)
        {
            StopCoroutine(_bootstrapCoroutine);
            _bootstrapCoroutine = null;
        }
    }

    private void HandleSpawnReady()
    {
        TryStartBootstrap();
    }

    private void TryStartBootstrap()
    {
        if (_started || _bootstrapCoroutine != null || !isActiveAndEnabled)
        {
            return;
        }

        _bootstrapCoroutine = StartCoroutine(WaitAndBootstrapCoroutine());
    }

    private IEnumerator WaitAndBootstrapCoroutine()
    {
        float waitElapsed = 0f;
        while (waitElapsed < _spawnWaitTimeoutSeconds && !ShouldRun())
        {
            waitElapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        if (!ShouldRun())
        {
            Log($"aborted: ShouldRun=false after {waitElapsed:F1}s");
            FinishBootstrap();
            yield break;
        }

        _started = true;
        Log("bootstrap start");

        float spawnElapsed = 0f;
        while (spawnElapsed < _spawnWaitTimeoutSeconds && !HasMinimumFieldPlayers())
        {
            spawnElapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        if (!HasMinimumFieldPlayers())
        {
            Log($"aborted: fieldPlayers={CountFieldPlayers()} (timeout={_spawnWaitTimeoutSeconds:F0}s)");
            FinishBootstrap();
            yield break;
        }

        float enemyElapsed = 0f;
        if (_ballTarget == GoapMainNpcVerifyBootstrapBallTarget.EnemyForDefenseVerify)
        {
            while (enemyElapsed < _spawnWaitTimeoutSeconds && CountFieldEnemies() <= _enemyBallOwnerIndex)
            {
                enemyElapsed += 0.25f;
                yield return new WaitForSeconds(0.25f);
            }

            if (CountFieldEnemies() <= _enemyBallOwnerIndex)
            {
                Log($"aborted: fieldEnemies={CountFieldEnemies()} index={_enemyBallOwnerIndex} (timeout={_spawnWaitTimeoutSeconds:F0}s)");
                FinishBootstrap();
                yield break;
            }
        }

        if (_ballTarget == GoapMainNpcVerifyBootstrapBallTarget.EnemyForDefenseVerify
            && _applyEnemyLayoutOnStart)
        {
            ApplyEnemyLayout();
            SyncTeamBlackboardMemberPositions();
        }

        if (_ballTarget == GoapMainNpcVerifyBootstrapBallTarget.MainNpcForAttackVerify)
        {
            yield return AssignBallToMainNpcCoroutine();
        }
        else
        {
            yield return AssignBallToEnemyCoroutine();
        }

        if (_settleSecondsAfterAssign > 0f)
        {
            yield return new WaitForSeconds(_settleSecondsAfterAssign);
        }

        SyncTeamBlackboardMemberPositions();

        if (_invalidateDefensivePositionFacts
            && _ballTarget == GoapMainNpcVerifyBootstrapBallTarget.EnemyForDefenseVerify)
        {
            InvalidateAllyDefensivePositionFacts();
        }

        FinishBootstrap();

        if (_triggerGoapReplanAfterAssign)
        {
            TriggerAllyGoapReplan();
        }

        Log("bootstrap complete");
        _bootstrapCoroutine = null;
    }

    private void FinishBootstrap()
    {
        GoapMainNpcVerifyEnvironment.MarkBootstrapComplete();
        _bootstrapCoroutine = null;
    }

    private bool ShouldRun()
    {
        if (!_enabled || GoapBatchVerifyEnvironment.IsActive)
        {
            return false;
        }

        if (!_requireMainNpcVerifyMode)
        {
            return true;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        return squad != null && squad.MainNpcGoapVerifyModeActive;
    }

    private void ApplyEnemyLayout()
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts == null)
        {
            Log($"ApplyEnemyLayout({_enemyLayout}) skipped: GoapEnemyPositionDebugPatterns not found");
            return;
        }

        _enemyLayouts.ApplyPattern(_enemyLayout);
        Log($"ApplyEnemyLayout({_enemyLayout})");
    }

    private IEnumerator AssignBallToEnemyCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _ballAssignTimeoutSeconds && !IsBallAvailable())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsBallAvailable())
        {
            Log($"AssignEnemyBall(index={_enemyBallOwnerIndex}) failed: ball_unavailable");
            yield break;
        }

        if (!GoapDefenseVerificationBallHelper.TryAssignBallToEnemyIndex(
                _enemyBallOwnerIndex,
                out string reason,
                out bool ownershipChanged))
        {
            Log($"AssignEnemyBall(index={_enemyBallOwnerIndex}) failed: {reason}");
            yield break;
        }

        Log($"AssignEnemyBall(index={_enemyBallOwnerIndex}) ok reason={reason} changed={ownershipChanged}");
        SyncTeamBlackboardMemberPositions();
    }

    private IEnumerator AssignBallToMainNpcCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _ballAssignTimeoutSeconds && !IsBallAvailable())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsBallAvailable())
        {
            Log("AssignMainNpcBall failed: ball_unavailable");
            yield break;
        }

        int slot = ResolveMainNpcFormationSlot();
        if (!GoapDefenseVerificationBallHelper.TryAssignBallToAllyFormationSlot(
                slot,
                out string reason,
                out bool ownershipChanged))
        {
            Log($"AssignMainNpcBall(slot={slot}) failed: {reason}");
            yield break;
        }

        Log($"AssignMainNpcBall(slot={slot}) ok reason={reason} changed={ownershipChanged}");
        SyncTeamBlackboardMemberPositions();
    }

    private int ResolveMainNpcFormationSlot()
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        return squad != null ? squad.MainNpcFormationSlot : 0;
    }

    private static void TriggerAllyGoapReplan()
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        squad?.RefreshLocalSquadRolesForMainNpcVerify();

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

            squad?.ApplyGoapPilotConfiguration(goap.Agent, ally.Facade);
            goap.Agent.ResetPlanningStateForVerification();
            count++;
        }

        Log($"TriggerAllyGoapReplan agents={count}");
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

    private static void SyncTeamBlackboardMemberPositions()
    {
        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        var regist = teamFacade != null ? teamFacade.TeamRegist : null;
        if (teamBB == null || regist == null)
        {
            return;
        }

        var allyPositions = new System.Collections.Generic.List<Vector3>();
        foreach (AnimalFacade ally in regist.Allys)
        {
            if (ally != null)
            {
                allyPositions.Add(ally.transform.position);
            }
        }

        var enemyPositions = new System.Collections.Generic.List<Vector3>();
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

    private static bool IsBallAvailable()
    {
        var teamFacade = TeamFacade.Instance;
        return teamFacade != null
            && teamFacade.BallManager != null
            && teamFacade.BallManager.Ball != null
            && teamFacade.TeamBlackboard != null
            && teamFacade.TeamBlackboard.BallInfo.IsExistBall;
    }

    private static bool HasMinimumFieldPlayers() => CountFieldPlayers() >= 3;

    private static int CountFieldPlayers()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var ally in regist.Allys)
        {
            if (ally != null && !ally.IsGK())
            {
                count++;
            }
        }

        return count;
    }

    private static int CountFieldEnemies()
    {
        return GoapDefenseVerificationBallHelper.GetFieldEnemies().Count;
    }

    private static void Log(string message)
    {
        GoapActionVerificationSessionLog.Append(LogTag, message);
    }
}
