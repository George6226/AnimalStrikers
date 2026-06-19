using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 事実(UFR)
public class Fact
{
    // 主題/述語/オブジェクト
    public string _subject;
    public string _predicate;
    public string _object;

    // コンストラクタ
    public Fact(string subject, string predicate, string obj = null)
    {
        _subject = subject;
        _predicate = predicate;
        _object = obj;
    }

    // ToStringの上書き => 
    public override string ToString() =>
        _object == null ? $"{_predicate}({_subject})" : $"{_predicate}({_subject}, {_object})";
}
