using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// PlayerBlackboardData.csで定義されたクラスを使用
// PlayerBasicData, PlayerPhysicalState, PlayerBallState, PlayerTacticalState, 
// PlayerSurroundingInfo, PlayerActionState, PlayerDerivedMetrics

// プレイヤー個人のブラックボード
public class PlayerBlackboard : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [Header("GOAP Debug")]
    [SerializeField] private bool _logBallOwnershipDiagnostics = true;
    [Header("HasBall Hysteresis")]
    [SerializeField] private int _releaseConfirmFrames = 18;
    [Tooltip("TeamBB の ownerId 未確定時、直前まで保持していた本人だけ短時間 hasBall を維持")]
    [SerializeField] private int _ownerUnknownHoldGraceFrames = 14;

    // === 新しいデータ構造 ===
    public PlayerBasicData BasicData = new();
    public PlayerPhysicalState PhysicalState = new();
    public PlayerBallState BallState = new();
    // public PlayerSkillState SkillState = new();
    // public PlayerTacticalState TacticalState = new();
    // public PlayerSurroundingInfo SurroundingInfo = new();
    public PlayerActionState ActionState = new();
    // public PlayerDerivedMetrics DerivedMetrics = new();
    // public PlayerTimeControlInfo TimeControl = new();

    // === GOAP推論専用ワーキングメモリ ===
    public WorkingMemory _workingMemory = new();
    // GOAP用WorkingMemory更新ロジック
    private PlayerWorkingMemoryUpdater _workingMemoryUpdater = new PlayerWorkingMemoryUpdater();
    private int _releaseCandidateStreak;
    private int _ownerUnknownHoldGraceCounter;
    
    // === 初期化 ===
    private void Start()
    {
        // 新しいデータ構造の初期化
        InitializeDataStructures();
    }

    // 新しいデータ構造の初期化
    private void InitializeDataStructures()
    {
        DebugLogger.Log($"[{this.name}(PlayerBlackboard)] データ構造の初期化");

        // 基本データの初期化
        BasicData.init(gameObject);
        // 物理状態の初期化
        PhysicalState.init(gameObject.transform.position);
        // ボール状態の初期化
        BallState.init();
        // アクション状態の初期化
        ActionState.init();
    }
    
    // 更新処理
    private void Update()
    {
        // プレイヤーの状態を更新
        UpdatePlayerState();
        // 戦術情報の更新
        UpdateTacticalInfo();
        // WorkingMemoryの更新
        RefreshWorkingMemory();
    }
    
    // プレイヤー状態の更新
    private void UpdatePlayerState()
    {
        if (BasicData.Self != null)
        {
            // 物理状態の更新
            Vector3 pos = BasicData.Self.transform.position;
            Vector3 v = _rigidbody?.linearVelocity ?? Vector3.zero;
            PhysicalState.updatePhysicalInfo(pos, v);
            
            // ボールとの距離を計算（TeamFacade 経由で TeamBlackboard を参照）
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB != null && teamBB.BallInfo.IsExistBall)
            {
                Vector3 ballPos = teamBB.BallInfo.BallPosition;
                float distance = Vector3.Distance(pos, ballPos);
                // ボールの方向を計算（正規化されたベクトル）
                Vector3 ballDirection = (ballPos - pos).normalized;
                // ボール保持状態: BallOwnerID と ViewID の一致のみ（位置フォールバックは使わない）
                int ownerId = teamBB.BallInfo.BallOwnerID;
                int playerId = BasicData.PlayerID;
                bool idMatch = ownerId >= 0 && ownerId == playerId;
                bool isHoldState = teamBB.BallInfo.BallState == BallManager_State.BALL_STATE.HOLD;
                bool previousHasBall = BallState.HasBall;
                bool hasBall = previousHasBall;

                if (idMatch)
                {
                    hasBall = true;
                    _releaseCandidateStreak = 0;
                    _ownerUnknownHoldGraceCounter = 0;
                }
                else if (!previousHasBall)
                {
                    hasBall = false;
                    _releaseCandidateStreak = 0;
                    _ownerUnknownHoldGraceCounter = 0;
                }
                else
                {
                    // 下降遷移（true -> false）は厳しめに確認
                    bool ownerUnknownGraceActive = ownerId < 0
                        && isHoldState
                        && _ownerUnknownHoldGraceCounter < _ownerUnknownHoldGraceFrames;
                    if (ownerUnknownGraceActive)
                    {
                        _ownerUnknownHoldGraceCounter++;
                        hasBall = true;
                        _releaseCandidateStreak = 0;
                    }
                    else
                    {
                        _ownerUnknownHoldGraceCounter = 0;
                        _releaseCandidateStreak++;
                        if (_releaseCandidateStreak >= _releaseConfirmFrames)
                        {
                            hasBall = false;
                            _releaseCandidateStreak = 0;
                        }
                    }
                }

                if (_logBallOwnershipDiagnostics)
                {
                    string diagLine =
                        $"[GOAP_DIAG][BallOwnership] ownerId={ownerId} playerId={playerId} hasBall={hasBall} " +
                        $"idMatch={idMatch} relStreak={_releaseCandidateStreak}/{_releaseConfirmFrames} " +
                        $"ownerUnknownGrace={_ownerUnknownHoldGraceCounter}/{_ownerUnknownHoldGraceFrames} " +
                        $"belongTeam={teamBB.BallInfo.BallBelongTeam} ballState={teamBB.BallInfo.BallState}";
                    Debug.Log(diagLine);
                    GoapDiagnosticLog.Write(diagLine);
                }

                // var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
                // DebugLogger.Log($"[{this.name}(PlayerBlackboard)] ボール保持状態更新: {hasBall} ID:{BasicData.PlayerID} ボール所持者ID:{(teamBB != null ? teamBB.BallInfo.BallOwnerID : -1)}");

                // ボール情報の更新
                BallState.updateBallInfo(hasBall, distance, ballDirection);
            }
        }
    }
    
    // 戦術情報の更新
    private void UpdateTacticalInfo()
    {
        // 戦術的な情報の更新（必要に応じて実装）
    }
    
    // アクション開始
    public void StartAction(string actionName)
    {
        // アクション開始時の処理（必要に応じて実装）
    }
    
    // アクション終了
    public void EndAction()
    {
        // アクション終了時の処理（必要に応じて実装）
    }
    
    // スタン状態にする
    public void Stun(float duration)
    {
        // スタン状態の処理（必要に応じて実装）
    }
    
    // === GOAP推論専用メソッド ===
    
    // Factの設定（GOAP推論用）
    public void SetFact(Fact fact, bool value) => _workingMemory.AssertFact(fact, value);
    
    // Factの取得（GOAP推論用）
    public bool? GetFact(Fact fact) => _workingMemory.GetFact(fact);

    // WorkingMemoryの更新
    public void RefreshWorkingMemory()
    {
        // 専用クラスにWorkingMemory更新処理を委譲
        _workingMemoryUpdater.Update(this);
    }

    /// <summary>
    /// ゴール条件のリストを受け取り、現在の状態との比較結果をDictionaryとして返す
    /// </summary>
    /// <param name="goalFacts">ゴール条件のリスト</param>
    /// <returns>ゴール条件と現在の状態の比較結果</returns>
    public Dictionary<GoapCondition, bool> GetGoalConditionStates(List<GoapCondition> goalFacts)
    {
        // 専用クラスにゴール条件評価処理を委譲
        return _workingMemoryUpdater.GetGoalConditionStates(this, goalFacts);
    }
}

