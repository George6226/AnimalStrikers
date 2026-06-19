using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// ボールを追跡する(途中アクション[Runtime])
public class ChaseBallActionRuntime : GoapActionRuntime
{
    private bool _isChasing = false;
    private float _chaseStartTime;
    private float _maxChaseDistance;
    private PlayerBlackboard _playerBlackboard; // 保存されたPlayerBlackboard
    private AnimalComponentManager _animalComponent;
    
    public ChaseBallActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName) 
    {
        var chaseSO = origin as ChaseBallActionSO;
        if (chaseSO != null)
        {
            // ScriptableObjectから設定値を取得
            _maxChaseDistance = chaseSO.MaxChaseDistance;
        }
    }
    
    public override bool CanExecute(PlayerBlackboard bb)
    {
        // ボールを持っていない
        //if (bb.hasBall) return false;
        
        //// スタン状態でない
        //if (bb.isStunned) return false;
        
        //// スタミナが少しある
        //if (bb.stamina < 15f) return false;
        
        //// ボールが近くにある
        //if (bb.ballDistance > _maxChaseDistance) return false;
        
        // ボールがフリー状態
        //var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        //if (teamBB != null && teamBB.BallInfo.BallBelongTeam != BallManager.BELONG_TEAM.FREE) return false;
        
        return true;
    }
    
    public override void Execute(PlayerBlackboard bb)
    {
        // PlayerBlackboardを保存
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        
        // ボール追跡開始
        _isChasing = true;
        _chaseStartTime = Time.time;

        // ブラックボードの状態を更新
        //bb.StartAction("ChaseBall");
        //bb.isMoving = true;
        //bb.targetPosition = _ballPosition;
        
        // 効果を適用
        // _originSO.ApplyEffects(bb);
        
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        Vector3 logBallPos = teamBB != null ? teamBB.BallInfo.BallPosition : Vector3.zero;
        Debug.Log($"ボール追跡開始: {bb.BasicData.Self.name} -> {logBallPos}");
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isChasing) return;
        
        if (_playerBlackboard == null) return;
        
        // ボールの現在位置を更新
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        Vector3 ballPosition = teamBB != null ? teamBB.BallInfo.BallPosition : _playerBlackboard.PhysicalState.Position;
        
        // ボール追跡の実行
        Vector3 currentPos = _playerBlackboard.PhysicalState.Position;
        Vector3 direction = (ballPosition - currentPos);
        
        // AnimalHandlerのmoveメソッドを使用
        // per: 移動の強度（1.0fで最大速度）
        // speedMag: 速度倍率（1.0fで通常速度、2.0fでダッシュ速度）
        float per = 1.0f; // 最大速度で移動
        float speedMag = 1.0f; // 通常速度
        
        _animalComponent.Animal.move(per, speedMag);

        // キャラクターの向きをボールの方向に調整
        float radian = Mathf.Atan2(-direction.x, direction.z);
        _animalComponent.Animal.rotate(radian);
        
        // アクション進行度を更新
        float distanceToBall = Vector3.Distance(_playerBlackboard.PhysicalState.Position, ballPosition);
        _playerBlackboard.ActionState.SetActionProgress(Mathf.Clamp01(1f - (distanceToBall / _maxChaseDistance)));
    }
    
    public override bool IsComplete()
    {
        if (!_isChasing) return true;
        
        // 完了条件
        bool isComplete = false;

        // ボール所持状態をデバッグ出力
        DebugLogger.Log($"[{_debugName}(ChaseBallActionRuntime)] ボール所持状態: {_playerBlackboard.BallState.HasBall}");
        
        // 1. ボールをキャプチャした
        if (_playerBlackboard.BallState.HasBall)
        {
           // ボールを取得
           _playerBlackboard.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
           isComplete = true;
        }
        
        //// 2. ボールが遠すぎる
        //if (_playerBlackboard.ballDistance > _maxChaseDistance)
        //{
        //    isComplete = true;
        //}
        
        //// 3. スタミナ不足
        //if (_playerBlackboard.stamina < 5f)
        //{
        //    isComplete = true;
        //}
        
        //// 4. ボールが他のプレイヤーに取られた
        //var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        //if (teamBB != null && teamBB.BallInfo.BallBelongTeam != BallManager.BELONG_TEAM.FREE)
        //{
        //    isComplete = true;
        //}
        
        //// 5. 追跡が長時間続いている
        //float elapsedTime = Time.time - _chaseStartTime;
        //if (elapsedTime > 8f)
        //{
        //    isComplete = true;
        //}
        
        if (isComplete)
        {
           // ボール追跡終了
        //    _playerBlackboard.EndAction();
           //_playerBlackboard.isMoving = false;
           //_isChasing = false;
            
           if (_playerBlackboard.BallState.HasBall)
           {
              DebugLogger.Log($"[{_debugName}(ChaseBallActionRuntime)] ボールキャプチャ成功: {_playerBlackboard.BasicData.Self.name}");
           }
           else
           {
              DebugLogger.Log($"[{_debugName}(ChaseBallActionRuntime)] ボール追跡終了: {_playerBlackboard.BasicData.Self.name}");
           }
           // 立ち止まる
           _animalComponent.Animal.stand();
        }
        
        return isComplete;
    }
    
    public override void Cancel()
    {
        //if (_playerBlackboard != null)
        //{
        //    _playerBlackboard.EndAction();
        //    //_playerBlackboard.isMoving = false;
        //}
        
        _isChasing = false;
         _animalComponent.Animal.stand();
        DebugLogger.Log($"[{_debugName}(ChaseBallActionRuntime)] ボール追跡キャンセル");
    }
} 