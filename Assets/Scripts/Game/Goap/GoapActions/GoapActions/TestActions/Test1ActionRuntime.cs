using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// テスト用アクション(途中アクション[Runtime])
public class Test1ActionRuntime : GoapActionRuntime
{
    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _testDuration;
    private string _testMessage;
    private PlayerBlackboard _playerBlackboard; // 保存されたPlayerBlackboard
    private AnimalComponentManager _animalComponent;
    
    // コンストラクタ
    public Test1ActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName) 
    {
        var testSO = origin as Test1ActionSO;
        if (testSO != null)
        {
            // ScriptableObjectから設定値を取得
            _testDuration = testSO.TestDuration;
            _testMessage = testSO.TestMessage;
        }
    }
    
    // 実行可能かどうか
    public override bool CanExecute(PlayerBlackboard bb)
    {
        // // 移動可能かチェック
        // if (!bb.ActionState.CanMove) return false;
        
        // // ボールを持っていないかチェック
        // if (bb.BallState.HasBall) return false;
        
        return true;
    }
    
    // 実行開始
    public override void Execute(PlayerBlackboard bb)
    {
        // PlayerBlackboardを保存
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        
        // テスト実行開始
        _isExecuting = true;
        _executionStartTime = Time.time;

        // ブラックボードの状態を更新
        // bb.ActionState.SetActionProgress(0f);
        
        // 効果を適用
        // _originSO.ApplyEffects(bb);
        
        DebugLogger.Log($"Test1開始: {bb.BasicData.Self.name} - {_testMessage}");
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isExecuting) return;
        
        if (_playerBlackboard == null) return;
        
        // // テスト実行の進行度を計算
        float elapsedTime = Time.time - _executionStartTime;
        float progress = Mathf.Clamp01(elapsedTime / _testDuration);
        
        // // アクション進行度を更新
        // _playerBlackboard.ActionState.SetActionProgress(progress);
        
        // // テスト用の移動処理（簡単な円形移動）
        // Vector3 currentPos = _playerBlackboard.PhysicalState.Position;
        // float angle = elapsedTime * _testSpeed;
        // Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 2f;
        // Vector3 targetPos = currentPos + offset;
        
        // // AnimalHandlerのmoveメソッドを使用
        // float per = 0.5f; // 中程度の速度で移動
        // float speedMag = _testSpeed; // 設定された速度倍率
        
        // _animalComponent.Animal.move(per, speedMag);

        // // キャラクターの向きを移動方向に調整
        // Vector3 direction = (targetPos - currentPos).normalized;
        // if (direction.magnitude > 0.1f)
        // {
        //     float radian = Mathf.Atan2(-direction.x, direction.z);
        //     _animalComponent.Animal.rotate(radian);
        // }
        
        // デバッグ出力
        if (elapsedTime % 1f < deltaTime) // 1秒ごとに出力
        {
            DebugLogger.Log($"[{_debugName}] テスト1進行中: {progress:P0} - {_playerBlackboard.BasicData.Self.name}");
        }
    }
    
    public override bool IsComplete()
    {
        if (!_isExecuting) return true;
        
        // 完了条件
        bool isComplete = false;

        // 実行時間が経過したかチェック
        float elapsedTime = Time.time - _executionStartTime;
        if (elapsedTime >= _testDuration)
        {
            isComplete = true;
        }
        
        if (isComplete)
        {
            // テスト実行終了
            // _playerBlackboard.ActionState.SetActionProgress(1f);
            
            DebugLogger.Log($"[{_debugName}] テスト1完了: {_playerBlackboard.BasicData.Self.name}");
            
            // 立ち止まる
            _animalComponent.Animal.stand();
        }
        
        return isComplete;
    }
    
    public override void Cancel()
    {
        _isExecuting = false;
        
        if (_animalComponent != null)
        {
            _animalComponent.Animal.stand();
        }
        
        Debug.Log($"[{_debugName}] テスト1キャンセル: {_playerBlackboard?.BasicData.Self.name ?? "Unknown"}");
    }
}
