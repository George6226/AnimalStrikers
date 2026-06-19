using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// アニマルの必殺技アクション(個別)
public class AnimalAction_Special : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Special;

    [SerializeField] private AnimalAction_Gauge _specialGauge;
    [SerializeField] private AnimalHandler _animalHandler;
    // スペシャルアクション
    [SerializeField] private AnimalSpecialActionBase _specialAction;

    // スペシャル発動中（全体フラグ）
    private static bool _isSpecialActive = false;
    public static bool IsSpecialActive => _isSpecialActive;

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;
        
        // ゲージが足りない
        // if(_specialGauge != null && _specialGauge.GaugeValue < 1.0f)
        // {
        //     return;
        // }

        // スペシャルアクションの発動条件を満たしているかチェック
        if(CanExecuteSpecial())
        {
            ActivateSpecial();
        }
    }

    public bool CanExecuteSpecial()
    {
        return _specialAction != null && _specialAction.CanExecuteSpecial();
    }

    public void ActivateSpecial()
    {
        // スペシャル発動中
        _isSpecialActive = true;
        // スペシャル発動(アニメーション)
        _animalHandler.special();
        // ゲージをリセット
        _specialGauge.ResetGauge();
        // スペシャルアクションを実行
        _specialAction.ExecuteSpecial();
    }

    public void onSpecialFinished()
    {
        // TODO:Photonの同期処理を行う
        _isSpecialActive = false;
    }
}
