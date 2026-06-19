using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// テスト用アクション
[CreateAssetMenu(menuName = "GOAP/Action/Test/Test1")]
public class Test1ActionSO : GoapActionSO
{
    [Header("テスト設定")]
    [SerializeField] private float _testDuration = 3f;           // テスト実行時間
    [SerializeField] private string _testMessage = "Test1実行中"; // テストメッセージ
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float TestDuration => _testDuration;
    public string TestMessage => _testMessage;
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new Test1ActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // プログラム上でPreconditionとEffectを設定
        SetupPreconditionsAndEffects();
    }
    
    /// <summary>
    /// 前提条件と効果を設定
    /// </summary>
    private void SetupPreconditionsAndEffects()
    {
        // 前提条件の設定
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Test.TEST0_MODE, true),
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, false),
        });
        
        // 効果の設定（次のアクションの前提条件としても機能）
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Test.TEST0_MODE, false),
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, true),
        });
    }
    
}
