using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GoapActionRuntime
{
    protected GoapActionSO _originSO;
    protected string _debugName;

    public List<GoapCondition> Preconditions => _originSO.Preconditions;
    public List<GoapCondition> Effects => _originSO.Effects;

    public GoapActionRuntime(GoapActionSO origin)
    {
        _originSO = origin;
        _debugName = "(NoName)";
    }
    public GoapActionRuntime(GoapActionSO origin, string debugName)
    {
        _originSO = origin;
        _debugName = debugName;
    }

    /// <summary>頭上デバッグラベル・サマリログ用の表示名（SOの ActionName を優先）。</summary>
    public string DisplayName
    {
        get
        {
            if (_originSO != null && !string.IsNullOrEmpty(_originSO.ActionName))
            {
                return _originSO.ActionName;
            }

            if (!string.IsNullOrEmpty(_debugName) && _debugName != "(NoName)")
            {
                return _debugName;
            }

            return GetType().Name.Replace("ActionRuntime", string.Empty);
        }
    }

    // 実行可能かどうか（Blackboardの状態を参照）
    public abstract bool CanExecute(PlayerBlackboard bb);

    // 実行開始（アニメ再生・移動など）
    public abstract void Execute(PlayerBlackboard bb);

    // 実行中の更新（必要なら）
    public virtual void Update(float deltaTime) { }

    // 終了条件を満たしたか
    public virtual bool IsComplete() => true;

    // 強制中断された場合
    public virtual void Cancel() { }
}