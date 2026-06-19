using UnityEngine;
using System;
using System.Collections;

// イノシシのスペシャルアクション
public class BoarSpecialAction : AnimalSpecialActionBase
{
    [SerializeField] private AnimalHandler _animalHandler;
    [SerializeField] private PhotonAvatarContainerChild _avatar;

    // 攻撃エリア
    [SerializeField] private GameObject _attackArea;
    [Header("Charge")]
    [SerializeField] private float _windUpDuration = 1.5f;   // ため（静止）時間
    [SerializeField] private float _chargeSpeed = 12.0f;
    [SerializeField] private float _chargeDuration = 1.5f;   // 走る時間
    [SerializeField] private float _stopDistance = 1.2f;

    private Coroutine _chargeRoutine;
    private AnimalCollider_Attack _attackCollider;

    private AnimalFacade _nearestEnemy;
    public AnimalFacade NearestEnemy => _nearestEnemy;

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

        // Player は従来どおり Enemy 側がボール所持で発動可能
        return state.BelongTeam == BallManager_State.BELONG_TEAM.ENEMY;
    }

    public override void ExecuteSpecial()
    {
        Debug.Log("イノシシのスペシャル発動: 突進攻撃");

        _nearestEnemy = findNearestEnemy();
        if (_nearestEnemy != null)
        {
            Debug.Log("[BoarSpecialAction] 一番近い敵: " + _nearestEnemy.name);
        }
        else
        {
            Debug.Log("[BoarSpecialAction] 一番近い敵が見つかりません");
            return;
        }

        startChargeTo(_nearestEnemy.transform.position, null);
    }

    private void startChargeTo(Vector3 targetWorldPos, Action onFinished)
    {
        // 既に突進中なら止めてから開始
        if (_chargeRoutine != null)
        {
            StopCoroutine(_chargeRoutine);
            _chargeRoutine = null;
            SetAttackAreaActive(false);
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

        // 敵方向へ向ける（角度合わせ）
        faceDirection(dir);

        // 攻撃エリアをONにする
        SetAttackAreaActive(true);

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
        else{
            Debug.LogError("[BoarSpecialAction] AnimalHandler が見つかりません");
        }
    }

    private IEnumerator chargeRoutine(Vector3 targetWorldPos, Action onFinished)
    {
        // ため：移動せず静止
        yield return new WaitForSeconds(_windUpDuration);

        // ため後に敵方向へ再度向きを合わせる
        if (_nearestEnemy != null)
        {
            Vector3 dir = _nearestEnemy.transform.position - transform.position;
            dir.y = 0.0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                faceDirection(dir);
            }
        }

        float endTime = Time.time + _chargeDuration;

        while (Time.time < endTime)
        {
            Vector3 myPos = transform.position;

            // 可能なら対象が動いても追従（ただし方向は開始時のまま）
            if (_nearestEnemy != null)
            {
                targetWorldPos = _nearestEnemy.transform.position;
            }

            // 前方へ突進（AnimalHandler のスペシャル移動を使用）
            if (_animalHandler != null)
            {
                _animalHandler.moveSpecial(1.0f, _chargeSpeed);
            }
            else
            {
                transform.position += transform.forward * _chargeSpeed * Time.deltaTime;
            }

            yield return null;
        }

        _chargeRoutine = null;
        SetAttackAreaActive(false);
        onFinished?.Invoke();
    }

    private void SetAttackAreaActive(bool isActive)
    {
        if (_attackArea == null)
        {
            return;
        }

        _attackArea.SetActive(isActive);

        if (_attackCollider == null)
        {
            _attackCollider = _attackArea.GetComponent<AnimalCollider_Attack>();
            if (_attackCollider == null)
            {
                _attackCollider = _attackArea.GetComponentInChildren<AnimalCollider_Attack>(true);
            }
        }

        if (_attackCollider != null)
        {
            _attackCollider.SpecialNow = isActive;
        }
    }

    private AnimalFacade findNearestEnemy()
    {
        var teamFacade = TeamFacade.Instance;
        var teamRegist = teamFacade != null ? teamFacade.TeamRegist : null;
        if (teamRegist == null)
        {
            return null;
        }

        bool isNpc = _avatar != null && (_avatar.tag == ConstData.NPC_TAG || _avatar.CurrentTag == ConstData.NPC_TAG);
        var candidates = isNpc ? teamRegist.Allys : teamRegist.Enemies;
        if (candidates == null)
        {
            return null;
        }

        Vector3 myPos = transform.position;
        AnimalFacade nearest = null;
        float best = float.MaxValue;

        foreach (var enemy in candidates)
        {
            if (enemy == null)
            {
                continue;
            }

            // GK は対象外
            var info = enemy.GetAnimalInfo();
            if (info != null && info.IsGK)
            {
                continue;
            }

            float d = Vector3.Distance(myPos, enemy.transform.position);
            if (d < best)
            {
                best = d;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
