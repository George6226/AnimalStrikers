using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルの攻撃アクション
public class AnimalAction_Attack : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Attack;

    [SerializeField] private AnimalHandler _animalHandler;

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装（プレイヤー操作前提）
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if(!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;
        _animalHandler.attack();
    }
}
