using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// テスト用アクション2
[CreateAssetMenu(menuName = "GOAP/Action/Test/Test2")]
public class Test2ActionSO : GoapActionSO
{
    [Header("テスト2設定")]
    [SerializeField] private float _testDuration = 2.5f;           // テスト2実行時間
    [SerializeField] private string _testMessage = "Test2実行中"; // テスト2メッセージ
    
    // プロパティ
    public float TestDuration => _testDuration;
    public string TestMessage => _testMessage;
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new Test2ActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        SetupPreconditionsAndEffects();
    }
    
    private void SetupPreconditionsAndEffects()
    {
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Test.TEST1_MODE, true),
            new GoapCondition(SymbolTag.Test.TEST2_MODE, false),
        });
        
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Test.TEST2_MODE, true),
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, true),
        });
    }
}
