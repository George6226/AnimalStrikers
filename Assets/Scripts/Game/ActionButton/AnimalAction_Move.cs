using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// アニマルの移動アクション
public class AnimalAction_Move : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Move;
    // ダッシュ中か?
    [SerializeField] private AnimalAction_Dash _dash;
    [SerializeField] private AnimalFacade _myFacade;
    [SerializeField] private AnimalHandler _animalHandler;
    
    // スライドパッドの値
    private float slideScale = 0.0f;
    private float radian = 0.0f;

    /// <summary>
    /// slideScaleとradianを設定する
    /// </summary>
    public void SetSlideValues(float scale, float rad)
    {
        slideScale = scale;
        radian = rad;
    }

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装
    /// SetSlideValuesで設定されたslideScaleとradianを使用して移動処理を実行
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if(!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return; 

        // スライド強度が0なら立ち状態
        if (slideScale <= 0.0f)
        {
            _animalHandler.stand();
            return;
        }

        float dash = (_dash != null && _dash.DashNow) ? ConstData.DASH_MULTIPLIER : 1.0f;

        // 速度の計算
        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        AnimalSpritInfo animalSpritInfo = _myFacade != null ? _myFacade.GetAnimalSpritInfo() : null;
        Param_SpritData paramSpritData = animalSpritInfo != null ? animalSpritInfo.ParamSpritData : null;
        float baseSpeed = paramSpritData != null ? paramSpritData.GetBaseParameterValue(Param_SpritData.ParameterType.Speed) : 0f;
        float increaseSpeed = paramSpritData != null ? paramSpritData.GetIncreaseParameterValue(Param_SpritData.ParameterType.Speed) : 0f;
        float speedStat = animalInfo != null ? animalInfo.Speed : 0f;
        float speedMag = (baseSpeed + (increaseSpeed * speedStat / 100.0f)) * dash;

        // 移動
        _animalHandler.move(slideScale, speedMag);

        // サブプレイヤーの場合は180度回転
        if (!PhotonNetwork.IsMasterClient)
        {
            radian += Mathf.PI;
        }
        _animalHandler.rotate(radian);
    }
}
