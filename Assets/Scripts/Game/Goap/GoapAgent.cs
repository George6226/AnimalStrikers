using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using Game.Goap;
using Game.Goap.Goals;

// GOAPエージェント - 目標指向行動計画を実行する
public class GoapAgent : MonoBehaviour
{
    private const string SummaryTag = "GOAP_SUMMARY";
    private static string _summaryLogFilePath;
    private static bool _summaryLogInitialized;

    /// <summary>GoapSummary_latest.txt を外部で初期化済みにする（バッチ検証のログリセット用）。</summary>
    public static void MarkSummaryLogSessionActive()
    {
        if (GoapRuntimeDiagnostics.Level < GoapDiagnosticLevel.Summary)
        {
            GoapRuntimeDiagnostics.SetLevel(GoapDiagnosticLevel.Summary);
        }
        string dir = Path.Combine(Application.dataPath, "DebugLog");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _summaryLogFilePath = Path.Combine(dir, "GoapSummary_latest.txt");
        _summaryLogInitialized = true;
    }

    // === 設定 ===
    [Header("GOAP設定")]
    [SerializeField] private PlayerBlackboard _playerBlackboard;    // プレイヤーブラックボード
    [SerializeField] private List<GoapActionSO> _availableActions;  // 利用可能なアクション
    [SerializeField] private List<GoapGoalSO> _availableGoals;      // 利用可能なゴール
    [SerializeField] private float _planningInterval = 20.0f;        // プラン再生成間隔
    [Tooltip("SHOOT/PASS・所属未確定など遷移中の NoGoal 待機（秒）")]
    [SerializeField] private float _transitionNoGoalIdleSeconds = 0.35f;
    [Tooltip("安定局面で有効ゴールが無いときの NoGoal 待機（秒）。_planningInterval とは別。")]
    [SerializeField] private float _defaultNoGoalIdleSeconds = 0.6f;
    [Tooltip("パス受け完了直後の NoGoal 待機（秒）")]
    [SerializeField] private float _postReceiveNoGoalIdleSeconds = 0.12f;
    [Tooltip("パス受け完了後、この秒数は遷移優先で再計画する")]
    [SerializeField] private float _postReceiveTransitionWindowSeconds = 1.25f;
    [Tooltip("ShootAtGoal 完了直後の強制守備プラン猶予（秒）。ボール SHOOT 状態が既に終わっていても NoGoal を防ぐ")]
    [SerializeField] private float _postShootDefenseGraceSeconds = 0.75f;
    [Tooltip("PassToTeammate 完了直後の TeamBallSupport 猶予（秒）。IsAchievable 同期ズレで NoGoal になるのを防ぐ")]
    [SerializeField] private float _postPassSupportGraceSeconds = 0.75f;
    [Tooltip("戦術移動 ActionSkipped 直後の再選出猶予（秒）。ボール遷移中の SelectBestGoal=null を防ぐ")]
    [SerializeField] private float _tacticalContextGraceSeconds = 0.5f;
    [SerializeField] private bool _debugMode = false;               // デバッグモード
    
    // === 内部状態 ===
    private Queue<GoapActionRuntime> _currentPlan;                  // 現在の実行プラン
    private GoapActionRuntime _currentAction;                       // 現在実行中のアクション
    private AStarPlanner _planner;                                  // A*プランナー
    private float _lastPlanningTime;                                // 最後のプラン生成時間
    private bool _isPlanning;                                       // プラン生成中か
    private GoapGoalSO _currentGoal;                                // 現在のゴール
    
    private bool _planFailed;                                       // プラン失敗フラグ
    private string _lastReplanReason = "None";
    private string _lastPlanSummary = "-";
    private string _lastSelectedGoalName = "-";
    private string _lastFailureCategory = "None";
    private string _lastFailureDetails = "-";
    private int _planningAttemptCount;
    private int _planningSuccessCount;
    private int _planningFailureCount;
    private float _lastPlanningAttemptTime;
    private float _nextNoConfigLogTime;
    private const float NoConfigLogInterval = 1.0f;
    private float _nextAllowedReplanTime;
    private float _nextVolatileImmediateReplanCoalesceUntil;
    private float _lastReceiveActionCompleteTime = -999f;
    private float _postShootDefenseGraceUntil = float.NegativeInfinity;
    private float _postPassSupportGraceUntil = float.NegativeInfinity;
    private float _postDefenseContextGraceUntil = float.NegativeInfinity;
    private float _postSupportContextGraceUntil = float.NegativeInfinity;
    private float _postAttackContextGraceUntil = float.NegativeInfinity;
    private string _lastReceiveFinishReason = string.Empty;
    private bool _lastReceiveOutcomeReceived;
    private bool _pendingReceivePassTransitionLog;
    private bool _matchPlayWasActive;
    private string _lastFailureSignature = "-";
    private int _sameFailureStreak;
    private bool _ballContextInitialized;
    private bool _lastTrackedTeamHasBall;
    private bool _lastTrackedEnemyHasBall;
    private BallManager_State.BALL_STATE _lastTrackedBallState = BallManager_State.BALL_STATE.FREE;
    private bool _enemyLayoutInitialized;
    private readonly List<Vector3> _lastTrackedEnemyPositions = new();
    private bool _passReceiveEligibilityInitialized;
    private bool _lastTrackedInPassReceivePosition;
    private bool _passReceivePendingValue;
    private float _passReceivePendingSince;
    private float _nextPassReceiveEligibilityReplanTime;
    private bool _ballOwnerLayoutInitialized;
    private Vector3 _lastTrackedBallOwnerPosition;
    private int _lastTrackedBallOwnerId;
    private float _nextBallOwnerMovementReplanTime;
    private GoapNpcTier _npcTier = GoapNpcTier.Sub;
    [Header("Failure Cooldown")]
    [SerializeField] private float _baseFailureCooldown = 0.2f;
    [SerializeField] private float _maxFailureCooldown = 1.2f;
    [Header("Enemy Layout Replan")]
    [Tooltip("味方/敵ボール時に敵配置の変化で即再計画する")]
    [SerializeField] private bool _replanOnEnemyLayoutChange = true;
    [Tooltip("敵1体の移動がこの距離(フィールド長比)を超えたら再計画")]
    [SerializeField] private float _enemyLayoutChangeThresholdRatio = 0.04f;
    [Tooltip("IS_IN_PASS_RECEIVE の true/false が変わったときも再計画する（味方ボール時）")]
    [SerializeField] private bool _replanOnPassReceiveEligibilityChange = true;
    [Tooltip("受け位置 Fact がこの秒数連続して変化したときだけ再計画（境界付近のオシレーション抑制）")]
    [SerializeField] private float _passReceiveEligibilityStableSeconds = 0.35f;
    [Tooltip("受け位置変化による再計画後、この秒数は同トリガーを無視")]
    [SerializeField] private float _passReceiveEligibilityReplanCooldown = 0.5f;
    [Header("Ball Owner Movement Replan")]
    [Tooltip("味方ボール時に保持者の移動で即再計画する（サポートNPCが旧位置に留まるのを防ぐ）")]
    [SerializeField] private bool _replanOnBallOwnerMovement = true;
    [Tooltip("保持者の移動がこの距離(フィールド長比)を超えたら再計画")]
    [SerializeField] private float _ballOwnerMovementThresholdRatio = 0.03f;
    [Tooltip("BallOwnerMoved 再計画後、この秒数は同トリガーを無視（BallOwnerChanged は除外）")]
    [SerializeField] private float _ballOwnerMovementReplanCooldown = 0.55f;
    
