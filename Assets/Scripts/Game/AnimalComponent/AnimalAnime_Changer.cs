using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// アニマルのアニメーション変更
public class AnimalAnime_Changer : MonoBehaviourPunCallbacks
{
    // アニメーター
    [SerializeField] private Animator _animator;
    // アニメーションの状態
    [SerializeField] private AnimalAnime_State _animState;

    // 固定情報
    [SerializeField] private AnimalInfo _animalInfo;

    // アニメーションの変更
    public void changeAnimation(int kind)
    {
        // 現在と同じ状態ならば
        if(kind == _animState.AnimeState){
            return;
        }
        // アニメーションをプレイ中 && 割り込み不可能
        if (_animState.AnimePlayNow && !canInsert(_animState.AnimeState, _animalInfo.IsGK)){
            return;
        }

        // TODO:割り込めた方が良さそう
        Debug.Log("アニメーション変更開始:" + kind);

        // 現在のアニメーション状態を変更する/アニメーション中にする
        _animState.AnimeState = kind;
        _animState.AnimePlayNow = true;
        // アニメーション
        StartCoroutine(changeAnime(kind));
    }

    // アニメーションの変更
    private IEnumerator changeAnime(int kind)
    {
        // Photonの場合
        if(photonView != null)
        {
            // アニメーション部分の同期
            photonView.RPC(nameof(executeAnime), RpcTarget.All, kind);
        }
        else
        {
            executeAnime(kind);
        }

        // アニメーションの終了待機
        yield return new WaitForSeconds(1.0f);
        yield return new WaitForAnimation(_animator, 0);

        // アニメーション終了
        _animState.AnimePlayNow = false;
        _animState.AnimeState = (int)AnimalAnime_State.PLAYER_ANIME_KIND.STAND;
    }

    // アニメーションの実行
    [PunRPC]
    private void executeAnime(int kind)
    {
        // 移動状態変更
        bool move = _animState.isMoveState(kind, !_animalInfo.IsGK);
        _animator.SetBool("IsMove", move);
        // アニメーションを始める
        string animeName = _animState.getAnimeName(kind, !_animalInfo.IsGK);
        Debug.Log("アニメーション変更直前:" + kind+" アニメーション名:"+animeName);
        _animator.Play(animeName);
    }

    // 割り込み可能か?
    private bool canInsert(int kind, bool goalKeeper)
    {
        // ゴールキーパーでない = GK以外
        if(!goalKeeper)
        {
            // 立ち状態 OR 移動状態
            if(kind == (int)AnimalAnime_State.PLAYER_ANIME_KIND.STAND || kind == (int)AnimalAnime_State.PLAYER_ANIME_KIND.MOVE)
            {
                return true;
            }
            // スペシャルの場合
            else if (kind == (int)AnimalAnime_State.PLAYER_ANIME_KIND.SPECIAL)
            {
                return true;
            }
        }
        // GK
        else
        {
            // 立ち状態 OR 移動状態
            if(kind == (int)AnimalAnime_State.KEEPER_ANIME_KIND.STAND || kind == (int)AnimalAnime_State.KEEPER_ANIME_KIND.MOVE_L || kind == (int)AnimalAnime_State.KEEPER_ANIME_KIND.MOVE_R)
            {
                return true;
            }
        }

        return false;
    }
}
