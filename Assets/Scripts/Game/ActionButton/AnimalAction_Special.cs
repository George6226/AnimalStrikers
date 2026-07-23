using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// アニマルの必殺技アクション(個別)
public class AnimalAction_Special : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Special;

    [SerializeField] private AnimalAction_Gauge _specialGauge;
    [SerializeField] private AnimalHandler _animalHandler;
    // スペシャルアクション
    [SerializeField] private AnimalSpecialActionBase _specialAction;

    // スペシャル発動中（全体フラグ）。AnimEvent 欠落時に張り付くと AnimalHandler.move が全員で no-op になる。
    private static bool _isSpecialActive = false;
    public static bool IsSpecialActive => _isSpecialActive;

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;

        if (CanExecuteSpecial())
        {
            ActivateSpecial();
        }
    }

    public bool CanExecuteSpecial()
    {
        if (_isSpecialActive)
        {
            return false;
        }

        if (_specialGauge != null && _specialGauge.GaugeValue < 1.0f)
        {
            return false;
        }

        return _specialAction != null && _specialAction.CanExecuteSpecial();
    }

    public void ActivateSpecial()
    {
        // スペシャル発動中
        _isSpecialActive = true;
        // スペシャル発動(アニメーション)
        _animalHandler.special();
        // ゲージをリセット
        if (_specialGauge != null)
        {
            _specialGauge.ResetGauge();
        }

        // スペシャルアクションを実行（早期 return しても AnimEvent 待ち。GOAP 側タイムアウトで ForceFinish する）
        if (_specialAction != null)
        {
            _specialAction.ExecuteSpecial();
        }

        TryBroadcastSpecialActivated();
    }

    public void onSpecialFinished()
    {
        ApplySpecialFinishedLocal();
        TryBroadcastSpecialFinished();
    }

    /// <summary>
    /// AnimEvent 欠落・GOAP タイムアウト時に全体フラグを下ろす。
    /// 下りないと AnimalHandler.move / rotate が全キャラでスキップされ見ため全員停止になる。
    /// </summary>
    public void ForceFinishSpecial()
    {
        if (!_isSpecialActive)
        {
            return;
        }

        ApplySpecialFinishedLocal();
        TryBroadcastSpecialFinished();
    }

    /// <summary>インスタンス無しでも張り付き解除できるようにする（EditMode / 保険）。</summary>
    public static void ClearSpecialActiveFlag()
    {
        _isSpecialActive = false;
    }

    /// <summary>6-E: Photon RPC 受信側。ゲージをオーナーと揃える。</summary>
    public void ApplySpecialActivatedFromNetwork()
    {
        if (!SpecialNetworkRules.ShouldResetGaugeOnNetworkMirror())
        {
            return;
        }

        if (_specialGauge != null)
        {
            _specialGauge.ResetGauge();
        }
    }

    /// <summary>6-E: Photon RPC 受信側。終了処理をオーナーと揃える。</summary>
    public void ApplySpecialFinishedFromNetwork()
    {
        ApplySpecialFinishedLocal();
    }

    private void ApplySpecialFinishedLocal()
    {
        _isSpecialActive = false;
        if (_specialAction != null)
        {
            _specialAction.EndSpecial();
        }
    }

    private void TryBroadcastSpecialActivated()
    {
        if (!ShouldSyncSpecialOverPhoton())
        {
            return;
        }

        PhotonAnimalFacade photonFacade = ResolvePhotonAnimalFacade();
        if (photonFacade != null)
        {
            photonFacade.BroadcastSpecialActivated();
        }
    }

    private void TryBroadcastSpecialFinished()
    {
        if (!ShouldSyncSpecialOverPhoton())
        {
            return;
        }

        PhotonAnimalFacade photonFacade = ResolvePhotonAnimalFacade();
        if (photonFacade != null)
        {
            photonFacade.BroadcastSpecialFinished();
        }
    }

    private static bool ShouldSyncSpecialOverPhoton()
    {
        ConstData.BATTLE_MODE battleMode = PhotonPlayerInfo.Instance != null
            ? PhotonPlayerInfo.Instance.BattleMode
            : ConstData.BATTLE_MODE.NPC;
        return SpecialNetworkRules.ShouldSyncSpecialOverPhoton(battleMode, PhotonNetwork.InRoom);
    }

    private PhotonAnimalFacade ResolvePhotonAnimalFacade()
    {
        AnimalFacade facade = GetComponentInParent<AnimalFacade>();
        return facade != null ? facade.GetPhotonAnimalFacade() : null;
    }
}
