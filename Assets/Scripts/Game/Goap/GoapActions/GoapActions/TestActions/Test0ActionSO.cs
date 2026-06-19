using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// テスト用アクション0
[CreateAssetMenu(menuName = "GOAP/Action/Test/Test0")]
public class Test0ActionSO : GoapActionSO
{
    [Header("テスト0設定")]
    [SerializeField] private float _testDuration = 2f;           // テスト0実行時間
    [SerializeField] private string _testMessage = "Test0実行中"; // テスト0メッセージ
    
    public float TestDuration => _testDuration;
    public string TestMessage => _testMessage;
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new Test0ActionRuntime(this, debugName);
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
            new GoapCondition(SymbolTag.Test.TEST0_MODE, false),
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, true),
        });
        
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Test.TEST0_MODE, true),
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, false),
        });
    }
}
