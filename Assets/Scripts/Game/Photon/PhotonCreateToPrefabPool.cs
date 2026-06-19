using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Photonの生成をプレハブに変更する
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

    // Start is called before the first frame update
    private void Start()
    {
        // ネットワークオブジェクトの生成・破棄を行う処理をこのクラスに変更
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
