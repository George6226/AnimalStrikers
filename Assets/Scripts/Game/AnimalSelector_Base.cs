using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

// キャラクタの選択（共通基底クラス）
public abstract class AnimalSelector_Base : MonoBehaviour
{
    // 選択されているキャラクタのFacade
    protected AnimalFacade _selAnimalFacade = null;
    public AnimalFacade SelAnimalFacade
    {
        get { return _selAnimalFacade; }
    }   

    // 経過時間
    private float _spendTime = 0.0f;
    private float _lastAutoSwitchTime = -999f;
    private float _defenceModeEnterTime = -999f;
    private float _autoSwitchBlockedUntil;
    private bool _isAutoSwitchInProgress;
    private BallManager_State.BELONG_TEAM _trackedBelongTeam = BallManager_State.BELONG_TEAM.FREE;

    [Header("ディフェンス時の自動キャラ切り替え")]
    [Tooltip("敵ボール／フリーボール時、最短間隔で近いキャラへ切り替えを試みる秒数")]
    [SerializeField] private float _defenceSwitchInterval = 3.0f;
    [Tooltip("現在操作キャラより、この距離（m）以上近い味方がいるときだけ切り替える")]
    [SerializeField] private float _switchDistanceMargin = 2.5f;
    [Tooltip("自動切り替え後、次の自動切り替えまで待つ秒数")]
    [SerializeField] private float _minHoldAfterAutoSwitch = 2.5f;
    [Tooltip("敵ボール／フリーに変わってから、最初の自動切り替えまで待つ秒数")]
    [SerializeField] private float _graceAfterDefenceModeEnter = 1.5f;
    [Tooltip("パス受け・ボール取得など手動切り替え後、自動切り替えを止める秒数")]
    [SerializeField] private float _manualSwitchLockDuration = 3.0f;

    private IEnumerator Start()
    {
        // ボールが生成されるまで待機（TeamFacade 経由で BallManager を取得）
        yield return new WaitUntil(() =>
        {
            var teamFacade = TeamFacade.Instance;
            if (teamFacade == null) return false;
            var ballManager = teamFacade.BallManager;
            return ballManager != null && ballManager.Ball != null;
        });

        // 近くのアニマル選択
        AnimalFacade selAnimal = findNearestAnimalToBall();
        setSelectAnimal(selAnimal);
    }

    /// <summary>敵ボール／フリーボールに入ったタイミングを記録（派生の Update から呼ぶ）。</summary>
    protected void TrackDefenceBallContext(BallManager_State state)
    {
        if (state == null)
        {
            return;
        }

        var belong = state.BelongTeam;
        bool isDefenceContext = belong == BallManager_State.BELONG_TEAM.ENEMY
            || belong == BallManager_State.BELONG_TEAM.FREE;

        if (isDefenceContext && belong != _trackedBelongTeam)
        {
            _defenceModeEnterTime = Time.time;
        }

        _trackedBelongTeam = belong;
    }

    // ディフェンス状態でのキャラの選択（間隔・距離差・ホールドで急な切り替えを抑制）
    protected void selectDefenceAnimal()
    {
        if (Time.time < _autoSwitchBlockedUntil)
        {
            return;
        }

        if (Time.time - _defenceModeEnterTime < _graceAfterDefenceModeEnter)
        {
            return;
        }

        if (Time.time - _lastAutoSwitchTime < _minHoldAfterAutoSwitch)
        {
            return;
        }

        _spendTime += Time.deltaTime;
        if (_spendTime < _defenceSwitchInterval)
        {
            return;
        }

        _spendTime = 0.0f;
        if (!TryFindSwitchCandidate(out AnimalFacade selAnimal))
        {
            return;
        }

        _isAutoSwitchInProgress = true;
        setSelectAnimal(selAnimal);
        _isAutoSwitchInProgress = false;
        _lastAutoSwitchTime = Time.time;
    }

    /// <summary>
    /// ボールに近い候補を探す。現在操作キャラより十分近い場合のみ返す。
    /// </summary>
    protected bool TryFindSwitchCandidate(out AnimalFacade candidate)
    {
        candidate = null;
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        if (ballManager == null || ballManager.Ball == null)
        {
            return false;
        }

        Vector3 ballPos = ballManager.Ball.transform.position;
        AnimalFacade nearest = null;
        float nearestDist = float.MaxValue;

        foreach (AnimalFacade facade in getTeamList())
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            float d = HorizontalDistance(facade.transform.position, ballPos);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = facade;
            }
        }

        if (nearest == null)
        {
            return false;
        }

        if (_selAnimalFacade == null)
        {
            candidate = nearest;
            return true;
        }

        if (nearest == _selAnimalFacade)
        {
            return false;
        }

        float currentDist = HorizontalDistance(_selAnimalFacade.transform.position, ballPos);
        if (nearestDist + _switchDistanceMargin < currentDist)
        {
            candidate = nearest;
            return true;
        }

        return false;
    }

    // 一番ボールに近い味方キャラクタを検索
    protected AnimalFacade findNearestAnimalToBall()
    {
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;

        // BallManager / Ball が無い
        if (ballManager == null || ballManager.Ball == null)
        {
            Debug.LogError("AnimalSelector: BallManager or Ball is null");
            return null;
        }

        AnimalFacade selFacade = null;
        float distance = 999999.0f;
        IEnumerable<AnimalFacade> list = getTeamList();
        foreach(AnimalFacade facade in list)
        {
            if (facade == null)
            {
                continue;
            }

            // GKは除外
            if (facade.IsGK()){
                continue;
            }

            // 距離
            float d = Mathf.Abs(Vector3.Distance(facade.transform.position, ballManager.Ball.transform.position));
            // より短い
            if (distance > d)
            {
                selFacade = facade;
                distance = d;
            }
        }

        return selFacade;
    }

    // チームリストの取得
    protected abstract IEnumerable<AnimalFacade> getTeamList();

    // 選択されたアニマルを設定する
    public void setSelectAnimal(AnimalFacade newFacade)
    {
        if (newFacade == null)
        {
            _spendTime = _defenceSwitchInterval;
            selectDefenceAnimal();
            return;
        }

        // 同じ場合は変更なし（Facade 基準）
        if (_selAnimalFacade != null && newFacade.gameObject.Equals(_selAnimalFacade.gameObject))
        {
            return;
        }

        if (!_isAutoSwitchInProgress)
        {
            _autoSwitchBlockedUntil = Time.time + _manualSwitchLockDuration;
        }

        // カメラとHPゲージの更新
        updateCameraAndHPGauge(newFacade);
        // 現在の選択キャラを更新
        _selAnimalFacade = newFacade;
        // スペシャルゲージの更新
        updateSpecialGauge(newFacade);
        OnSelectAnimalChanged(newFacade);
    }

    /// <summary>選択キャラが変わったとき（派生クラスでロール同期など）。</summary>
    protected virtual void OnSelectAnimalChanged(AnimalFacade newFacade) { }

    protected virtual void updateCameraAndHPGauge(AnimalFacade newFacade){}

    protected virtual void updateSpecialGauge(AnimalFacade newFacade){}

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
