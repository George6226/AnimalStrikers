using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// テスト用アクション0(途中アクション[Runtime])
public class Test0ActionRuntime : GoapActionRuntime
{
    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _testDuration;
    private string _testMessage;
    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    
    public Test0ActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as Test0ActionSO;
        if (so != null)
        {
            _testDuration = so.TestDuration;
            _testMessage = so.TestMessage;
        }
    }
    
    public override bool CanExecute(PlayerBlackboard bb)
    {
        // 条件なし（テスト用）
        return true;
    }
    
    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        _isExecuting = true;
        _executionStartTime = Time.time;
        
        DebugLogger.Log($"Test0開始: {bb.BasicData.Self.name} - {_testMessage}");
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null) return;
        
        float elapsed = Time.time - _executionStartTime;
        float progress = Mathf.Clamp01(elapsed / _testDuration);
        
        if (elapsed % 0.5f < deltaTime)
        {
            DebugLogger.Log($"[{_debugName}] Test0進行中: {progress:P0} - {_playerBlackboard.BasicData.Self.name}");
        }
    }
    
    public override bool IsComplete()
    {
        if (!_isExecuting) return true;
        
        if (Time.time - _executionStartTime >= _testDuration)
        {
            if (_animalComponent != null)
            {
                _animalComponent.Animal.stand();
            }
            DebugLogger.Log($"[{_debugName}] Test0完了: {_playerBlackboard.BasicData.Self.name}");
            return true;
        }
        return false;
    }
    
    public override void Cancel()
    {
        _isExecuting = false;
        if (_animalComponent != null)
        {
            _animalComponent.Animal.stand();
        }
        DebugLogger.Log($"[{_debugName}] Test0キャンセル: {_playerBlackboard?.BasicData.Self.name ?? "Unknown"}");
    }
}
