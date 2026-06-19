using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルのダッシュアクション
public class AnimalAction_Dash : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（DashUpとDashDownの両方に対応）
    // bit演算で検索されるため、DashDown(5)とDashUp(6)のbitを設定
    public override int ButtonType => (1 << (int)AnimalButtonType.DashDown) | (1 << (int)AnimalButtonType.DashUp);

    // ダッシュ中
    private bool _dashNow = false;
    public bool DashNow
    {
        get { return _dashNow; }
    }


    // 外部から直接true/falseを設定するためのメソッド
    public void SetDash(bool value)
    {
        _dashNow = value;
    }

    public override void Execute()
    {
    }
}
