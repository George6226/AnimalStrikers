using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class BallManager_Photon : MonoBehaviour
{
    // 現在ボールを保持している ownerID
    public int BallOwnerID { get; private set; } = -1;

    // 無効にするID(一度のみ弾く)/無効にする時間
    private int _guardID = -1;
    private float _guardTimer = -1.0f;
    private const float TIME_GUARD_RESET = 0.1f;  // ガードを無効にする時間

    // 更新
    private void Update()
    {
        // ガード発動
        if(_guardTimer >= 0.0f){
            // 時間をマイナス
            _guardTimer -= Time.deltaTime;

            // 時間が経ったらガードを解除
            if (_guardTimer <= 0.0f)
            {
                resetGuardID();
            }
        }
    }

    // 所有権の変更
    public bool changeOwnership(int ownerID, BallManager_State.BALL_STATE bState, BallHandler ball)
    {
        // 所有権を変更可能か?
        if (!canChangeOwnership(ownerID)){
            return false;
        }

        // 現在の所有者IDを更新
        BallOwnerID = ownerID;

        // NPC対戦の場合
        if(PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC)
        {
            // キャラクタを TeamFacade 経由で検索
            PhotonAvatarContainerChild character = FindCharacterByOwnerId(ownerID);

            if (character != null && ball != null)
            {
                ball.photonView.TransferOwnership(character.Owner);
            }
        }
        // プレイヤー対戦の場合
        else if(PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NORMAL)
        {
            // ボールの所有権がない = 相手に所有権がある(対人戦以外にはありえない)
            if (ball != null && !ball.photonView.IsMine)
            {
                // 所有権の移動
                ball.photonView.RequestOwnership();
            }
        }
        if (ball != null)
        {
            ball.synchronizedBallState(ownerID);
        }

        return true;
    }

    public PhotonAvatarContainerChild FindCharacterByOwnerId(int ownerID)
    {
        PhotonAvatarContainerChild character = null;
        var teamReg = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamReg != null)
        {
            var facade = teamReg.AllAnimals.FirstOrDefault(a =>
            {
                if (a == null) return false;
                var avatar = a.GetAvatar();
                return avatar != null && avatar.ViewID == ownerID;
            });
            if (facade != null)
            {
                character = facade.GetAvatar();
            }
        }

        return character;
    }

    // 所有権を変更可能か?
    private bool canChangeOwnership(int ownerID)
    {
        // IDがある & 0.5秒以内に同じ前のキャラに変更を求められた
        if (_guardID > 0 & _guardID == ownerID){
            resetGuardID();
            // 無効にする
            return false;
        }

        // IDがある場合は保存
        if (ownerID > 0)
        {
            _guardID = ownerID;
            // タイマー停止
            _guardTimer = -1.0f;
        }
        // IDがない = ボールがフリーになった
        else{
            // タイマー開始
            _guardTimer = TIME_GUARD_RESET;
        }
        return true;
    }

    // ガードIDのリセット
    private void resetGuardID()
    {
        Debug.Log("ownerIDのリセット:" + _guardID+" を-1に");
        _guardID = -1;
        _guardTimer = -1.0f;
    }
}

