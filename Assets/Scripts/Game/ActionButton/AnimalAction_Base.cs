using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルアクションの基底クラス
public abstract class AnimalAction_Base : MonoBehaviour
{
    /// <summary>
    /// このアクションが対応するボタンタイプ（bitフラグとしてintで表現）
    /// </summary>
    public abstract int ButtonType { get; }

    /// <summary>
    /// アクションを実行する（派生クラスで実装）
    /// </summary>
    public abstract void Execute();
}
