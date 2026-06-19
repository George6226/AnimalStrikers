using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Photonネットワークオブジェクトの取得
public class PhotonNetworkObjs : MonoBehaviourPunCallbacks
{
    private float _spendTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _spendTime += Time.deltaTime;

        if(_spendTime >= 10.0f)
        {
            foreach (var pv in PhotonNetwork.PhotonViewCollection)
            {
                DebugLogViewer.Instance.addDebugLog("オブジェ名:" + pv.gameObject.name + " viewId:" + pv.ViewID);
            }

            _spendTime = 0.0f;
        }
    }
}
