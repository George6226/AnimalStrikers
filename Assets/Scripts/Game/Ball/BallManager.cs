using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;

public class BallManager : MonoBehaviour
{
    // カメラ
    [SerializeField] private CameraTargetGroupHandler _cameraTarget;
    [SerializeField] private GameObject _parent; // ボールの親オブジェクト
    // ボールの状態と所有者情報を扱うハンドラー
    [SerializeField] private BallManager_State _state;
    // ボールの状態ハンドラーへの公開アクセス
    public BallManager_State State
    {
        get { return _state; }
    }

    // ボールの Photon 所有権まわりを扱うコンポーネント
    [SerializeField] private BallManager_Photon _photon;
    // GOAP / TeamBlackboard とボールの橋渡しを行うコンポーネント
    [SerializeField] private BallManager_Goap _goap;

    // ボール
    private BallHandler _ball;
    public BallHandler Ball
    {
        get { return _ball; }
    }

    // ボールを登録する
    public void RegisterBall(BallHandler ball)
    {
        _ball = ball;
        _ball.transform.SetParent(_parent.transform);
        _cameraTarget.AddTarget(_ball.transform, 1.0f, 1.0f);
        _goap.setExistBall();
    }

    // ボールを保持しているかどうかを判定
    public bool isHoldBall(int ownerID)
    {
        return ownerID == _photon.BallOwnerID;
    }

    // 更新
    private void Update()
    {        
        if(_state.updateBallFree()){
            _goap.updateBallState(_state.BallState);
        }
    }

    // 所有権の変更
    public bool changeOwnership(int ownerID, BallManager_State.BALL_STATE bState)
    {
        Debug.Log($"[BallManager] changeOwnership called. ownerID: {ownerID}, bState: {bState}, currentOwnerID: {_photon.BallOwnerID}");

        // 所有権を変更できた場合
        if(_photon.changeOwnership(ownerID, bState, _ball)){
            Debug.Log($"[BallManager] Ownership successfully changed to ownerID: {ownerID}, new state: {bState}");
            // ボールの状態を更新
            _state.BallState = bState;
            // ボールの状態をTBに書き込み
            _goap.updateBallState(bState);
            // RPC 完了前に TeamBB をローカル更新し、保持者の idMatch を即成立させる
            if (ownerID > 0)
            {
                setBallOwnerIDAndTeam(ownerID);
            }
            else
            {
                _goap.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
            }
            return true;
        }
        Debug.LogWarning($"[BallManager] Ownership change failed for ownerID: {ownerID}, state: {bState}");
        return false;
    }

    // ボール所持者のIDとチームを設定
    public void setBallOwnerIDAndTeam(int ownerID)
    {
        var character = _photon.FindCharacterByOwnerId(ownerID);
        Vector3 ownerPosition = _state.getBallOwnerPosition(character);
        int resolvedViewId = character != null ? character.ViewID : -1;
        string diagLine = $"[GOAP_DIAG][BallOwnerSync] inputOwnerID={ownerID} resolvedViewID={resolvedViewId} belongTeam={_state.BelongTeam}";
        Debug.Log(diagLine);
        GoapDiagnosticLog.Write(diagLine);
        _goap.updateBallID(ownerID, _state.BelongTeam, ownerPosition);
    }

    /// <summary>現在のボール保持者のワールド座標を解決する。</summary>
    public bool TryResolveBallOwnerWorldPosition(int ownerId, out Vector3 position)
    {
        position = Vector3.zero;
        if (ownerId < 0)
        {
            return false;
        }

        var character = _photon.FindCharacterByOwnerId(ownerId);
        position = _state.getBallOwnerPosition(character);
        return position.sqrMagnitude > 0.0001f;
    }

    // ボールの親を変更する
    public void changeBallParent()
    {
        if (_ball != null)
        {
            _ball.transform.SetParent(_parent.transform);
        }
    }

    // ボールの位置をリセット
    public void ResetBallPosition()
    {
        if (_ball != null)
        {
            _ball.transform.position = new Vector3(0f, 0.5f, 0f);
            _ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            _ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            _ball.transform.SetParent(_parent.transform);

            // ボールの状態をリセット
            _state.BallState = BallManager_State.BALL_STATE.FREE;
            // ボールのIDと状態を更新
            _goap.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
            _goap.updateBallState(_state.BallState);
            _ball.SetBallBuff(BallBuffKind.None);
        }
    }
}
