using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 状態の管理
public class StateManager : MonoBehaviourPunCallbacks {

	// 状態
	public enum STATE_KIND
	{
		NONE = 0,
		READY,
		GAME,
		RESULT,
		PAUSE,
	};

	// 状態リスト
	[SerializeField]
	private List<State> _stateList;
	// 現在の状態
	private STATE_KIND _stateKind = STATE_KIND.READY;
	public STATE_KIND StateKind{
		get{ return _stateKind;}
	}

	#region Singleton
	// インスタンス
	private static StateManager _instance;
	public static StateManager Instance
	{
		get
		{
			// インスタンス
			if (_instance == null)
			{
				_instance = (StateManager)FindObjectOfType(typeof(StateManager));

				if (_instance == null)
				{
					Debug.LogError(typeof(StateManager) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	private PhotonView photonView;

	// 初期化
	void Awake()
	{
		photonView = GetComponent<PhotonView>();
		if (photonView == null)
		{
			Debug.LogError("PhotonView component not found!");
		}
	}

    // 状態の変更
    public void changeState(STATE_KIND state)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // マスターのみが状態を変更し、他のプレイヤーに同期
            photonView.RPC("RPCChangeState", RpcTarget.All, (int)state);
        }
    }

    // ローカルのみ状態を変更（相手切断など RPC 不要な場合）
    public void changeStateLocal(STATE_KIND state)
    {
        applyState(state);
    }

    private void applyState(STATE_KIND state)
    {
        for (int i = 0; i < _stateList.Count; i++)
        {
            State s = _stateList[i];
            if (state == s.StateKind)
            {
                _stateKind = state;
                s.changeObject();
            }
        }
    }

    [PunRPC]
    private void RPCChangeState(int stateValue)
    {
        STATE_KIND state = (STATE_KIND)stateValue;
        Debug.Log($"状態変更: {state}");
        applyState(state);
    }

	// 同じ種類か?
	public bool isSameKind(STATE_KIND state)
	{
		if (_stateKind == state) {
            //Debug.Log("現在のState:" + _stateKind);
			return true;
		}

		return false;
	}
}
