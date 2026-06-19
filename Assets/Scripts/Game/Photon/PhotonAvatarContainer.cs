using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Photon用のアバターをリスト管理するクラス
public class PhotonAvatarContainer : MonoBehaviour, IEnumerable<PhotonAvatarContainerChild>
{
    // #region Singleton
    // // インスタンス
    // private static PhotonAvatarContainer _instance;
    // public static PhotonAvatarContainer Instance
    // {
    //     get
    //     {
    //         // インスタンス
    //         if (_instance == null)
    //         {
    //             _instance = (PhotonAvatarContainer)FindObjectOfType(typeof(PhotonAvatarContainer));

    //             if (_instance == null)
    //             {
    //                 Debug.LogError(typeof(PhotonAvatarContainer) + "is nothing");
    //             }
    //         }
    //         return _instance;
    //     }
    // }
    // #endregion Singleton

    // Photon用のアバターリスト
    private List<PhotonAvatarContainerChild> _avatarList = new List<PhotonAvatarContainerChild>();

    // リストからアバターを取得する
    public PhotonAvatarContainerChild this[int index] => _avatarList[index];

    // リスト数を取る
    public int Count => _avatarList.Count;

    // 味方を取得（PlayerAgentタグのみ）
    public List<PhotonAvatarContainerChild> Allys => _avatarList.Where(o => 
        o.tag.Equals(ConstData.PLAYER_TAG) && 
        !o.tag.Equals(ConstData.BALL_TAG)
    ).ToList();

    // 敵を取得（NPCタグとEnemyAgentタグ）
    public List<PhotonAvatarContainerChild> Enemies => _avatarList.Where(o => 
        (o.tag.Equals(ConstData.NPC_TAG) || o.tag.Equals(ConstData.ENEMY_TAG)) && 
        !o.tag.Equals(ConstData.BALL_TAG)
    ).ToList();

    // 全ての動物を取得（ボール以外）
    public List<PhotonAvatarContainerChild> AllAnimals => _avatarList.Where(o => 
        !o.tag.Equals(ConstData.BALL_TAG)
    ).ToList();

    // // ボールを所持しているキャラクタ
    // public PhotonAvatarContainerChild HasBallPlayer
    // {
    //     get
    //     {
    //         var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
    //         if (teamBB == null) return null;
    //         int ownerId = teamBB.BallInfo.BallOwnerID;
    //         return _avatarList.FirstOrDefault(o => o.ViewID == ownerId);
    //     }
    // }

    //// 子供の数が変更した時
    //private void OnTransformChildrenChanged()
    //{
    //    // リストをクリア
    //    _avatarList.Clear();
    //    // 全ての子供を入れ直し
    //    foreach(Transform child in transform)
    //    {
    //        _avatarList.Add(child.GetComponent<PhotonAvatarContainerChild>());
    //        // ボールを確保
    //        if (child.tag.Equals("Ball")){
    //            _ball = child.GetComponent<Ball>();
    //        }
    //    }
    //}

    // アバター追加
    // public void addAvatar(PhotonAvatarContainerChild avatar)
    // {
    //     _avatarList.Add(avatar);

    //     if (avatar.tag.Equals(ConstData.BALL_TAG))
    //     {
    //         // ボール情報をボール管理へ
    //         BallManager.Instance.BallInfo = avatar.GetComponent<BallHandler>();
    //     }
    // }

    // アバター削除
    public void removeAvatar(PhotonAvatarContainerChild avatar)
    {
        if (_avatarList.Contains(avatar))
        {
            _avatarList.Remove(avatar);
        }
    }

    // 反復処理を取得する
    public IEnumerator<PhotonAvatarContainerChild> GetEnumerator()
    {
        return _avatarList.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
