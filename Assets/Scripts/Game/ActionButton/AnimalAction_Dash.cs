using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルのダッシュアクション
public class AnimalAction_Dash : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（DashUpとDashDownの両方に対応）
    // bit演算で検索されるため、DashDown(5)とDashUp(6)のbitを設定
    public override int ButtonType => (1 << (int)AnimalButtonType.DashDown) | (1 << (int)AnimalButtonType.DashUp);

    [SerializeField] private AnimalFacade _myFacade;

    // ダッシュ中
    private bool _dashNow = false;
    private PhotonHPGauge _hpGauge;

    public bool DashNow => _dashNow;

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }

        if (_myFacade != null)
        {
            _hpGauge = _myFacade.GetHPGauge();
        }
    }

    /// <summary>残スタミナ比率からダッシュ可否を判定（F2）。0 以下は不可。</summary>
    public static bool CanDashFromStaminaRatio(float staminaRatio) => staminaRatio > 0f;

    /// <summary>現在のスタミナでダッシュ可能か（ゲージ未設定時は許可）。</summary>
    public bool CanDashNow()
    {
        if (_hpGauge == null)
        {
            return true;
        }

        return CanDashFromStaminaRatio(_hpGauge.StaminaRatio);
    }

    // 外部から直接true/falseを設定するためのメソッド
    public void SetDash(bool value)
    {
        if (value && !CanDashNow())
        {
            return;
        }

        _dashNow = value;
    }

    private void Update()
    {
        if (_dashNow && !CanDashNow())
        {
            _dashNow = false;
        }
    }

    public override void Execute()
    {
    }
}
