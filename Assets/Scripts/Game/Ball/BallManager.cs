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

    private const float DefaultKickoffPickupSuppressSeconds = 1.5f;
    private float _kickoffPickupSuppressUntil;

    public bool IsKickoffBallPickupSuppressed =>
        BallKickoffResetRules.ShouldRejectOwnershipClaim(1, _kickoffPickupSuppressUntil, Time.time);

    private void Awake()
    {
        if (GetComponent<SetPieceRuntimeController>() == null)
        {
            gameObject.AddComponent<SetPieceRuntimeController>();
        }
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

    public void BeginKickoffPickupSuppress(float seconds = DefaultKickoffPickupSuppressSeconds)
    {
        _kickoffPickupSuppressUntil = Time.time + Mathf.Max(0f, seconds);
    }

    private bool ShouldRejectKickoffOwnershipClaim(int ownerID)
    {
        return BallKickoffResetRules.ShouldRejectOwnershipClaim(
            ownerID,
            _kickoffPickupSuppressUntil,
            Time.time);
    }

    // 所有権の変更
    public bool changeOwnership(int ownerID, BallManager_State.BALL_STATE bState)
    {
        if (ShouldRejectKickoffOwnershipClaim(ownerID))
        {
            Debug.Log($"[BallManager] changeOwnership ignored during kickoff reset: ownerID={ownerID}, state={bState}");
            return false;
        }

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
        if (ShouldRejectKickoffOwnershipClaim(ownerID))
        {
            Debug.Log($"[BallManager] setBallOwnerIDAndTeam ignored during kickoff reset: ownerID={ownerID}");
            return;
        }

        ApplyBallOwnerIDAndTeamInternal(ownerID);
    }

    /// <summary>
    /// 6-E P0: RPC 受信で所有権を適用する（kickoff suppress / changeOwnership guard を bypass）。
    /// </summary>
    public void ApplyOwnershipFromNetwork(int ownerID)
    {
        _photon.ApplyOwnerIdFromNetwork(ownerID);

        BallManager_State.BALL_STATE bState =
            BallOwnershipNetworkApplyRules.ResolveBallStateFromOwnerId(ownerID);
        _state.BallState = bState;
        _goap.updateBallState(bState);
        ApplyBallOwnerIDAndTeamInternal(ownerID);
    }

    private void ApplyBallOwnerIDAndTeamInternal(int ownerID)
    {
        var character = _photon.FindCharacterByOwnerId(ownerID);
        Vector3 ownerPosition = _state.getBallOwnerPosition(character);
        int resolvedViewId = character != null ? character.ViewID : -1;
        string diagLine = $"[GOAP_DIAG][BallOwnerSync] inputOwnerID={ownerID} resolvedViewID={resolvedViewId} belongTeam={_state.BelongTeam}";
        if (GoapRuntimeDiagnostics.VerboseLoggingEnabled)
        {
            Debug.Log(diagLine);
            GoapDiagnosticLog.Write(diagLine);
        }
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

    /// <summary>キックオフ先頭へボールを渡す（意図的な所有権変更は抑制ガードを通す）。</summary>
    public bool AssignKickoffPossession(int ownerViewId)
    {
        if (ownerViewId <= 0)
        {
            return false;
        }

        _kickoffPickupSuppressUntil = 0f;
        _photon.ClearBallOwnerForKickoff();

        if (_ball != null)
        {
            _ball.SetBallBuff(BallBuffKind.None);
            _ball.stop();
        }

        if (!changeOwnership(ownerViewId, BallManager_State.BALL_STATE.HOLD))
        {
            return false;
        }

        BeginKickoffPickupSuppress();
        return true;
    }

    /// <summary>6-B P1: ゴールキック — ボールを配置し守備 GK に HOLD を渡す。</summary>
    public bool AssignGoalKickPossession(
        int ownerViewId,
        Vector3 ballWorldPosition,
        float suppressSeconds = GoalKickSetPieceRules.DefaultSuppressSeconds)
    {
        if (ownerViewId <= 0)
        {
            return false;
        }

        _kickoffPickupSuppressUntil = 0f;
        _photon.ClearBallOwnerForKickoff();

        if (_ball != null)
        {
            _ball.SetBallBuff(BallBuffKind.None);
            _ball.stop();
            changeBallParent();
            Vector3 pos = ballWorldPosition;
            if (pos.y < 0.2f)
            {
                pos.y = 0.5f;
            }

            _ball.transform.position = pos;
            Rigidbody rb = _ball.Rigid;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (!changeOwnership(ownerViewId, BallManager_State.BALL_STATE.HOLD))
        {
            return false;
        }

        BeginKickoffPickupSuppress(suppressSeconds);
        return true;
    }

    // ゴール後キックオフ: 失点側の先頭へボールを渡す
    public bool ResetBallPositionForKickoff(int storedOwnerIndex)
    {
        if (BallKickoffAssignment.TryAssignFromStoredIndex(this, storedOwnerIndex, out _))
        {
            return true;
        }

        ResetBallPositionToCenterFree();
        return false;
    }

    // ボールをセンター FREE に戻す（フォールバック）
    public void ResetBallPositionToCenterFree()
    {
        BeginKickoffPickupSuppress();
        _state.ResetToKickoffFree();
        _photon.ClearBallOwnerForKickoff();

        if (_ball != null)
        {
            _ball.SetBallBuff(BallBuffKind.None);
            _ball.stop();
            changeBallParent();
            _ball.transform.position = new Vector3(0f, 0.5f, 0f);
            Rigidbody rb = _ball.Rigid;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (!changeOwnership(-1, BallManager_State.BALL_STATE.FREE))
        {
            _goap.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
            _goap.updateBallState(BallManager_State.BALL_STATE.FREE);
            _ball?.ApplyFreeBallStateLocal();
        }
    }
}
