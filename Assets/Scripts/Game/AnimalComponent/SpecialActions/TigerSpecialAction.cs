using UnityEngine;

// トラのスペシャルアクション
public class TigerSpecialAction : AnimalSpecialActionBase
{
    public override bool CanExecuteSpecial()
    {
        return true;
    }

/*
    [SerializeField] private float _speedMultiplier = 1.5f; // 速度の倍率
    [SerializeField] private float _specialDuration = 5.0f; // スペシャル効果の持続時間

    private bool _isSpecialActive = false;

    public override void ExecuteSpecial()
    {
        if (_isSpecialActive)
        {
            return;
        }

        Debug.Log("トラのスペシャル発動: 速度アップ");
        _isSpecialActive = true;

        // ここにトラ固有のスペシャルアクションを実装
        // 例: 移動速度を一時的に上げる、ダッシュ効果を付与するなど

        // 一定時間後に効果を終了
        Invoke(nameof(EndSpecialEffect), _specialDuration);
    }

    private void EndSpecialEffect()
    {
        _isSpecialActive = false;
        Debug.Log("トラのスペシャル効果終了");
    }

    public override void EndSpecial()
    {
        _isSpecialActive = false;
        CancelInvoke(nameof(EndSpecialEffect));
    }

    // 速度の倍率を取得（他のクラスから呼び出し可能）
    public float GetSpeedMultiplier()
    {
        return _isSpecialActive ? _speedMultiplier : 1.0f;
    }
*/
}
