using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Photon用のアバター管理へ格納する
public class PhotonAvatarContainerChild : MonoBehaviourPunCallbacks
{
    // 現在のタグを保持
    private string _currentTag = "";
    /// <summary>論理上の陣営タグ（<see cref="SetTag"/> で設定。空のときは <see cref="updateTagName"/> がデフォルトで敵タグを付与）</summary>
    public string CurrentTag => _currentTag;

    // オーナー
    public Player Owner => photonView.Owner;
    // 所有権
    public bool IsMine => photonView.IsMine;
    // ViewID
    public int ViewID => photonView.ViewID;

    // 初期位置を保持
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // 初期位置を取得するプロパティ
    public Vector3 InitialPosition => _initialPosition;
    public Quaternion InitialRotation => _initialRotation;
    // ユニフォームの種類
    //private int _uniformType = 0;

    // タグを設定するメソッド
    public void SetTag(string newTag)
    {
        _currentTag = newTag;
    }

    public void Start()
    {
        base.OnEnable();

        // 初期位置と回転を保存
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // タグ名の更新
        updateTagName();
    }

    // タグ名の更新
    private void updateTagName()
    {
        // ボールの場合更新なし
        if(this.tag == ConstData.BALL_TAG){
            return;
        }

        // タグ名の更新
        string tagName = ConstData.ENEMY_TAG;
        if (_currentTag != "")
        {
            tagName = _currentTag;
        }
        this.tag = tagName;
    }

    // 初期位置に戻すメソッド
    public void ResetToInitialPosition()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
    }
}
