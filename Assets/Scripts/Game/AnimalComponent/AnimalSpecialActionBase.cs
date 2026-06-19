using UnityEngine;
using System;

// 動物のスペシャルアクションの基底クラス
public abstract class AnimalSpecialActionBase : MonoBehaviour
{
    // スペシャルアクションの発動条件を満たしているかチェック
    // デフォルトは常に true（条件なし）
    public virtual bool CanExecuteSpecial()
    {
        return true;
    }

    // スペシャルアクションを実行（デフォルトは何もしない）
    public virtual void ExecuteSpecial() {}

    // スペシャルアクションを終了（デフォルトは何もしない）
    public virtual void EndSpecial() {}

    /// <summary>
    /// エフェクトにコールバック先を設定する（デフォルトは何もしない）
    /// </summary>
    /// <param name="effect">コールバックを設定するエフェクト</param>
    public virtual void SetEffectCallback(GameObject effect)
    {
        // デフォルト実装では何もしない
    }

    // エフェクトのコールバック（デフォルトは何もしない）
    public virtual void callBackEffect() {}

    // // AnimalHandlerへの参照
    // protected AnimalHandler _animalHandler;
    // // PhotonAvatarContainerChildへの参照
    // protected PhotonAvatarContainerChild _avatar;

    // // 初期化
    // protected virtual void Awake()
    // {
    //     _animalHandler = GetComponent<AnimalHandler>();
    //     if (_animalHandler == null)
    //     {
    //         _animalHandler = GetComponentInParent<AnimalHandler>();
    //     }

    //     _avatar = GetComponent<PhotonAvatarContainerChild>();
    //     if (_avatar == null)
    //     {
    //         _avatar = GetComponentInParent<PhotonAvatarContainerChild>();
    //     }
    // }

    // // スペシャルアクションの発動条件を満たしているかチェック
    // public virtual bool CanExecuteSpecial()
    // {
    //     // デフォルトでは常にtrue（条件なし）
    //     // 各実装クラスでオーバーライドして条件を追加
    //     return true;
    // }

    // // ボールを所持しているかチェック
    // protected bool HasBall()
    // {
    //     if (_avatar == null)
    //     {
    //         return false;
    //     }

    //     // TODO:TeamBlackboardの使用をやめる
    //     // TeamBlackboardからボールの所持者IDを取得して比較（TeamFacade 経由）
    //     var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
    //     if (teamBB != null && teamBB.BallInfo != null)
    //     {
    //         return _avatar.ViewID == teamBB.BallInfo.BallOwnerID;
    //     }

    //     return false;
    // }

    // // ボールを所持していないかチェック
    // protected bool DoesNotHaveBall()
    // {
    //     return !HasBall();
    // }

    // // HPが指定値以上かチェック
    // protected bool HasHPAbove(float minHPPercent)
    // {
    //     if (_animalHandler == null)
    //     {
    //         return false;
    //     }

    //     // AnimalHandlerからHPゲージを取得してチェック
    //     // 注意: PhotonHPGaugeへの直接アクセスが必要な場合は、AnimalHandlerにプロパティを追加する必要があります
    //     // ここでは基本的な実装のみ
    //     return true; // 実装は後で拡張可能
    // }

    // // スペシャルアクションを実行
    // public abstract void ExecuteSpecial();

    // // スペシャルアクションを終了
    // public virtual void EndSpecial()
    // {
    //     // デフォルトの終了処理（必要に応じてオーバーライド）
    // }
}
