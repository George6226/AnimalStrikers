using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 部屋のマッチングの実行(シーン変更)
public class PhotonRoomMatchingExecutor_SceneFade : PhotonRoomMatchingExecutor_Base
{
    // シーンフェーダー
    [SerializeField] private SceneFader _sceneFader;

    private const string GAME_SCENE = "GameScene";      // 通常対戦シーン

    // 部屋のマッチングの実行
    public override void executeRoomMatching()
    {
        if (_sceneFader != null)
        {
            // シーンの変更
            _sceneFader.changeScene(GAME_SCENE);
        }
    }
}

