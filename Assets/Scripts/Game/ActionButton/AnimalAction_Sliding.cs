using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// スライディング
public class AnimalAction_Sliding : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Sliding;
    // アニマル選択
    [SerializeField] private AnimalHandler _animalHandler;

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装（プレイヤー操作前提）
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if(!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return; 
        _animalHandler.sliding();
    }
}
