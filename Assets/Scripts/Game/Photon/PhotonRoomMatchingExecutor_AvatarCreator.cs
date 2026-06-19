using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 部屋マッチングの実行(アバターの生成)
public class PhotonRoomMatchingExecutor_AvatarCreator : PhotonRoomMatchingExecutor_Base
{
    [SerializeField] private PhotonAvatarCreator _avatarCreator;

    // 部屋のマッチングの実行
    public override void executeRoomMatching()
    {
        // キャラ生成
        _avatarCreator.executeAvatarCreator();
    }
}
