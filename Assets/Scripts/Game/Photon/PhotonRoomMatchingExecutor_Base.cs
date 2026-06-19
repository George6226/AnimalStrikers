using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ルームマッチングの実行の基礎
public abstract class PhotonRoomMatchingExecutor_Base : MonoBehaviour
{
    // 部屋のマッチングの実行
    public abstract void executeRoomMatching();
}
