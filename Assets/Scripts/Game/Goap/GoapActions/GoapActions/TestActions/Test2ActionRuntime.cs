using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// テスト用アクション2(途中アクション[Runtime])
public class Test2ActionRuntime : GoapActionRuntime
{
    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _testDuration;
    private string _testMessage;
    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    
    public Test2ActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as Test2ActionSO;
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
        
        DebugLogger.Log($"Test2開始: {bb.BasicData.Self.name} - {_testMessage}");
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null) return;
        
        float elapsed = Time.time - _executionStartTime;
        float progress = Mathf.Clamp01(elapsed / _testDuration);
        
        // 任意の簡易動作（足踏み程度）
        if (elapsed % 0.5f < deltaTime)
        {
            DebugLogger.Log($"[{_debugName}] Test2進行中: {progress:P0} - {_playerBlackboard.BasicData.Self.name}");
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
            DebugLogger.Log($"[{_debugName}] Test2完了: {_playerBlackboard.BasicData.Self.name}");
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
        Debug.Log($"[{_debugName}] Test2キャンセル: {_playerBlackboard?.BasicData.Self.name ?? "Unknown"}");
    }
}
