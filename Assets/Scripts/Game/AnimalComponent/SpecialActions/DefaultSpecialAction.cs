using UnityEngine;

// デフォルトのスペシャルアクション（スペシャルアクションが未実装の動物用）
public class DefaultSpecialAction : AnimalSpecialActionBase
{
    public override bool CanExecuteSpecial()
    {
        return true;
    }

    // public override void ExecuteSpecial()
    // {
    //     Debug.Log("デフォルトのスペシャル発動（効果なし）");
    //     // デフォルトでは何も実行しない
    // }
}