    // === 初期化 ===
    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        DebugLogger.Clear();
        EnsureInitialized();
    }
    
    // GOAPエージェントの初期化
    private void EnsureInitialized()
    {
        // リストの初期化(可能アクション/可能ゴール/プランキュー)
        if (_availableActions == null) _availableActions = new List<GoapActionSO>();
        if (_availableGoals == null) _availableGoals = new List<GoapGoalSO>();
        if (_currentPlan == null) _currentPlan = new Queue<GoapActionRuntime>();
        
        _isPlanning = false;
        _planFailed = false;
        _lastPlanningTime = 0f;
        _lastReplanReason = "Init";
        _lastPlanSummary = "Initialized";
        _lastSelectedGoalName = "-";
        _lastFailureCategory = "None";
        _lastFailureDetails = "-";
        _planningAttemptCount = 0;
        _planningSuccessCount = 0;
        _planningFailureCount = 0;
        _lastPlanningAttemptTime = 0f;
        _nextNoConfigLogTime = 0f;
        _nextAllowedReplanTime = 0f;
        _lastFailureSignature = "-";
        _sameFailureStreak = 0;
        _ballContextInitialized = false;
        _enemyLayoutInitialized = false;
        _lastTrackedEnemyPositions.Clear();
        _passReceiveEligibilityInitialized = false;
        _lastTrackedInPassReceivePosition = false;
        _passReceivePendingValue = false;
        _passReceivePendingSince = 0f;
        _nextPassReceiveEligibilityReplanTime = 0f;
        _ballOwnerLayoutInitialized = false;
        _lastTrackedBallOwnerPosition = Vector3.zero;
        _lastTrackedBallOwnerId = -1;
        _nextBallOwnerMovementReplanTime = 0f;
        
        // A*プランナーの初期化（エージェント名を渡す）
        _planner = new AStarPlanner($"{this.name}(GoapAgent)");
        
        // ブラックボードの確認
        if (_playerBlackboard == null)
        {
            _playerBlackboard = GetComponent<PlayerBlackboard>();
            if (_playerBlackboard == null)
            {
                _playerBlackboard = GetComponentInChildren<PlayerBlackboard>(true);
            }
        }
        
        if (_debugMode)
        {
            DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent初期化完了: {_availableActions.Count}個のアクション, {_availableGoals.Count}個のゴール");
        }
    }
    
    // === メイン更新ループ ===
    private void Update()
    {
        if (!enabled || _playerBlackboard == null) return;

        bool matchPlayActive = GoapMatchPlayGate.IsMatchPlayActive();
        bool matchPlayJustStarted = matchPlayActive && !_matchPlayWasActive;
        _matchPlayWasActive = matchPlayActive;

        if (!matchPlayActive)
        {
            ClearPlanForPreMatch();
        }

        // P3: 人間操作キャラ・非対象NPCでは GOAP を動かさない（enabled が誤って true でも安全）
        bool goapContextEnabled = IsEnabledForTeammateNpcGoap();
        if (matchPlayJustStarted && goapContextEnabled)
        {
            TriggerImmediateReplan(
                "MatchPlayStarted",
                "MatchPlayStarted(state=GAME)",
                allowDuringDeferredAction: true);
        }

        if (!goapContextEnabled && _currentAction == null)
        {
            return;
        }

        // 更新順の差分を吸収するため、計画直前にも明示的に同期する
        _playerBlackboard.RefreshWorkingMemory();

        if (!HasValidPlanningSetup())
        {
            if (Time.time >= _nextNoConfigLogTime)
            {
                _nextNoConfigLogTime = Time.time + NoConfigLogInterval;
                string details = $"goals={(_availableGoals != null ? _availableGoals.Count : 0)}, actions={(_availableActions != null ? _availableActions.Count : 0)}";
                SetPlanningFailure("NoConfig", details);
                _lastPlanSummary = $"SkipPlanning({details})";
            }
            return;
        }

        // 現在のアクションが完了したかチェック
        if (_currentAction != null)
        {
            UpdateCurrentAction();
        }

        // プランが必要かチェック
        if (ShouldReplan())
        {
            // プラン作成開始
            StartPlanning();
        }

        //Debug.Log("プラン生成中:" + _isPlanning + " アクションがある:" + _currentAction + " プラン数:" + _currentPlan.Count);
        // プラン生成中でない場合
        if (!_isPlanning && _currentAction == null && _currentPlan.Count > 0)
        {
           // 次のアクションを実行
           ExecuteNextAction();
        }
    }
    
    // === プラン管理 ===
    // プランを構築するか?
    private bool ShouldReplan()
    {
        if (TryConsumeBallContextChange())
        {
            return _currentAction == null;
        }

        if (TryConsumeEnemyLayoutChange())
        {
            return _currentAction == null;
        }

        if (TryConsumePassReceiveEligibilityChange())
        {
            return _currentAction == null;
        }

        if (TryConsumeBallOwnerMovementChange())
        {
            return _currentAction == null;
        }

        // クールダウン中は再計画しない（失敗・既達成どちらのケースも）
        if (Time.time < _nextAllowedReplanTime)
        {
            return false;
        }

        // アクション実行中は完了まで再計画しない（Interval で二重キュー→ActionRejected を防ぐ）
        if (_currentAction != null)
        {
            return false;
        }

        // プランがない場合
        if (_currentPlan.Count == 0)
        {
            _lastReplanReason = "PlanQueueEmpty";
            DebugLogger.Log($"[{this.name}(GoapAgent)] プランキューが空のため再計画");
            return true;
        }
        
        // 一定時間経過した場合
        if (Time.time - _lastPlanningTime > _planningInterval)
        {
            _lastReplanReason = "Interval";
            DebugLogger.Log($"[{this.name}(GoapAgent)] 一定時間経過した場合");
            return true;
        }
        
        // プランが失敗した場合
        if (_planFailed)
        {
            _lastReplanReason = "PlanFailed";
            DebugLogger.Log($"[{this.name}(GoapAgent)] プランが失敗した場合");
            return true;
        }
        
        return false;
    }

    /// <summary>TeamBlackboard の所属/ボール状態変化時に即再計画（遷移中の長い NoGoalIdle を回避）。</summary>
    private bool TryConsumeBallContextChange()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !teamBB.BallInfo.IsExistBall)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        bool teamHas = ball.TeamHasBall;
        bool enemyHas = ball.EnemyHasBall;
        var ballState = ball.BallState;

        if (!_ballContextInitialized)
        {
            _ballContextInitialized = true;
            _lastTrackedTeamHasBall = teamHas;
            _lastTrackedEnemyHasBall = enemyHas;
            _lastTrackedBallState = ballState;
            return false;
        }

        bool possessionChanged = teamHas != _lastTrackedTeamHasBall || enemyHas != _lastTrackedEnemyHasBall;
        bool stateChanged = ballState != _lastTrackedBallState;
        if (!possessionChanged && !stateChanged)
        {
            return false;
        }

        _lastTrackedTeamHasBall = teamHas;
        _lastTrackedEnemyHasBall = enemyHas;
        _lastTrackedBallState = ballState;
        ResetTacticalLayoutTracking();
        _lastReplanReason = possessionChanged && stateChanged
            ? "BallContextChanged"
            : (possessionChanged ? "BallPossessionChanged" : "BallStateChanged");
        TriggerImmediateReplan(
            _lastReplanReason,
            $"BallContextChanged(teamHasBall={teamHas}, enemyHasBall={enemyHas}, ballState={ballState}, reason={_lastReplanReason})");
        DebugLogger.Log(
            $"[{this.name}(GoapAgent)] ボール文脈変化 (teamHasBall={teamHas}, enemyHasBall={enemyHas}, ballState={ballState})");
        return true;
    }

    /// <summary>敵フィールドプレイヤーの配置変化時に即再計画（GoalAlreadyAchieved の長いクールダウンも解除）。</summary>
    private bool TryConsumeEnemyLayoutChange()
    {
        if (!_replanOnEnemyLayoutChange)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !ShouldTrackEnemyLayoutForReplan(teamBB))
        {
            ResetEnemyLayoutTracking();
            return false;
        }

        var enemies = teamBB.BasicInfo.EnemyPositions;
        if (enemies == null)
        {
            ResetEnemyLayoutTracking();
            return false;
        }

        float threshold = Mathf.Max(
            0.5f,
            teamBB.FieldInfo.FieldLength * Mathf.Max(0.01f, _enemyLayoutChangeThresholdRatio));

        if (!_enemyLayoutInitialized)
        {
            CaptureEnemyLayoutSnapshot(enemies);
            _enemyLayoutInitialized = true;
            return false;
        }

        float maxDelta = ComputeEnemyLayoutMaxDelta(_lastTrackedEnemyPositions, enemies);
        bool countChanged = _lastTrackedEnemyPositions.Count != enemies.Count;
        if (!countChanged && maxDelta < threshold)
        {
            return false;
        }

        CaptureEnemyLayoutSnapshot(enemies);
        TriggerImmediateReplan(
            "EnemyLayoutChanged",
            $"EnemyLayoutChanged(enemies={enemies.Count}, maxDelta={maxDelta:F2}, threshold={threshold:F2})");
        return true;
    }

    /// <summary>味方ボール時のパス受け可否 Fact 変化で再計画（敵がパスレーンを塞いだ等）。</summary>
    private bool TryConsumePassReceiveEligibilityChange()
    {
        if (!_replanOnPassReceiveEligibilityChange || _playerBlackboard == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
        {
            ResetPassReceiveEligibilityTracking();
            return false;
        }

        if (_playerBlackboard.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            ResetPassReceiveEligibilityTracking();
            return false;
        }

        bool inPosition = _playerBlackboard.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true;
        if (!_passReceiveEligibilityInitialized)
        {
            CommitPassReceiveEligibilityState(inPosition);
            return false;
        }

        if (inPosition == _lastTrackedInPassReceivePosition)
        {
            _passReceivePendingValue = inPosition;
            _passReceivePendingSince = Time.time;
            return false;
        }

        if (inPosition != _passReceivePendingValue)
        {
            _passReceivePendingValue = inPosition;
            _passReceivePendingSince = Time.time;
            return false;
        }

        float stableSeconds = Mathf.Max(0.05f, _passReceiveEligibilityStableSeconds);
        if (Time.time - _passReceivePendingSince < stableSeconds)
        {
            return false;
        }

        if (Time.time < _nextPassReceiveEligibilityReplanTime)
        {
            return false;
        }

        bool wasInPosition = _lastTrackedInPassReceivePosition;
        CommitPassReceiveEligibilityState(inPosition);
        _nextPassReceiveEligibilityReplanTime = Time.time + Mathf.Max(0.1f, _passReceiveEligibilityReplanCooldown);
        TriggerImmediateReplan(
            "PassReceiveEligibilityChanged",
            $"PassReceiveEligibilityChanged(was={wasInPosition}, now={inPosition}, stable={stableSeconds:F2}s)");
        return true;
    }

    /// <summary>味方ボール時に保持者が動いたら再計画（CreateSupportAngle 完了後の居座り対策）。</summary>
    private bool TryConsumeBallOwnerMovementChange()
    {
        if (!_replanOnBallOwnerMovement || _playerBlackboard == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
        {
            ResetBallOwnerLayoutTracking();
            return false;
        }

        if (_playerBlackboard.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            ResetBallOwnerLayoutTracking();
            return false;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = teamBB.BallInfo.BallPosition;
        }

        int ownerId = teamBB.BallInfo.BallOwnerID;
        if (!_ballOwnerLayoutInitialized)
        {
            CaptureBallOwnerLayoutSnapshot(ownerPos, ownerId);
            return false;
        }

        float threshold = Mathf.Max(
            0.35f,
            teamBB.FieldInfo.FieldLength * Mathf.Max(0.01f, _ballOwnerMovementThresholdRatio));

        bool ownerChanged = ownerId != _lastTrackedBallOwnerId;
        float delta = Vector3.Distance(ownerPos, _lastTrackedBallOwnerPosition);
        if (!ownerChanged && delta < threshold)
        {
            return false;
        }

        // BallOwnerMoved のみクールダウン。スナップショットは維持し、解除後に蓄積 delta で発火する。
        if (TeammateNpcSupportPlanning.IsBallOwnerMovedReplanOnCooldown(
                ownerChanged,
                Time.time,
                _nextBallOwnerMovementReplanTime))
        {
            return false;
        }

        CaptureBallOwnerLayoutSnapshot(ownerPos, ownerId);
        string reason = ownerChanged ? "BallOwnerChanged" : "BallOwnerMoved";
        float cooldown = Mathf.Max(0.1f, _ballOwnerMovementReplanCooldown);

        // GetOpen 等のサポート移動中は Cancel せず snapshot だけ更新（短周期 Abort ループ抑制）。
        // AbortCurrentPlan / BallOwnerChanged は従来どおり即再計画。
        if (TeammateNpcSupportPlanning.ShouldDeferBallOwnerMovedWhileSupportMoving(
                ownerChanged,
                TeammateNpcSupportPlanning.IsSupportMovementRuntime(_currentAction)))
        {
            _nextBallOwnerMovementReplanTime = Time.time + cooldown;
            LogSummary($"ReplanDeferred(reason={reason}, action={_currentAction.DisplayName})");
            return false;
        }

        _nextBallOwnerMovementReplanTime = Time.time + cooldown;
        TriggerImmediateReplan(
            reason,
            $"{reason}(ownerId={ownerId}, delta={delta:F2}, threshold={threshold:F2})");
        return true;
    }

    private void ResetBallOwnerLayoutTracking()
    {
        _ballOwnerLayoutInitialized = false;
        _lastTrackedBallOwnerPosition = Vector3.zero;
        _lastTrackedBallOwnerId = -1;
        _nextBallOwnerMovementReplanTime = 0f;
    }

    private void CaptureBallOwnerLayoutSnapshot(Vector3 ownerPos, int ownerId)
    {
        _lastTrackedBallOwnerPosition = ownerPos;
        _lastTrackedBallOwnerId = ownerId;
        _ballOwnerLayoutInitialized = true;
    }

    private bool ShouldTrackEnemyLayoutForReplan(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return false;
        }

        if (TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
        {
            return true;
        }

        var ball = teamBB.BallInfo;
        return ball.EnemyHasBall && !ball.TeamHasBall;
    }

    private void ResetTacticalLayoutTracking()
    {
        ResetEnemyLayoutTracking();
        ResetPassReceiveEligibilityTracking();
        ResetBallOwnerLayoutTracking();
    }

    private void ResetEnemyLayoutTracking()
    {
        _enemyLayoutInitialized = false;
        _lastTrackedEnemyPositions.Clear();
    }

    private void ResetPassReceiveEligibilityTracking()
    {
        _passReceiveEligibilityInitialized = false;
        _lastTrackedInPassReceivePosition = false;
        _passReceivePendingValue = false;
        _passReceivePendingSince = 0f;
        _nextPassReceiveEligibilityReplanTime = 0f;
    }

    private void CommitPassReceiveEligibilityState(bool inPosition)
    {
        _lastTrackedInPassReceivePosition = inPosition;
        _passReceivePendingValue = inPosition;
        _passReceivePendingSince = Time.time;
        _passReceiveEligibilityInitialized = true;
    }

    private void CaptureEnemyLayoutSnapshot(IReadOnlyList<Vector3> enemies)
    {
        _lastTrackedEnemyPositions.Clear();
        if (enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 p = enemies[i];
            p.y = 0f;
            _lastTrackedEnemyPositions.Add(p);
        }
    }

    private static float ComputeEnemyLayoutMaxDelta(IReadOnlyList<Vector3> previous, IReadOnlyList<Vector3> current)
    {
        if (previous == null || current == null)
        {
            return float.MaxValue;
        }

        if (previous.Count != current.Count)
        {
            return float.MaxValue;
        }

        float maxDelta = 0f;
        for (int i = 0; i < current.Count; i++)
        {
            Vector3 a = previous[i];
            Vector3 b = current[i];
            a.y = 0f;
            b.y = 0f;
            maxDelta = Mathf.Max(maxDelta, Vector3.Distance(a, b));
        }

        return maxDelta;
    }

    private void TriggerImmediateReplan(
        string replanReason,
        string summaryMessage,
        bool allowDuringDeferredAction = false)
    {
        if (!allowDuringDeferredAction && ShouldDeferPlanInterrupt())
        {
            LogSummary($"ReplanDeferred(reason={replanReason}, action={_currentAction.DisplayName})");
            return;
        }

        float now = Time.time;
        if (GoapReplanLoadRules.ShouldCoalesceImmediateReplan(
                replanReason,
                now,
                _nextVolatileImmediateReplanCoalesceUntil))
        {
            if (GoapRuntimeDiagnostics.VerboseLoggingEnabled)
            {
                LogSummary($"ReplanCoalesced(reason={replanReason}, until={_nextVolatileImmediateReplanCoalesceUntil:F2})");
            }

            return;
        }

        _lastReplanReason = replanReason;
        _nextAllowedReplanTime = 0f;
        _sameFailureStreak = 0;
        _planFailed = false;

        float coalesceCooldown = GoapReplanLoadRules.ResolveCoalesceCooldownSeconds(replanReason);
        if (coalesceCooldown > 0f)
        {
            _nextVolatileImmediateReplanCoalesceUntil = GoapReplanLoadRules.ComputeCoalesceUntil(now, replanReason);
        }

        if (_currentAction != null || _currentPlan.Count > 0)
        {
            if (_currentAction != null)
            {
                _currentAction.Cancel();
                _currentAction = null;
            }

            _currentPlan?.Clear();
        }

        LogSummary(summaryMessage);
    }

    private float GetNoGoalIdleDelay()
    {
        if (IsInPostReceiveTransitionWindow())
        {
            return Mathf.Max(0.05f, _postReceiveNoGoalIdleSeconds);
        }

        if (IsVolatileNoGoalReplanReason())
        {
            // 連続 NoGoal では待機を伸ばしスパムと固着感を抑える
            float baseDelay = Mathf.Max(0.1f, _transitionNoGoalIdleSeconds);
            if (_planningAttemptCount >= 8)
            {
                return Mathf.Min(1.5f, baseDelay * (1f + _planningAttemptCount * 0.15f));
            }

            return baseDelay;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !teamBB.BallInfo.IsExistBall)
        {
            return Mathf.Max(0.15f, _defaultNoGoalIdleSeconds);
        }

        var ball = teamBB.BallInfo;
        bool inTransition = ball.BallState == BallManager_State.BALL_STATE.SHOOT
            || ball.BallState == BallManager_State.BALL_STATE.PASS
            || ball.BallState == BallManager_State.BALL_STATE.FREE
            || (!ball.TeamHasBall && !ball.EnemyHasBall);

        float delay = inTransition
            ? Mathf.Max(0.1f, _transitionNoGoalIdleSeconds)
            : Mathf.Max(0.15f, _defaultNoGoalIdleSeconds);

        if (_planningAttemptCount >= 8)
        {
            delay = Mathf.Min(2f, delay * (1f + _planningAttemptCount * 0.12f));
        }

        return delay;
    }

    private bool IsInPostReceiveTransitionWindow()
    {
        return Time.time - _lastReceiveActionCompleteTime <= Mathf.Max(0.1f, _postReceiveTransitionWindowSeconds);
    }

    private bool IsVolatileNoGoalReplanReason()
    {
        return _lastReplanReason == "BallContextChanged"
            || _lastReplanReason == "BallStateChanged"
            || _lastReplanReason == "BallPossessionChanged"
            || _lastReplanReason == "PlanQueueEmpty"
            || _lastReplanReason == "EnemyLayoutChanged"
            || _lastReplanReason == "BallOwnerMoved"
            || _lastReplanReason == "BallOwnerChanged"
            || _lastReplanReason == "PassReceiveEligibilityChanged"
            || _lastReplanReason == "PassReceiveComplete"
            || _lastReplanReason == "PassIssued"
            || _lastReplanReason == "MatchPlayStarted";
    }

    /// <summary>キックオフ前など非 GAME 中に残ったプランを破棄する。</summary>
    private void ClearPlanForPreMatch()
    {
        if (_currentAction == null && (_currentPlan == null || _currentPlan.Count == 0) && !_isPlanning)
        {
            return;
        }

        if (_currentAction != null)
        {
            _currentAction.Cancel();
            _currentAction = null;
        }

        _currentPlan?.Clear();
        _currentGoal = null;
        _planFailed = false;
        _isPlanning = false;
        _nextAllowedReplanTime = 0f;
    }

    /// <summary>味方フィールドNPC向け GOAP のみ Update を許可（操作キャラ・非対象は除外）。</summary>
    private bool IsEnabledForTeammateNpcGoap()
    {
        if (_playerBlackboard?.BasicData?.Self == null)
        {
            return true;
        }

        var self = _playerBlackboard.BasicData.Self;
        var facade = self.GetComponent<AnimalFacade>();
        if (facade != null && facade.IsGK())
        {
            return false;
        }

        if (GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive)
        {
            var squadDuringVerify = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            return squadDuringVerify == null || squadDuringVerify.ShouldUseGoapFor(facade);
        }

        if (!GoapMatchPlayGate.IsMatchPlayActive())
        {
            return false;
        }

        if (GoapMainNpcProductionEnvironment.IsActive
            && GoapMainNpcProductionEnvironment.IsProductionMainPlayer(facade))
        {
            if (GoapBallActionGuard.IsAnyInProgress(facade) || HasUnfinishedCommittedBallAction)
            {
                return true;
            }

            return GoapMainNpcProductionEnvironment.ShouldEnableGoap(_playerBlackboard, facade);
        }

        var assignment = self.GetComponentInParent<AnimalControlAssignment>()
            ?? self.GetComponent<AnimalControlAssignment>();
        if (assignment != null && assignment.Role == AnimalControlRole.EnemyFieldNpc)
        {
            var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
            if (enemySquad == null || !enemySquad.ShouldUseGoapFor(facade))
            {
                return false;
            }

            if (enemySquad.ResolveNpcTier(facade) == GoapNpcTier.Main)
            {
                return GoapEnemyMainNpcPlanning.ShouldEnableGoap(_playerBlackboard, facade);
            }

            return GoapEnemySubNpcPlanning.ShouldEnableGoap(_playerBlackboard, facade);
        }

        if (IsActiveHumanSelectedPlayer(facade))
        {
            return false;
        }

        if (assignment == null)
        {
            return true;
        }

        if (assignment.IsHumanControlled || assignment.Role == AnimalControlRole.Human)
        {
            return false;
        }

        if (assignment.Role != AnimalControlRole.TeammateNpc)
        {
            return false;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        return squad == null || facade == null || squad.ShouldUseGoapFor(facade);
    }

    /// <summary>AnimalSelector で現在選択中の操作キャラ（例: Shark 1001）では GOAP を動かさない。</summary>
    private static bool IsActiveHumanSelectedPlayer(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        if (GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive)
        {
            return false;
        }

        var teamFacade = TeamFacade.Instance;
        var selector = teamFacade != null ? teamFacade.AnimalSelectorManager : null;
        var playerSelector = selector != null ? selector.PlayerSelector : null;
        if (playerSelector == null)
        {
            return false;
        }

        return playerSelector.SelAnimalFacade == facade;
    }

    // プラン作成開始
    private void StartPlanning()
    {
        // プラン作成中か?
        if (_isPlanning) return;
        // プラン作成中に変更/プラン作成失敗をなくす
        _isPlanning = true;
        _planFailed = false;
        _planningAttemptCount++;
        _lastPlanningAttemptTime = Time.time;
        _lastPlanSummary = $"PlanningStart(reason={_lastReplanReason}, attempt={_planningAttemptCount})";
        LogSummary(_lastPlanSummary);
        
        // 非同期でプラン生成を開始
        StartCoroutine(GeneratePlanCoroutine());
    }
    // プラン作成コルーチン
    private IEnumerator GeneratePlanCoroutine()
    {
        bool shouldLogReceiveTransition = _pendingReceivePassTransitionLog;
        string receiveTransition = "unknown";
        _pendingReceivePassTransitionLog = false;

        // 各ゴールに対してプランを作成し、成功したプランの中から最適なものを選択
        var bestPlan = SelectBestPlanWithGoal();
        
        if (bestPlan.plan != null)
        {
            var resolvedPlan = bestPlan.plan;
            if (resolvedPlan.Count == 0
                && (TryConvertEmptyPlanToForcedSupport(bestPlan.goal, ref resolvedPlan)
                    || TryConvertEmptyPlanToForcedDefense(bestPlan.goal, ref resolvedPlan)
                    || TryConvertEmptyPlanToForcedMainAttack(bestPlan.goal, ref resolvedPlan)
                    || TryConvertEmptyPlanToForcedFreeBall(bestPlan.goal, ref resolvedPlan)
                    || TryConvertEmptyPlanToForcedIncomingPassReceive(bestPlan.goal, ref resolvedPlan)))
            {
                bestPlan.plan = resolvedPlan;
            }

            // 空プラン = ゴール条件がすでに満たされている（即達成）
            if (bestPlan.plan.Count == 0)
            {
                _lastPlanningTime = Time.time;
                _planningSuccessCount++;
                _lastFailureCategory = "None";
                _lastFailureDetails = "-";
                _lastFailureSignature = "-";
                _sameFailureStreak = 0;
                // インターバル後に再評価（連続プランニングを防ぐ）
                _nextAllowedReplanTime = Time.time + _planningInterval;
                _lastPlanSummary = $"GoalAlreadyAchieved(goal={_lastSelectedGoalName}, attempt={_planningAttemptCount})";
                LogSummary(_lastPlanSummary);
                receiveTransition = "goal_already_achieved";
                DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: ゴール既達成（空プラン）");
            }
            else
            {
                // プランをランタイムアクションに変換
                _currentPlan.Clear();
                foreach (var actionSO in bestPlan.plan)
                {
                    // ランタイムを取得する
                    var runtimeAction = actionSO.CreateRuntime(GetRuntimeDebugName(actionSO));
                    if (runtimeAction != null){
                        // プランキューにランタイムを追加
                        _currentPlan.Enqueue(runtimeAction);
                    }
                }
                
                // ゴールが変わった場合の処理
                if (_currentGoal != bestPlan.goal)
                {
                    _currentGoal = bestPlan.goal;
                    DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: ゴール変更 -> {bestPlan.goal.GoalName}");
                    LogSummary($"GoalChanged(goal={bestPlan.goal.GoalName})");
                }
                
                // プランの生成時間を記録
                _lastPlanningTime = Time.time;
                _planningSuccessCount++;
                _lastFailureCategory = "None";
                _lastFailureDetails = "-";
                _lastFailureSignature = "-";
                _sameFailureStreak = 0;
                _nextAllowedReplanTime = 0f;
                string actionPath = _currentPlan.Count > 0
                    ? string.Join(">", _currentPlan.Select(a => a.DisplayName))
                    : "(alreadyAchieved)";
                _lastPlanSummary = $"PlanSuccess(goal={_lastSelectedGoalName}, actions={_currentPlan.Count}, path={actionPath}, attempt={_planningAttemptCount})";
                LogSummary(_lastPlanSummary);
                receiveTransition = ResolveReceivePassTransition(_lastSelectedGoalName);
                
                DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: プラン生成成功 ({_currentPlan.Count}個のアクション)");
            }
        }
        // プラン生成失敗
        else
        {
            // 一時的に有効ゴールが無い（例: ボール状態遷移中）は失敗扱いしない
            // PlanFailed表示のスパムを抑え、次の有効局面まで待機する。
            if (_lastFailureCategory == "NoGoal")
            {
                float idleDelay = GetNoGoalIdleDelay();
                _planFailed = false;
                _nextAllowedReplanTime = Time.time + idleDelay;
                _lastPlanSummary = $"NoGoalIdle(wait={idleDelay:F1}s, reason={_lastReplanReason}, attempt={_planningAttemptCount})";
                LogSummary(_lastPlanSummary);
                receiveTransition = "nogal";
                DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: 有効ゴールなしのため待機");
            }
            else
            {
                _planFailed = true;
                _planningFailureCount++;
                ApplyFailureCooldown();
                _lastPlanSummary = $"PlanFailure(goal={_lastSelectedGoalName}, reason={_lastReplanReason}, category={_lastFailureCategory}, details={_lastFailureDetails}, attempt={_planningAttemptCount})";
                LogSummary(_lastPlanSummary);
                receiveTransition = "plan_failure";
                DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: 全てのゴールでプラン生成失敗");
            }
        }

        if (shouldLogReceiveTransition)
        {
            LogReceivePassTransition(receiveTransition);
        }
        
        _isPlanning = false;
        DebugLogger.Save();
        yield break;
    }
    
    // === ゴール選択 ===
    // 最適なプランとゴールの選択
    private (Queue<GoapActionSO> plan, GoapGoalSO goal) SelectBestPlanWithGoal()
    {
        if (_lastReplanReason == "PassIssued")
        {
            var postPassPlan = TrySelectPostPassSupportPlan();
            if (postPassPlan.plan != null)
            {
                return postPassPlan;
            }
        }

        if (_lastReplanReason == "PassReceiveComplete" && !_lastReceiveOutcomeReceived)
        {
            var postReceivePlan = TrySelectPostReceiveRecoveryPlan();
            if (postReceivePlan.plan != null)
            {
                return postReceivePlan;
            }
        }

        // DebugLogger.Log($"[{this.name}(GoapAgent)] 可能ゴール数:" + _availableGoals.Count);
        
        // 可能ゴールがない
        if (_availableGoals.Count == 0) 
        {
            SetPlanningFailure("NoGoal", "availableGoals=0");
            // DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: 利用可能なゴールがありません");
            return (null, null);
        }

        if (_availableActions == null || _availableActions.Count == 0)
        {
            SetPlanningFailure("EmptyActions", "availableActions=0");
            return (null, null);
        }
        
        // 先に最適なゴールを選択
        var bestGoal = SelectBestGoal();
        if (bestGoal == null)
        {
            if (TryBuildForcedPlanWhenSelectBestGoalNull(out bestGoal, out Queue<GoapActionSO> forcedPlan))
            {
                _lastSelectedGoalName = bestGoal.GoalName;
                return (forcedPlan, bestGoal);
            }

            _lastSelectedGoalName = "-";
            SetPlanningFailure("NoGoal", "SelectBestGoal returned null");
            _lastPlanSummary = $"NoGoalSelected(attempt={_planningAttemptCount})";
            LogSummary(_lastPlanSummary);
            // DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: 最適なゴールが見つかりません");
            return (null, null);
        }
        _lastSelectedGoalName = bestGoal.GoalName;
        
        // DebugLogger.Log($"[{this.name}(GoapAgent)] 選択されたゴール: '{bestGoal.GoalName}' のプラン生成を開始");
        
        // 現在の状態を更新する(推論用)/選択されたゴールに対してプランを生成
        var goalActions = FilterActionsForGoal(bestGoal, _availableActions);
        if (goalActions == null || goalActions.Count == 0)
        {
            SetPlanningFailure("EmptyActions", $"goal={bestGoal.GoalName}, scopedActions=0");
            return (null, bestGoal);
        }

        foreach (GoapActionSO action in goalActions)
        {
            action?.EnsurePlanningFactsConfigured();
        }

        var plans = _planner.Plan(goalActions, _playerBlackboard, bestGoal.GetPlanningRequiredFacts(_playerBlackboard));

        // // 可能なプランのデバッグ表示
        // if (plans != null && plans.Count > 0)
        // {
        //     DebugLogger.Log($"[{this.name}(GoapAgent)] 可能なプラン数: {plans.Count}");
        //     for (int i = 0; i < plans.Count; i++)
        //     {
        //         var planActions = string.Join(" -> ", plans[i].Select(a => a.ActionName));
        //         DebugLogger.Log($"[{this.name}(GoapAgent)] プラン{i + 1}: {planActions}");
        //     }
        // }
        
        if (plans != null && plans.Count > 0)
        {
            // 複数のプランから一番コストが低いものを選択
            var bestPlan = SelectBestPlanFromPlans(plans);
            bestPlan = PreferForcedBallPossessionAttackPlan(bestGoal, goalActions, bestPlan);
            bestPlan = SanitizeBallPossessionAttackPlan(bestGoal, goalActions, bestPlan);
            if ((bestPlan == null || bestPlan.Count == 0)
                && TryBuildForcedIncomingPassReceivePlan(bestGoal, goalActions, out var forcedReceivePlan))
            {
                bestPlan = forcedReceivePlan;
            }
            else if ((bestPlan == null || bestPlan.Count == 0)
                && TryBuildForcedTacticalPlanForGoal(_playerBlackboard, goalActions, out var forcedPlan))
            {
                bestPlan = forcedPlan;
                LogSummary("ForcedTacticalSupportPlan(action=" +
                    (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ")");
            }

            LogPlanCostSummary(bestGoal, goalActions, plans, bestPlan);

            if (bestPlan != null)
            {
                // DebugLogger.Log($"[{this.name}(GoapAgent)] ゴール '{bestGoal.GoalName}' のプラン生成成功 (アクション数: {bestPlan.Count})");
                return (bestPlan, bestGoal);
            }

            SetPlanningFailure("NoPlanFromPlanner", $"goal={bestGoal.GoalName}, plannerPlans={plans.Count}, bestPlan=null");
            return (null, bestGoal);
        }

        LogPlanCostSummary(bestGoal, goalActions, plans, null);

        if (TryBuildForcedIncomingPassReceivePlan(bestGoal, goalActions, out var forcedReceivePlanWhenNoCandidates)
            && forcedReceivePlanWhenNoCandidates != null
            && forcedReceivePlanWhenNoCandidates.Count > 0)
        {
            return (forcedReceivePlanWhenNoCandidates, bestGoal);
        }

        if (TryBuildForcedTacticalPlanForGoal(
                _playerBlackboard, goalActions, out var forcedPlanWhenNoCandidates)
            && forcedPlanWhenNoCandidates != null
            && forcedPlanWhenNoCandidates.Count > 0)
        {
            forcedPlanWhenNoCandidates = PreferForcedBallPossessionAttackPlan(
                bestGoal,
                goalActions,
                forcedPlanWhenNoCandidates);
            forcedPlanWhenNoCandidates = SanitizeBallPossessionAttackPlan(
                bestGoal,
                goalActions,
                forcedPlanWhenNoCandidates);
            LogSummary("ForcedTacticalSupportPlan(action=" +
                forcedPlanWhenNoCandidates.Peek().ActionName + ", reason=noPlannerPlans)");
            return (forcedPlanWhenNoCandidates, bestGoal);
        }

        string missingFacts = BuildMissingFactsSummary(bestGoal);
        int scopedCount = FilterActionsForGoal(bestGoal, _availableActions)?.Count ?? 0;
        SetPlanningFailure("NoPlanFromPlanner", $"goal={bestGoal.GoalName}, plannerPlans=0, missingFacts={missingFacts}, actions={scopedCount}");
        return (null, bestGoal);
    }
    
    /// <summary>
    /// 複数のプランから一番コストが低いものを選択
    /// </summary>
    /// <param name="plans">プランのリスト</param>
    /// <returns>一番コストが低いプラン</returns>
    private Queue<GoapActionSO> SelectBestPlanFromPlans(List<Queue<GoapActionSO>> plans)
    {
        if (plans == null || plans.Count == 0)
        {
            return null;
        }
        
        Queue<GoapActionSO> bestPlan = null;
        float lowestCost = float.MaxValue;
        Queue<GoapActionSO> emptyPlan = null;
        
        foreach (var plan in plans)
        {
            if (plan == null) continue;

            if (plan.Count == 0)
            {
                emptyPlan = plan;
                continue;
            }

            float totalCost = plan.Sum(action => action.CalculateDynamicCost(_playerBlackboard));
            
            DebugLogger.Log($"[{this.name}(GoapAgent)] プラン: {string.Join(" -> ", plan.Select(a => a.ActionName))} コスト: {totalCost:F2}");

            if (totalCost < lowestCost
                || (Mathf.Abs(totalCost - lowestCost) < 0.001f
                    && ShouldPreferDefensePlanOnCostTie(plan, bestPlan)))
            {
                lowestCost = totalCost;
                bestPlan = plan;
            }
        }

        if (bestPlan != null)
        {
            DebugLogger.Log($"[{this.name}(GoapAgent)] {plans.Count}個のプランから選択: コスト {lowestCost:F2}");
            return bestPlan;
        }

        if (emptyPlan != null
            && !TeammateNpcSupportPlanning.NeedsTacticalSupportMovement(_playerBlackboard)
            && !MainNpcPostPassPlanning.NeedsPostPassSupportMovement(_playerBlackboard)
            && !TeammateNpcDefensePlanning.NeedsTacticalDefenseMovement(_playerBlackboard)
            && !MainNpcAttackPlanning.NeedsForcedAttackPlan(_playerBlackboard)
            && !MainNpcPostPassPlanning.NeedsForcedFreeBallRecoveryPlan(_playerBlackboard)
            && !IncomingPassPlanning.NeedsForcedIncomingPassReceivePlan(_playerBlackboard)
            && !TeammateNpcDefensePlanning.NeedsForcedPostShootDefensePlan(
                _playerBlackboard,
                _postShootDefenseGraceUntil))
        {
            DebugLogger.Log($"[{this.name}(GoapAgent)] 空プラン（ゴール既達成）を選択");
            return emptyPlan;
        }
        
        DebugLogger.Log($"[{this.name}(GoapAgent)] 戦術サポート継続のため空プランをスキップ");
        return null;
    }

    /// <summary>空プランだがレーン追従が必要なとき、強制サポートプランへ差し替える。</summary>
    private bool TryConvertEmptyPlanToForcedSupport(GoapGoalSO goal, ref Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count > 0 || goal == null)
        {
            return false;
        }

        if (!TeammateNpcSupportPlanning.NeedsTacticalSupportMovement(_playerBlackboard)
            && !MainNpcPostPassPlanning.NeedsPostPassSupportMovement(_playerBlackboard))
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(goal, _availableActions);
        if (!TeammateNpcSupportPlanning.TryBuildForcedTacticalSupportPlan(
                _playerBlackboard, goalActions, out var forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        string reasonTag = MainNpcPostPassPlanning.NeedsPostPassSupportMovement(_playerBlackboard)
            ? "ForcedMainPostPassSupportPlan"
            : "ForcedTacticalSupportPlan";
        LogSummary($"{reasonTag}(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=emptyPlan)");
        return true;
    }

    private bool TryConvertEmptyPlanToForcedDefense(GoapGoalSO goal, ref Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count > 0 || goal == null)
        {
            return false;
        }

        if (!(goal is DefensivePositioningGoalSO or EnemyBallDefenseGoalSO))
        {
            return false;
        }

        if (!TeammateNpcDefensePlanning.NeedsTacticalDefenseMovement(_playerBlackboard)
            && !TeammateNpcDefensePlanning.NeedsForcedPostShootDefensePlan(
                _playerBlackboard,
                _postShootDefenseGraceUntil))
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(goal, _availableActions);
        if (TeammateNpcDefensePlanning.NeedsForcedPostShootDefensePlan(
                _playerBlackboard,
                _postShootDefenseGraceUntil)
            && TeammateNpcDefensePlanning.TryBuildForcedPostShootDefensePlan(
                _playerBlackboard,
                _availableGoals,
                _availableActions,
                out _,
                out Queue<GoapActionSO> postShootPlan,
                _postShootDefenseGraceUntil)
            && postShootPlan != null
            && postShootPlan.Count > 0)
        {
            plan = postShootPlan;
            LogSummary("ForcedPostShootDefensePlan(action=" +
                postShootPlan.Peek().ActionName + ", goal=" + goal.GoalName + ", reason=emptyPlan)");
            return true;
        }

        if (!TeammateNpcDefensePlanning.TryBuildForcedTacticalDefensePlan(
                _playerBlackboard, goalActions, out var forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        LogSummary("ForcedTacticalDefensePlan(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=emptyPlan)");
        return true;
    }

    private bool TryConvertEmptyPlanToForcedMainAttack(GoapGoalSO goal, ref Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count > 0 || goal == null)
        {
            return false;
        }

        if (goal is not BallPossessionAttackGoalSO)
        {
            return false;
        }

        if (!MainNpcAttackPlanning.NeedsForcedAttackPlan(_playerBlackboard))
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(goal, _availableActions);
        if (!MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                _playerBlackboard, goalActions, out var forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        LogSummary("ForcedMainAttackPlan(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=emptyPlan)");
        return true;
    }

    private bool TryConvertEmptyPlanToForcedFreeBall(GoapGoalSO goal, ref Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count > 0 || goal == null)
        {
            return false;
        }

        if (goal is not FreeBallRecoveryGoalSO)
        {
            return false;
        }

        if (!MainNpcPostPassPlanning.NeedsForcedFreeBallRecoveryPlan(_playerBlackboard))
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(goal, _availableActions);
        if (!MainNpcPostPassPlanning.TryBuildForcedFreeBallRecoveryPlan(
                _playerBlackboard, goalActions, out var forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        LogSummary("ForcedFreeBallRecoveryPlan(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=emptyPlanNearBall)");
        return true;
    }

    private bool TryBuildForcedPlanWhenSelectBestGoalNull(
        out GoapGoalSO goal,
        out Queue<GoapActionSO> plan)
    {
        goal = null;
        plan = null;

        if (TeammateNpcDefensePlanning.TryBuildForcedDefensePlanWhenNoGoal(
                _playerBlackboard,
                _availableGoals,
                _availableActions,
                out goal,
                out plan,
                _postShootDefenseGraceUntil,
                _postDefenseContextGraceUntil)
            && goal != null
            && plan != null
            && plan.Count > 0)
        {
            string reason = Time.time < _postShootDefenseGraceUntil
                ? "postShootGrace"
                : (Time.time < _postDefenseContextGraceUntil ? "defenseContextGrace" : "enemyBallContext");
            LogSummary("ForcedDefensePlanWhenNoGoal(action=" + plan.Peek().ActionName
                + ", goal=" + goal.GoalName + ", reason=" + reason + ")");
            return true;
        }

        if (TeammateNpcSupportPlanning.TryBuildForcedSupportPlanWhenNoGoal(
                _playerBlackboard,
                _availableGoals,
                _availableActions,
                out goal,
                out plan,
                _postPassSupportGraceUntil,
                _postSupportContextGraceUntil)
            && goal != null
            && plan != null
            && plan.Count > 0)
        {
            string reason = Time.time < _postPassSupportGraceUntil
                ? "postPassGrace"
                : (Time.time < _postSupportContextGraceUntil ? "supportContextGrace" : "teamBallContext");
            LogSummary("ForcedSupportPlanWhenNoGoal(action=" + plan.Peek().ActionName
                + ", goal=" + goal.GoalName + ", reason=" + reason + ")");
            return true;
        }

        var incomingGoal = _availableGoals.FirstOrDefault(g => g is IncomingPassReceiveGoalSO);
        if (incomingGoal != null
            && IncomingPassPlanning.NeedsForcedIncomingPassReceivePlan(_playerBlackboard))
        {
            var incomingActions = FilterActionsForGoal(incomingGoal, _availableActions);
            if (IncomingPassPlanning.TryBuildForcedIncomingPassReceivePlan(
                    _playerBlackboard,
                    incomingActions,
                    out plan)
                && plan != null
                && plan.Count > 0)
            {
                goal = incomingGoal;
                LogSummary("ForcedIncomingPassReceivePlan(action=" + plan.Peek().ActionName
                    + ", reason=selectBestGoalNull)");
                return true;
            }
        }

        var freeBallGoal = _availableGoals.FirstOrDefault(g => g is FreeBallRecoveryGoalSO);
        if (freeBallGoal != null
            && MainNpcPostPassPlanning.IsFreeBallRecoveryEligible(_playerBlackboard))
        {
            var freeBallActions = FilterActionsForGoal(freeBallGoal, _availableActions);
            if (MainNpcPostPassPlanning.TryBuildForcedFreeBallRecoveryPlan(
                    _playerBlackboard,
                    freeBallActions,
                    out plan)
                && plan != null
                && plan.Count > 0)
            {
                goal = freeBallGoal;
                LogSummary("ForcedFreeBallRecoveryPlan(action=" + plan.Peek().ActionName
                    + ", reason=selectBestGoalNull)");
                return true;
            }
        }

        var attackGoal = _availableGoals.FirstOrDefault(g => g is BallPossessionAttackGoalSO);
        if (attackGoal != null
            && MainNpcAttackPlanning.NeedsForcedAttackPlanWhenNoGoal(
                _playerBlackboard,
                _postAttackContextGraceUntil))
        {
            var attackActions = FilterActionsForGoal(attackGoal, _availableActions);
            if (MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                    _playerBlackboard,
                    attackActions,
                    out plan)
                && plan != null
                && plan.Count > 0)
            {
                goal = attackGoal;
                LogSummary("ForcedMainAttackPlan(action=" + plan.Peek().ActionName
                    + ", reason=selectBestGoalNull)");
                return true;
            }
        }

        return false;
    }

    private void RecordTacticalContextGrace(string goalName)
    {
        if (string.IsNullOrEmpty(goalName))
        {
            return;
        }

        float until = Time.time + _tacticalContextGraceSeconds;
        switch (goalName)
        {
            case "DefensivePositioning":
            case "EnemyBallDefense":
                _postDefenseContextGraceUntil = until;
                break;
            case "TeamBallSupport":
                _postSupportContextGraceUntil = until;
                break;
            case "BallPossessionAttack":
                _postAttackContextGraceUntil = until;
                break;
        }
    }

    private bool TryBuildForcedIncomingPassReceivePlan(
        GoapGoalSO goal,
        List<GoapActionSO> goalActions,
        out Queue<GoapActionSO> plan)
    {
        plan = null;
        if (goal is not IncomingPassReceiveGoalSO)
        {
            return false;
        }

        if (!IncomingPassPlanning.TryBuildForcedIncomingPassReceivePlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        LogSummary("ForcedIncomingPassReceivePlan(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=nearBallCatchPhase)");
        return true;
    }

    private bool TryConvertEmptyPlanToForcedIncomingPassReceive(GoapGoalSO goal, ref Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count > 0 || goal == null)
        {
            return false;
        }

        if (goal is not IncomingPassReceiveGoalSO)
        {
            return false;
        }

        if (!IncomingPassPlanning.NeedsForcedIncomingPassReceivePlan(_playerBlackboard))
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(goal, _availableActions);
        if (!IncomingPassPlanning.TryBuildForcedIncomingPassReceivePlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return false;
        }

        plan = forcedPlan;
        LogSummary("ForcedIncomingPassReceivePlan(action=" +
            (forcedPlan.Count > 0 ? forcedPlan.Peek().ActionName : "-") + ", reason=emptyPlanNearBall)");
        return true;
    }

    /// <summary>
    /// BallPossessionAttack は後方連鎖プランにドリブルが入らないため、Pass/Shoot/Dribble の最低コスト強制プランと比較する。
    /// </summary>
    private Queue<GoapActionSO> PreferForcedBallPossessionAttackPlan(
        GoapGoalSO goal,
        List<GoapActionSO> goalActions,
        Queue<GoapActionSO> plannerPlan)
    {
        if (goal is not BallPossessionAttackGoalSO
            || !MainNpcAttackPlanning.IsActivelyHoldingBall(_playerBlackboard)
            || !MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> forcedPlan)
            || forcedPlan == null
            || forcedPlan.Count == 0)
        {
            return plannerPlan;
        }

        if (plannerPlan == null || plannerPlan.Count == 0)
        {
            return forcedPlan;
        }

        float forcedCost = ComputePlanTotalCost(forcedPlan);
        float plannerCost = ComputePlanTotalCost(plannerPlan);
        return forcedCost < plannerCost ? forcedPlan : plannerPlan;
    }

    /// <summary>
    /// プランナーが選んだ ShootAtGoal が実行不可（シュート進行中など）のとき、Pass/Dribble へ差し替える。
    /// </summary>
    private Queue<GoapActionSO> SanitizeBallPossessionAttackPlan(
        GoapGoalSO goal,
        List<GoapActionSO> goalActions,
        Queue<GoapActionSO> plan)
    {
        if (goal is not BallPossessionAttackGoalSO
            || plan == null
            || plan.Count == 0
            || plan.Peek() is not ShootAtGoalActionSO
            || MainNpcAttackPlanning.CanExecuteShootAtGoal(_playerBlackboard))
        {
            return plan;
        }

        if (!MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> fallback,
                excludeShoot: true)
            || fallback == null
            || fallback.Count == 0)
        {
            return plan;
        }

        LogSummary("ShootPlanSanitized(substitute=" + fallback.Peek().ActionName + ")");
        return fallback;
    }

    private static bool TryBuildForcedTacticalPlanForGoal(
        PlayerBlackboard bb,
        List<GoapActionSO> goalActions,
        out Queue<GoapActionSO> plan)
    {
        if (TeammateNpcSupportPlanning.TryBuildForcedTacticalSupportPlan(bb, goalActions, out plan))
        {
            return true;
        }

        if (TeammateNpcDefensePlanning.TryBuildForcedTacticalDefensePlan(bb, goalActions, out plan))
        {
            return true;
        }

        if (MainNpcPostPassPlanning.TryBuildForcedFreeBallRecoveryPlan(bb, goalActions, out plan))
        {
            return true;
        }

        return MainNpcAttackPlanning.IsActivelyHoldingBall(bb)
            && MainNpcAttackPlanning.TryBuildForcedAttackPlan(bb, goalActions, out plan);
    }

    /// <summary>パス出し直後は TeamBallSupport を優先して選ぶ。</summary>
    private (Queue<GoapActionSO> plan, GoapGoalSO goal) TrySelectPostPassSupportPlan()
    {
        var supportGoal = _availableGoals.FirstOrDefault(g => g is TeamBallSupportGoalSO);
        bool supportContextActive = MainNpcPostPassPlanning.IsTeamBallSupportContext(_playerBlackboard)
            || Time.time < _postPassSupportGraceUntil;
        if (supportGoal == null
            || (!supportGoal.IsAchievable(_playerBlackboard) && !supportContextActive))
        {
            return (null, null);
        }

        _lastSelectedGoalName = supportGoal.GoalName;
        var goalActions = FilterActionsForGoal(supportGoal, _availableActions);
        if (goalActions == null || goalActions.Count == 0)
        {
            return (null, supportGoal);
        }

        foreach (GoapActionSO action in goalActions)
        {
            action?.EnsurePlanningFactsConfigured();
        }

        var plans = _planner.Plan(
            goalActions,
            _playerBlackboard,
            supportGoal.GetPlanningRequiredFacts(_playerBlackboard));
        if (plans != null && plans.Count > 0)
        {
            var bestPlan = SelectBestPlanFromPlans(plans);
            if (bestPlan != null && bestPlan.Count > 0)
            {
                LogPlanCostSummary(supportGoal, goalActions, plans, bestPlan);
                return (bestPlan, supportGoal);
            }
        }

        if (MainNpcPostPassPlanning.TryBuildForcedPostPassSupportPlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> forcedPlan)
            && forcedPlan != null
            && forcedPlan.Count > 0)
        {
            LogSummary("ForcedMainPostPassSupportPlan(action=" + forcedPlan.Peek().ActionName + ", reason=PassIssued)");
            return (forcedPlan, supportGoal);
        }

        return (null, supportGoal);
    }

    /// <summary>受け失敗直後は Support / FreeBall / 守備を優先して選ぶ（nogal 固着防止）。</summary>
    private (Queue<GoapActionSO> plan, GoapGoalSO goal) TrySelectPostReceiveRecoveryPlan()
    {
        var recoveryGoals = new[]
        {
            _availableGoals.FirstOrDefault(g => g is TeamBallSupportGoalSO),
            _availableGoals.FirstOrDefault(g => g is FreeBallRecoveryGoalSO),
            _availableGoals.FirstOrDefault(g => g is DefensivePositioningGoalSO),
            _availableGoals.FirstOrDefault(g => g is EnemyBallDefenseGoalSO),
        };

        foreach (var goal in recoveryGoals)
        {
            if (goal == null || !goal.IsAchievable(_playerBlackboard))
            {
                continue;
            }

            _lastSelectedGoalName = goal.GoalName;
            var goalActions = FilterActionsForGoal(goal, _availableActions);
            if (goalActions == null || goalActions.Count == 0)
            {
                continue;
            }

            foreach (GoapActionSO action in goalActions)
            {
                action?.EnsurePlanningFactsConfigured();
            }

            var plans = _planner.Plan(
                goalActions,
                _playerBlackboard,
                goal.GetPlanningRequiredFacts(_playerBlackboard));
            if (plans != null && plans.Count > 0)
            {
                var bestPlan = SelectBestPlanFromPlans(plans);
                if (bestPlan != null && bestPlan.Count > 0)
                {
                    LogPlanCostSummary(goal, goalActions, plans, bestPlan);
                    LogSummary("PostReceiveRecoveryPlan(goal=" + goal.GoalName + ", reason=PassReceiveFailed)");
                    return (bestPlan, goal);
                }
            }

            if (TryBuildForcedTacticalPlanForGoal(_playerBlackboard, goalActions, out Queue<GoapActionSO> forcedPlan)
                && forcedPlan != null
                && forcedPlan.Count > 0)
            {
                LogSummary("PostReceiveRecoveryPlan(goal=" + goal.GoalName
                    + ", action=" + forcedPlan.Peek().ActionName
                    + ", reason=PassReceiveFailedForced)");
                return (forcedPlan, goal);
            }

            if (goal is TeamBallSupportGoalSO
                && MainNpcPostPassPlanning.TryBuildForcedPostPassSupportPlan(
                    _playerBlackboard,
                    goalActions,
                    out Queue<GoapActionSO> forcedSupportPlan)
                && forcedSupportPlan != null
                && forcedSupportPlan.Count > 0)
            {
                LogSummary("PostReceiveRecoveryPlan(goal=TeamBallSupport, action="
                    + forcedSupportPlan.Peek().ActionName
                    + ", reason=PassReceiveFailedSupport)");
                return (forcedSupportPlan, goal);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// プランニング時の動的コストを GoapSummary に出力する（候補アクション単体コスト + 成立プラン総コスト）。
    /// planCandidates の * は選択されたプラン。
    /// </summary>
    private void LogPlanCostSummary(
        GoapGoalSO goal,
        List<GoapActionSO> scopedActions,
        List<Queue<GoapActionSO>> plans,
        Queue<GoapActionSO> selectedPlan)
    {
        string goalName = goal != null ? goal.GoalName : "-";
        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(_playerBlackboard);
        string actionsPart = BuildScopedActionCostsPart(scopedActions);
        string overlapPart = BuildSupportOverlapPart(goal, scopedActions);
        string plansPart = BuildPlanCandidatesCostPart(plans, selectedPlan);
        string selectedPath = FormatPlanPath(selectedPlan);
        float selectedCost = ComputePlanTotalCost(selectedPlan);
        string defenseDiagPart = BuildDefenseOverextensionDiagnosticPart(goal);
        string mainNpcDiagPart = BuildMainNpcPlaytestDiagnosticPart(goal);
        LogSummary(
            $"PlanCosts(goal={goalName}, tier={_npcTier}, slot={slot}, {defenseDiagPart}{mainNpcDiagPart}{overlapPart}{actionsPart}, {plansPart}, selected={selectedPath}:{selectedCost:F2})");
    }

    private string BuildDefenseOverextensionDiagnosticPart(GoapGoalSO goal)
    {
        if (goal == null
            || goal.GoalName != "DefensivePositioning"
            || _playerBlackboard == null)
        {
            return string.Empty;
        }

        TeammateNpcDefensePlanning.DefensiveRetreatOverextensionDiagnostic diagnostic =
            TeammateNpcDefensePlanning.GetRetreatOverextensionDiagnostic(_playerBlackboard);
        if (!diagnostic.HasSample)
        {
            return string.Empty;
        }

        return "defenseDiag="
            + $"urgency:{diagnostic.RetreatUrgency:F2},"
            + $"ahead:{diagnostic.AheadRatio:F2},"
            + $"sigAhead:{diagnostic.IsSignificantlyAhead}, ";
    }

    private string BuildMainNpcPlaytestDiagnosticPart(GoapGoalSO goal)
    {
        if (_npcTier != GoapNpcTier.Main
            || goal == null
            || _playerBlackboard == null)
        {
            return string.Empty;
        }

        string goalName = goal.GoalName;
        if (goalName != "TeamBallSupport"
            && goalName != "FreeBallRecovery"
            && goalName != "BallPossessionAttack")
        {
            return string.Empty;
        }

        MainNpcPostPassPlanning.MainNpcPlaytestDiagnostic diagnostic =
            MainNpcPostPassPlanning.GetPlaytestDiagnostic(_playerBlackboard);
        if (!diagnostic.HasSample)
        {
            return string.Empty;
        }

        string productionTag = IsProductionMainNpc()
            ? BuildProductionMainNpcDiagnosticTag(diagnostic)
            : IsEnemyMainNpc()
                ? BuildProductionMainNpcDiagnosticTag(diagnostic)
                : string.Empty;
        return "mainNpcDiag="
            + productionTag
            + $"ctx:{diagnostic.ContextTag},"
            + $"needsMove:{diagnostic.NeedsSupportMovement},"
            + $"canPass:{diagnostic.CanPass},"
            + $"canShoot:{diagnostic.CanShoot},"
            + $"press:{diagnostic.Pressure},"
            + $"passAdj:{diagnostic.PassCostAdjustment:F2},"
            + $"shootAdj:{diagnostic.ShootCostAdjustment:F2}, ";
    }

    /// <summary>TeamBallSupport 時: アクション別予測位置の重なりコスト（B の検証用）。</summary>
    private string BuildSupportOverlapPart(GoapGoalSO goal, List<GoapActionSO> scopedActions)
    {
        if (goal == null || goal.GoalName != "TeamBallSupport" || scopedActions == null || scopedActions.Count == 0)
        {
            return string.Empty;
        }

        var others = TeammateNpcGoapRoleDifferentiation.CollectOtherTeammateFieldPositions(_playerBlackboard);
        var parts = scopedActions
            .Select(a =>
            {
                Vector3 target = TeammateNpcGoapRoleDifferentiation.PredictActionTargetForCost(
                    _playerBlackboard, TeammateNpcTacticalMode.Support, a);
                float overlap = TeammateNpcGoapRoleDifferentiation.ComputeOverlapCost(target, others);
                return $"{a.ActionName}:{overlap:F1}";
            })
            .OrderBy(p => p);
        return "overlap=" + string.Join(",", parts) + ", ";
    }

    private string BuildScopedActionCostsPart(List<GoapActionSO> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return "actionCosts=none";
        }

        var parts = actions
            .Select(a => (name: a.ActionName, cost: a.CalculateDynamicCost(_playerBlackboard)))
            .OrderBy(x => x.cost)
            .Select(x => $"{x.name}:{x.cost:F2}");
        return "actionCosts=" + string.Join(",", parts);
    }

    private string BuildPlanCandidatesCostPart(List<Queue<GoapActionSO>> plans, Queue<GoapActionSO> selectedPlan)
    {
        if (plans == null || plans.Count == 0)
        {
            return "planCandidates=none";
        }

        var entries = new List<(float cost, string text)>();
        foreach (var plan in plans)
        {
            if (plan == null)
            {
                continue;
            }

            float cost = ComputePlanTotalCost(plan);
            string path = FormatPlanPath(plan);
            bool isSelected = selectedPlan != null && ReferenceEquals(plan, selectedPlan);
            entries.Add((cost, $"{path}:{cost:F2}{(isSelected ? "*" : "")}"));
        }

        if (entries.Count == 0)
        {
            return "planCandidates=none";
        }

        entries.Sort((a, b) => a.cost.CompareTo(b.cost));
        return "planCandidates=" + string.Join("|", entries.Select(e => e.text));
    }

    private float ComputePlanTotalCost(Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count == 0)
        {
            return 0f;
        }

        return plan.Sum(action => action.CalculateDynamicCost(_playerBlackboard));
    }

    private static string FormatPlanPath(Queue<GoapActionSO> plan)
    {
        if (plan == null || plan.Count == 0)
        {
            return "empty";
        }

        return string.Join(">", plan.Select(a => a.ActionName));
    }
    
        // 最適ゴールの選択（旧メソッド - 後方互換性のため残す）
        private GoapGoalSO SelectBestGoal()
    {
        DebugLogger.Log($"[{this.name}(GoapAgent)] 可能ゴール数:" + _availableGoals.Count);
        // 可能ゴールがない
        if (_availableGoals.Count == 0) return null;
        // 最適ゴールと最高スコア
        GoapGoalSO bestGoal = null;
        float bestScore = float.MinValue;
        
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        foreach (var goal in _availableGoals)
        {
            // 計画生成前に「到達不可能」を除外する
            // （ここが無いと、必須Factを満たしてない/プレイヤー状態が不一致のゴールに対して
            // planner が NoPlanFromPlanner を連発しやすくなる）
            if (goal == null) continue;
            if (!goal.IsAchievable(_playerBlackboard)) continue;

            // ゴールスコアの中で最高の者を選ぶ
            float score = goal.EvaluatePriority(_playerBlackboard, teamBB);
            DebugLogger.Log($"[{this.name}(GoapAgent)] goal:" + goal + " スコア:" + score);
            if (score > bestScore)
            {
                bestScore = score;
                bestGoal = goal;
            }
        }
        
        return bestGoal;
    }
    
    // === アクション実行 ===
    // 次のアクションを実行する
    private void ExecuteNextAction()
    {
        DebugLogger.Log($"[{this.name}(GoapAgent)] 作ったプランのアクションの実行:"+_currentPlan.Count);
        // プランがない
        if (_currentPlan.Count == 0) return;
        // キューからプランを取得する
        _currentAction = _currentPlan.Dequeue();

        DebugLogger.Log($"[{this.name}(GoapAgent)] 実行するアクション:"+_currentAction.DisplayName);
        
        // アクションが実行可能かチェック
        if (!_currentAction.CanExecute(_playerBlackboard))
        {
            // 守備アクションの再実行クールダウン中は「失敗」ではなく「待機」にする
            if (_currentAction is MoveToDefensivePositionActionRuntime
                && MoveToDefensivePositionActionRuntime.IsInReTriggerCooldown(_playerBlackboard, out float remain))
            {
                _nextAllowedReplanTime = Mathf.Max(_nextAllowedReplanTime, Time.time + remain);
                LogSummary($"ActionDeferred(action={_currentAction.DisplayName}, reason=defense_cooldown, remain={remain:F2}s)");
                _currentAction = null;
                _planFailed = false;
                return;
            }

            if (_currentAction is ShootAtGoalActionRuntime
                && GoapMainNpcAttackBridge.ResolveFacade(_playerBlackboard) is AnimalFacade shootFacade
                && GoapBallActionGuard.IsShootInProgress(shootFacade))
            {
                _nextAllowedReplanTime = Mathf.Max(_nextAllowedReplanTime, Time.time + 0.15f);
                LogSummary($"ActionDeferred(action=ShootAtGoal, reason=shoot_in_progress)");
                _currentAction = null;
                _planFailed = false;
                return;
            }

            if (_currentAction is ShootAtGoalActionRuntime
                && TrySubstituteRejectedShootAtGoal())
            {
                return;
            }

            DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: アクション実行不可 -> {_currentAction.DisplayName}");
            if (IsMovementActionStaleReject(_currentAction))
            {
                RecordTacticalContextGrace(DebugCurrentGoalName);
                LogSummary($"ActionSkipped(action={_currentAction.DisplayName}, goal={DebugCurrentGoalName}, reason=context_changed)");
                _currentAction = null;
                _currentPlan.Clear();
                _planFailed = false;
                return;
            }

            LogSummary($"ActionRejected(action={_currentAction.DisplayName}, goal={DebugCurrentGoalName})");
            _currentAction = null;
            _planFailed = true;
            return;
        }
        
        // アクション開始
        _currentAction.Execute(_playerBlackboard);
        
        DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: アクション開始 -> {_currentAction.DisplayName}");
        LogSummary($"ActionStart(action={_currentAction.DisplayName}, goal={DebugCurrentGoalName})");
    }

    private bool TrySubstituteRejectedShootAtGoal()
    {
        if (_currentGoal is not BallPossessionAttackGoalSO)
        {
            return false;
        }

        var goalActions = FilterActionsForGoal(_currentGoal, _availableActions);
        if (!MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                _playerBlackboard,
                goalActions,
                out Queue<GoapActionSO> fallback,
                excludeShoot: true)
            || fallback == null
            || fallback.Count == 0)
        {
            return false;
        }

        _currentAction = null;
        _planFailed = false;
        foreach (GoapActionSO actionSO in fallback)
        {
            GoapActionRuntime runtime = actionSO.CreateRuntime(GetRuntimeDebugName(actionSO));
            if (runtime != null && runtime.CanExecute(_playerBlackboard))
            {
                _currentPlan.Enqueue(runtime);
            }
        }

        if (_currentPlan.Count == 0)
        {
            return false;
        }

        LogSummary("ShootRejectedSubstitute(action=" + fallback.Peek().ActionName + ")");
        ExecuteNextAction();
        return true;
    }

    // 現在のアクションを更新する
    private void UpdateCurrentAction()
    {
        // 現在のアクションがない/現在のゴールがない
        if (_currentAction == null) return;
        if (_currentGoal == null) return;
        
        // アクションの更新
        _currentAction.Update(Time.deltaTime);

        // アクションが完了したかチェック
        if (_currentAction.IsComplete())
        {
            string completedActionName = _currentAction.DisplayName;
            string completedGoalName = DebugCurrentGoalName;
            bool wasReceivePass = _currentAction is MoveToReceivePassActionRuntime;
            bool wasPassIssued = _currentAction is PassToTeammateActionRuntime;
            bool wasShootAtGoal = _currentAction is ShootAtGoalActionRuntime;

            if (wasReceivePass)
            {
                _pendingReceivePassTransitionLog = true;
                _lastReceiveActionCompleteTime = Time.time;
                var receiveAction = (MoveToReceivePassActionRuntime)_currentAction;
                _lastReceiveFinishReason = string.IsNullOrEmpty(receiveAction.LastFinishReason)
                    ? "unknown"
                    : receiveAction.LastFinishReason;
                _lastReceiveOutcomeReceived = IncomingPassPlanning.HasReceivedIncomingPass(_playerBlackboard);
                // 受け切れなくてもトラッカーを残すと IncomingPassTarget で Support が塞がり NoGoal 固着する。
                GoapPassFlightTracker.Clear();

                LogSummary(
                    "ReceivePassOutcome("
                    + $"finishReason={_lastReceiveFinishReason}, "
                    + $"received={_lastReceiveOutcomeReceived.ToString().ToLower()}, "
                    + $"hasBallFact={(_playerBlackboard.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true).ToString().ToLower()}"
                    + ")");

                TriggerImmediateReplan(
                    "PassReceiveComplete",
                    $"PassReceiveComplete(received={_lastReceiveOutcomeReceived.ToString().ToLower()}, reason={_lastReceiveFinishReason})",
                    allowDuringDeferredAction: true);
            }
            else if (wasPassIssued)
            {
                _currentGoal = null;
                _postPassSupportGraceUntil = Time.time + _postPassSupportGraceSeconds;
                TriggerImmediateReplan(
                    "PassIssued",
                    "PassIssued(passer_released_ball)",
                    allowDuringDeferredAction: true);
            }
            else if (wasShootAtGoal)
            {
                _postShootDefenseGraceUntil = Time.time + _postShootDefenseGraceSeconds;
            }

            DebugLogger.Log($"[{this.name}(GoapAgent)] GoapAgent: アクション完了 -> {completedActionName}");
            LogSummary($"ActionComplete(action={completedActionName}, goal={completedGoalName})");

            _currentAction = null;

            if (_currentGoal != null && !_currentGoal.IsAchievable(_playerBlackboard))
            {
                _planFailed = true;
            }
        }
    }

    // === 段階0: 頭上デバッグラベル用 ===
    public string DebugCurrentGoalName => _currentGoal != null
        ? _currentGoal.GoalName
        : (!string.IsNullOrEmpty(_lastSelectedGoalName) ? _lastSelectedGoalName : "-");

    public string DebugCurrentActionName
    {
        get
        {
            if (_isPlanning)
            {
                return "Planning";
            }

            if (_currentAction != null)
            {
                return _currentAction.DisplayName;
            }

            if (_planFailed)
            {
                return $"PlanFailed({_lastReplanReason})";
            }

            if (_currentPlan != null && _currentPlan.Count > 0)
            {
                return $"Queued×{_currentPlan.Count}";
            }

            return "-";
        }
    }

    public bool DebugIsPlanning => _isPlanning;
    public int DebugPlanQueueCount => _currentPlan != null ? _currentPlan.Count : 0;
    public bool DebugPlanFailed => _planFailed;
    public string DebugLastReplanReason => _lastReplanReason;
    public string DebugLastPlanSummary => _lastPlanSummary;
    public string DebugLastFailureCategory => _lastFailureCategory;
    public string DebugLastFailureDetails => _lastFailureDetails;
    public string DebugPlanningStats =>
        $"A:{_planningAttemptCount} S:{_planningSuccessCount} F:{_planningFailureCount}";

    /// <summary>GOAPがプラン実行中か（段階2: 簡易移動との排他判定用）。</summary>
    public bool IsActivelyControlling =>
        enabled
        && (_isPlanning
            || _currentAction != null
            || (_currentPlan != null && _currentPlan.Count > 0));

    /// <summary>段階2パイロット用: ゴール・アクション・再計画間隔を注入する。</summary>
    public void ConfigurePilot(
        IReadOnlyList<GoapGoalSO> goals,
        IReadOnlyList<GoapActionSO> actions,
        float planningInterval,
        GoapNpcTier npcTier = GoapNpcTier.Sub)
    {
        _npcTier = npcTier;

        if (goals != null && goals.Count > 0)
        {
            _availableGoals = goals.Where(g => g != null).ToList();
        }
        else if (goals != null)
        {
            _availableGoals = new List<GoapGoalSO>();
        }

        if (actions != null && actions.Count > 0)
        {
            _availableActions = actions.Where(a => a != null).ToList();
        }
        else if (actions != null)
        {
            _availableActions = new List<GoapActionSO>();
        }

        EnsurePilotFallbackSet();

        _planningInterval = Mathf.Max(0.5f, planningInterval);
        _planFailed = false;
        _isPlanning = false;
        _lastReplanReason = "ConfigurePilot";
        _lastFailureSignature = "-";
        _sameFailureStreak = 0;
        _nextAllowedReplanTime = 0f;
        _ballContextInitialized = false;
        _lastPlanSummary =
            $"Configured(tier={_npcTier}, goals={_availableGoals.Count}, actions={_availableActions.Count}, interval={_planningInterval:F1})";
        if (!ShouldDeferPlanInterrupt())
        {
            ClearPlanForReconfigure();
        }
    }

    private bool ShouldDeferPlanInterrupt()
    {
        if (_currentAction == null || _playerBlackboard == null)
        {
            return false;
        }

        if (ShouldDeferInterruptForRuntime(_currentAction))
        {
            return true;
        }

        if (!GoapBallActionGuard.IsCommittedGoapAction(_currentAction))
        {
            return false;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(_playerBlackboard);
        return GoapBallActionGuard.IsAnyInProgress(facade) || HasUnfinishedCommittedBallAction;
    }

    /// <summary>パス／シュート GOAP アクションが ActionComplete するまで Update を継続する。</summary>
    public bool HasUnfinishedCommittedBallAction =>
        _currentAction != null && GoapBallActionGuard.IsCommittedGoapAction(_currentAction);

    /// <summary>パス受け移動中は外部からの Abort / 再計画を抑止する。</summary>
    public bool HasUnfinishedIncomingPassReceive =>
        _currentAction is MoveToReceivePassActionRuntime;

    /// <summary>ドリブル・戦術移動中は外部からの Abort を抑止する。</summary>
    public bool HasUnfinishedDeferredTacticalAction =>
        ShouldDeferInterruptForRuntime(_currentAction);

    /// <summary>AIContextSwitcher 等の外部中断判定用。</summary>
    public bool ShouldDeferExternalPlanInterrupt => ShouldDeferPlanInterrupt();

    /// <summary>
    /// プレイ中に途中中断すると破綻するアクションのみ Abort を遅延する。
    /// サポート移動（GetOpen/CSA/MTS 等）はバッチ検証のパターン切替で即 Abort できるよう対象外。
    /// </summary>
    private static bool ShouldDeferInterruptForRuntime(GoapActionRuntime action)
    {
        if (action == null)
        {
            return false;
        }

        return action is DribbleTowardGoalActionRuntime
            || action is MoveToReceivePassActionRuntime;
    }

    public GoapNpcTier DebugNpcTier => _npcTier;

    private List<GoapActionSO> FilterActionsForGoal(GoapGoalSO goal, List<GoapActionSO> actions)
    {
        if (IsEnemyGoapFieldNpc())
        {
            return GoapEnemyNpcCatalog.FilterActionsForGoal(goal, actions, _npcTier);
        }

        return _npcTier == GoapNpcTier.Main
            ? GoapMainNpcCatalog.FilterActionsForGoal(goal, actions)
            : GoapTeammateNpcCatalog.FilterActionsForGoal(goal, actions);
    }

    private static bool IsMovementActionStaleReject(GoapActionRuntime action)
    {
        return GoapTeammateNpcCatalog.IsTacticalMoveRuntime(action);
    }

    /// <summary>パスレーン遮断が急なとき、同コストなら MTD より専用守備アクションを優先する。</summary>
    private bool ShouldPreferDefensePlanOnCostTie(Queue<GoapActionSO> candidate, Queue<GoapActionSO> currentBest)
    {
        if (candidate == null || candidate.Count == 0 || currentBest == null || currentBest.Count == 0)
        {
            return false;
        }

        if (TeammateNpcDefensePlanning.ComputePassLaneBlockUrgency(_playerBlackboard) < 0.75f)
        {
            return false;
        }

        return DefenseActionCostTieRank(candidate.Peek()) < DefenseActionCostTieRank(currentBest.Peek());
    }

    private static int DefenseActionCostTieRank(GoapActionSO action)
    {
        string name = action != null ? action.ActionName : string.Empty;
        if (name == "BlockPassLane")
        {
            return 0;
        }

        if (name == "MarkOpponent")
        {
            return 1;
        }

        if (name == "BlockShotLane")
        {
            return 2;
        }

        if (name == "MoveToDefensivePosition")
        {
            return 3;
        }

        return 10;
    }

    private static string GetRuntimeDebugName(GoapActionSO actionSO)
    {
        if (actionSO == null)
        {
            return "(NoAction)";
        }

        if (!string.IsNullOrEmpty(actionSO.ActionName))
        {
            return actionSO.ActionName;
        }

        return actionSO.name;
    }

    private void EnsurePilotFallbackSet()
    {
        if (_availableGoals == null)
        {
            _availableGoals = new List<GoapGoalSO>();
        }
        if (_availableActions == null)
        {
            _availableActions = new List<GoapActionSO>();
        }

        if (IsEnemyGoapFieldNpc())
        {
            GoapEnemyNpcCatalog.NormalizeLists(_availableGoals, _availableActions, _npcTier);
            return;
        }

        if (_npcTier == GoapNpcTier.Main)
        {
            GoapMainNpcCatalog.NormalizeLists(_availableGoals, _availableActions);
            return;
        }

        GoapTeammateNpcCatalog.NormalizeLists(_availableGoals, _availableActions);
    }

    private bool IsEnemyGoapFieldNpc()
    {
        if (_playerBlackboard?.BasicData?.Self == null)
        {
            return false;
        }

        var facade = _playerBlackboard.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? _playerBlackboard.BasicData.Self.GetComponent<AnimalFacade>();
        return GoapFieldNpcPerspective.IsEnemyFieldNpc(facade);
    }

    private bool IsEnemyMainNpc()
    {
        if (_npcTier != GoapNpcTier.Main)
        {
            return false;
        }

        var self = _playerBlackboard?.BasicData?.Self;
        var facade = self != null ? self.GetComponent<AnimalFacade>() : null;
        return GoapEnemyMainNpcPlanning.IsEnemyMainPlayer(facade);
    }

    private bool IsProductionMainNpc()
    {
        if (_npcTier != GoapNpcTier.Main || !GoapMainNpcProductionEnvironment.IsActive)
        {
            return false;
        }

        var self = _playerBlackboard?.BasicData?.Self;
        var facade = self != null ? self.GetComponent<AnimalFacade>() : null;
        return GoapMainNpcProductionEnvironment.IsProductionMainPlayer(facade);
    }

    private static string BuildProductionMainNpcDiagnosticTag(
        MainNpcPostPassPlanning.MainNpcPlaytestDiagnostic diagnostic)
    {
        if (diagnostic.ContextTag == "Attack")
        {
            return "prodM1:True,";
        }

        if (diagnostic.ContextTag == "Support" || diagnostic.ContextTag == "FreeBall")
        {
            return "prodM2:True,";
        }

        return string.Empty;
    }

    // === プラン中断 ===
    public void AbortCurrentPlan()
    {
        ClearCurrentPlanState(logAsAbort: true, summaryReason: "Abort");
    }

    private void ClearPlanForReconfigure()
    {
        ClearCurrentPlanState(logAsAbort: false, summaryReason: "ConfigurePilot");
    }

    private void ClearCurrentPlanState(bool logAsAbort, string summaryReason)
    {
        EnsureInitialized();

        if (ShouldDeferPlanInterrupt())
        {
            LogSummary($"AbortDeferred(action={_currentAction.DisplayName})");
            return;
        }

        bool hadAction = _currentAction != null;
        bool hadPlan = _currentPlan != null && _currentPlan.Count > 0;

        if (_currentAction != null)
        {
            _currentAction.Cancel();
            _currentAction = null;
        }

        _currentPlan?.Clear();

        if (!hadAction && !hadPlan)
        {
            return;
        }

        _planFailed = true;
        _lastReplanReason = summaryReason;
        _lastFailureCategory = logAsAbort ? "Abort" : "None";
        _lastFailureDetails = logAsAbort ? "AbortCurrentPlan called" : summaryReason;

        if (logAsAbort)
        {
            _lastPlanSummary = $"Aborted(attempt={_planningAttemptCount}, t={Time.time:F1})";
            LogSummary(_lastPlanSummary);
            DebugLogger.Log($"[{this.name}(GoapAgent)] プラン中断");
            return;
        }

        _lastPlanSummary = $"PlanCleared(reason={summaryReason}, t={Time.time:F1})";
        if (hadAction)
        {
            LogSummary(_lastPlanSummary);
        }
    }

    /// <summary>検証レイアウト適用後: クールダウンを解除して即再プランニング可能にする。</summary>
    public void ResetPlanningStateForVerification()
    {
        EnsureInitialized();
        StopAllCoroutines();
        _isPlanning = false;
        _currentAction = null;
        _currentPlan?.Clear();
        _nextAllowedReplanTime = 0f;
        _lastPlanningTime = 0f;
        _planFailed = true;
        _lastReplanReason = "VerificationReset";
        _lastFailureCategory = "None";
        _lastFailureDetails = "-";
    }

    private static string ResolveReceivePassTransition(string goalName)
    {
        if (string.IsNullOrEmpty(goalName))
        {
            return "other";
        }

        return goalName switch
        {
            "BallPossessionAttack" => "attack",
            "TeamBallSupport" => "support",
            "FreeBallRecovery" => "freeball",
            "DefensivePositioning" => "defense",
            "EnemyBallDefense" => "defense",
            "IncomingPassReceive" => "receive_retry",
            _ => "other",
        };
    }

    private void LogReceivePassTransition(string transition)
    {
        string goal = _lastSelectedGoalName ?? "-";
        LogSummary(
            "ReceivePassTransition("
            + $"received={_lastReceiveOutcomeReceived.ToString().ToLower()}, "
            + $"finishReason={_lastReceiveFinishReason}, "
            + $"goal={goal}, "
            + $"transition={transition}"
            + ")");
    }

    private void LogSummary(string message)
    {
        if (!GoapRuntimeDiagnostics.SummaryLoggingEnabled)
        {
            return;
        }

        if (!GoapRuntimeDiagnostics.VerboseLoggingEnabled
            && !GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog(message))
        {
            return;
        }

        string actor = GetActorIdentity();
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{SummaryTag}] [{actor}] {message}";
        if (GoapRuntimeDiagnostics.VerboseLoggingEnabled)
        {
            Debug.Log(line);
        }

        AppendSummaryToFile(line);
    }

    private bool HasValidPlanningSetup()
    {
        return _availableGoals != null
            && _availableActions != null
            && _availableGoals.Count > 0
            && _availableActions.Count > 0;
    }

    private string GetActorIdentity()
    {
        string ownerName = "UnknownOwner";
        string playerId = "unknown";
        var blackboard = _playerBlackboard != null ? _playerBlackboard : GetComponentInChildren<PlayerBlackboard>(true);
        if (blackboard != null && blackboard.BasicData != null)
        {
            if (blackboard.BasicData.Self != null)
            {
                ownerName = blackboard.BasicData.Self.name;
            }
            playerId = blackboard.BasicData.PlayerID.ToString();
        }

        return $"{name}#{GetInstanceID()}|owner={ownerName},playerId={playerId}";
    }

    private void SetPlanningFailure(string category, string details)
    {
        _lastFailureCategory = category;
        _lastFailureDetails = string.IsNullOrEmpty(details) ? "-" : details;
        LogSummary($"FailureReason(category={_lastFailureCategory}, details={_lastFailureDetails})");
    }

    private void ApplyFailureCooldown()
    {
        string signature = $"{_lastFailureCategory}|{_lastFailureDetails}";
        if (signature == _lastFailureSignature)
        {
            _sameFailureStreak++;
        }
        else
        {
            _lastFailureSignature = signature;
            _sameFailureStreak = 1;
        }

        float cooldown = Mathf.Clamp(_baseFailureCooldown * _sameFailureStreak, _baseFailureCooldown, _maxFailureCooldown);
        _nextAllowedReplanTime = Time.time + cooldown;
        LogSummary($"ReplanCooldown(seconds={cooldown:F2}, streak={_sameFailureStreak}, category={_lastFailureCategory})");
    }

    private string BuildMissingFactsSummary(GoapGoalSO goal)
    {
        var planningFacts = goal != null ? goal.GetPlanningRequiredFacts(_playerBlackboard) : null;
        if (goal == null || planningFacts == null || planningFacts.Count == 0 || _playerBlackboard == null || _playerBlackboard._workingMemory == null)
        {
            return "-";
        }

        var missing = new List<string>();
        bool hasBallTrueToFalse = false;
        foreach (var cond in planningFacts)
        {
            var fact = new Fact(cond.Tag, cond.ExpectedValue.ToString().ToLower());
            bool? current = _playerBlackboard._workingMemory.GetFact(fact);
            if (current != cond.ExpectedValue)
            {
                string currentText = current.HasValue ? current.Value.ToString().ToLower() : "null";
                missing.Add($"{cond.Tag}:{currentText}->{cond.ExpectedValue.ToString().ToLower()}");
                // タグ表記ゆれ（hasBall / has_ball / Basic.hasBall など）に耐えるため、
                // 小文字化 + 英数字のみで正規化して hasball を判定する。
                string normalizedTag = NormalizeTag(cond.Tag);
                bool isHasBallTag = normalizedTag.Contains("hasball");
                if (isHasBallTag && cond.ExpectedValue && current == false)
                {
                    hasBallTrueToFalse = true;
                }
            }
        }

        if (missing.Count == 0)
        {
            return "none";
        }

        string missingSummary = string.Join("|", missing);
        if (!hasBallTrueToFalse)
        {
            return missingSummary;
        }

        return $"{missingSummary};hasBallContext={BuildHasBallContextSummary()}";
    }

    private string BuildHasBallContextSummary()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        string teamHasBall = teamBB != null ? teamBB.BallInfo.TeamHasBall.ToString().ToLower() : "unknown";
        string enemyHasBall = teamBB != null ? teamBB.BallInfo.EnemyHasBall.ToString().ToLower() : "unknown";
        string ballState = teamBB != null ? teamBB.BallInfo.BallState.ToString() : "Unknown";
        string ownerId = teamBB != null ? teamBB.BallInfo.BallOwnerID.ToString() : "unknown";
        string playerId = _playerBlackboard != null ? _playerBlackboard.BasicData.PlayerID.ToString() : "unknown";
        string selfHasBall = _playerBlackboard != null ? _playerBlackboard.BallState.HasBall.ToString().ToLower() : "unknown";
        return $"teamHasBall={teamHasBall},enemyHasBall={enemyHasBall},ballState={ballState},ownerId={ownerId},playerId={playerId},selfHasBall={selfHasBall}";
    }

    private static string NormalizeTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return string.Empty;
        }

        var chars = new List<char>(tag.Length);
        foreach (char c in tag.ToLowerInvariant())
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                chars.Add(c);
            }
        }
        return new string(chars.ToArray());
    }

    private static void AppendSummaryToFile(string line)
    {
        try
        {
            if (!_summaryLogInitialized)
            {
                string dir = Path.Combine(Application.dataPath, "DebugLog");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _summaryLogFilePath = Path.Combine(dir, "GoapSummary_latest.txt");
                File.WriteAllText(_summaryLogFilePath, string.Empty);
                _summaryLogInitialized = true;
            }

            File.AppendAllText(_summaryLogFilePath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{SummaryTag}] file write failed: {e.Message}");
        }
    }

    // === デバッグ情報 ===
    private void OnGUI()
    {
        if (!_debugMode) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"GOAP Agent Debug");
        GUILayout.Label($"Current Goal: {(_currentGoal?.GoalName ?? "None")}");
        GUILayout.Label($"Planning: {_isPlanning}");
        GUILayout.Label($"Plan Length: {_currentPlan.Count + (_currentAction != null ? 1 : 0)}");
        GUILayout.Label($"Current Action: {(_currentAction?.DisplayName ?? "None")}");
        GUILayout.Label($"Last Replan: {_lastReplanReason}");
        GUILayout.Label($"Last Plan: {_lastPlanSummary}");
        GUILayout.Label($"Stats: {DebugPlanningStats}");
        GUILayout.EndArea();
    }

    // _loggerのインスタンス変数は削除
}
