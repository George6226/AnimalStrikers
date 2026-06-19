using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ワーキングメモリ
// MonoBehaviour にする必要がない純粋なデータクラスとして扱う
public class WorkingMemory
{
    // UFR辞書
    private Dictionary<string, bool> _facts = new();
    // 履歴
    private List<string> _history = new();

    // UFRの上書き
    public void AssertFact(Fact fact, bool value)
    {
        // Factの文字化をキーに
        var key = fact.ToString();
        _facts[key] = value;
        // 時間とともに履歴に追加
        _history.Add($"{Time.time:F2}: {key} = {value}");
    }

    // Factキーがあるか?
    public bool? GetFact(Fact fact)
    {
        // Factキーが存在しているか?
        return _facts.TryGetValue(fact.ToString(), out var val) ? val : null;
    }

    // 履歴リストの取得
    public IEnumerable<string> GetHistory() => _history;
}
