using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Photonの生成をプレハブに変更する
[DefaultExecutionOrder(-500)]
public class PhotonCreateToPrefabPool : MonoBehaviour, IPunPrefabPool
{
    // Photon用のプレハブ
    [SerializeField] private List<StructPhotonPrefabInfo> _photonPrefabs;

    [System.Serializable]
    public struct StructPhotonPrefabInfo
    {
        // プレハブ
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab
        {
            get { return _prefab; }
        }
        // プレハブ名
        [SerializeField] private string _prefabName;
        public string PrefabName
        {
            get { return _prefabName; }
        }
    }

  /// <summary>GameScene の Prefabs/Animals 登録プールが有効か（DefaultPool の Resources 参照を避ける）。</summary>
    public static bool IsActivePool =>
        PhotonNetwork.PrefabPool is PhotonCreateToPrefabPool;

    private void Awake()
    {
        PhotonNetwork.PrefabPool = this;
    }

    /// <summary>
    /// GameObject生成
    /// </summary>
    /// <param name="prefabId">プレハブの名前</param>
    /// <param name="position">位置</param>
    /// <param name="rotation">回転</param>
    /// <returns>生成したオブジェクト</returns>
    GameObject IPunPrefabPool.Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        // プレハブリスト
        foreach(StructPhotonPrefabInfo info in _photonPrefabs)
        {
            // 同じ文字ならば
            if(prefabId.Equals(info.PrefabName))
            {
                // プレハブを元に生成/PhotonNetworkの方でONにするのでOFFで
                var obj = Instantiate(info.Prefab, position, rotation);
                obj.gameObject.SetActive(false);

                return obj;
            }
        }

        return null;
    }

    // 破棄
    void IPunPrefabPool.Destroy(GameObject gameObject)
    {
        Destroy(gameObject);
    }
}
