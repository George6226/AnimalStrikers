using UnityEngine;
using System;
using System.Collections;

// ワニのスペシャルアクション（ため → ボール方向へ突進して奪う）
public class CrocodileSpecialAction : AnimalSpecialActionBase
{
    [SerializeField] private AnimalHandler _animalHandler;

    private int _selfViewId = -1;

    [Header("Follow up")]
    [SerializeField] private AnimalAction_Shoot _shootAction;

    // 攻撃エリア
    [SerializeField] private GameObject _attackArea;
    [SerializeField] private PhotonAvatarContainerChild _avatar;
    [Header("Charge")]
    [SerializeField] private float _windUpDuration = 1.5f;   // ため（静止）時間
    [SerializeField] private float _moveDistance = 2.0f;     // 移動する一定距離（XZ）
    [SerializeField] private float _moveSpeedMag = 12.0f;    // moveSpecial の speedMag（per=1想定）
    [SerializeField] private float _chargeDuration = 1.5f;   // 移動の最大時間（安全策）

    private Coroutine _chargeRoutine;

    private void Awake()
    {
        if (_animalHandler == null)
        {
            _animalHandler = GetComponent<AnimalHandler>();
            if (_animalHandler == null)
            {
                _animalHandler = GetComponentInParent<AnimalHandler>();
            }
        }

        if (_avatar == null)
        {
            _avatar = GetComponent<PhotonAvatarContainerChild>();
            if (_avatar == null)
            {
                _avatar = GetComponentInParent<PhotonAvatarContainerChild>();
            }
        }
    }

    public override bool CanExecuteSpecial()
    {
        // 敵チームがボールを持っている時のみ発動可能
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        var state = ballManager != null ? ballManager.State : null;
        if (state == null)
        {
            return false;
        }

        bool isNpc = _avatar != null && (_avatar.tag == ConstData.NPC_TAG || _avatar.CurrentTag == ConstData.NPC_TAG);
        if (isNpc)
        {
            // NPC は Player 側がボールを所持しているときに発動可能
            return state.BelongTeam == BallManager_State.BELONG_TEAM.PLAYER;
        }

        // Player は Enemy 側がボール所持で発動可能
        return state.BelongTeam == BallManager_State.BELONG_TEAM.ENEMY;
    }

    public override void ExecuteSpecial()
    {
        Debug.Log("ワニのスペシャル発動: ボール奪取");

        var ballPos = getBallPosition();
        if (ballPos == null)
        {
            return;
        }

        _selfViewId = _avatar != null ? _avatar.ViewID : -1;

        startChargeTo(ballPos.Value, null);
    }

    private void startChargeTo(Vector3 targetWorldPos, Action onFinished)
    {
        // 既に突進中なら止めてから開始
        if (_chargeRoutine != null)
        {
            StopCoroutine(_chargeRoutine);
            _chargeRoutine = null;
        }

        // 自分→敵の方向（XZ）
        Vector3 myPos = transform.position;
        Vector3 dir = targetWorldPos - myPos;
        dir.y = 0.0f;
        if (dir.sqrMagnitude <= 0.0001f)
        {
            onFinished?.Invoke();
            return;
        }

        // ボール方向へ向ける（角度合わせ）
        faceDirection(dir);

        // 攻撃エリアをONにする
        if (_attackArea != null)
        {
            _attackArea.SetActive(true);
        }

        _chargeRoutine = StartCoroutine(chargeRoutine(targetWorldPos, onFinished));
    }

    private void faceDirection(Vector3 dir)
    {
        Vector3 flat = dir;
        flat.y = 0.0f;
        if (flat.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        // AnimalHandler.rotateCommon の変換（theta = 360 - radDeg）に合わせて rad を作る
        float desiredYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y; // 0..360
        float rad = (360.0f - desiredYaw) * Mathf.Deg2Rad;

        if (_animalHandler != null)
        {
            _animalHandler.specialRotate(rad);
        }
        else
        {
            Debug.LogError("[CrocodileSpecialAction] AnimalHandler が見つかりません");
        }
    }

    private IEnumerator chargeRoutine(Vector3 targetWorldPos, Action onFinished)
    {
        // ため：移動せず静止
        yield return new WaitForSeconds(_windUpDuration);

        // ため後にボール方向へ再度向きを合わせる
        var ballPos = getBallPosition();
        if (ballPos != null)
        {
            Vector3 dir = ballPos.Value - transform.position;
            dir.y = 0.0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                faceDirection(dir);
            }
        }

        // 移動開始位置（XZ）
        Vector3 startXZ = new Vector3(transform.position.x, 0.0f, transform.position.z);
        float endTime = Time.time + _chargeDuration;

        bool shouldShoot = false;

        while (Time.time < endTime)
        {
            // 接触判定のためにフレームを回しつつ、方向だけは維持/更新する
            Vector3 myPos = transform.position;

            // 可能ならボール位置は取り直す（追従の判断用）
            var currentBallPos = getBallPosition();
            if (currentBallPos != null)
            {
                Vector3 dir = currentBallPos.Value - myPos;
                dir.y = 0.0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    faceDirection(dir);
                }
            }

            // 一定距離だけ移動（ジャンプアニメなので短距離移動）
            Vector3 curXZ = new Vector3(transform.position.x, 0.0f, transform.position.z);
            float traveled = Vector3.Distance(curXZ, startXZ);
            if (traveled >= _moveDistance)
            {
                break;
            }

            // 向いている方向へ前進
            if (_animalHandler != null)
            {
                _animalHandler.moveSpecial(1.0f, _moveSpeedMag);
            }
            else
            {
                // fallback: AnimalHandler が存在しない場合（rbクランプなどは保証されない）
                transform.position += transform.forward * (_moveSpeedMag * 3.0f) * Time.deltaTime;
            }

            yield return null;
        }

        _chargeRoutine = null;
        if (_attackArea != null)
        {
            _attackArea.SetActive(false);
        }

        // スペシャルでボールを奪えたら、終了後にシュートへつなぐ
        if (isBallOwnerMe())
        {
            if (_shootAction != null)
            {
                Debug.Log("[CrocodileSpecialAction] シュートを実行");
                _shootAction.Execute();
            }
            else
            {
                Debug.LogWarning("[CrocodileSpecialAction] AnimalAction_Shoot が設定されていません");
            }
        }
        onFinished?.Invoke();
    }

    private Vector3? getBallPosition()
    {
        var teamFacade = TeamFacade.Instance;
        var ball = teamFacade != null && teamFacade.BallManager != null ? teamFacade.BallManager.Ball : null;
        return ball != null ? (Vector3?)ball.transform.position : null;
    }

    private bool isBallOwnerMe()
    {
        if (_selfViewId < 0)
        {
            Debug.LogWarning("[CrocodileSpecialAction] _selfViewId is invalid (<0): " + _selfViewId);
            return false;
        }

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        if (teamBB == null || teamBB.BallInfo == null)
        {
            Debug.LogWarning("[CrocodileSpecialAction] teamBB or BallInfo is null");
            return false;
        }

        bool isOwner = teamBB.BallInfo.BallOwnerID == _selfViewId;
        Debug.Log("[CrocodileSpecialAction] BallOwnerID: " + teamBB.BallInfo.BallOwnerID + ", _selfViewId: " + _selfViewId + ", isOwner: " + isOwner);

        return isOwner;
    }

}
