using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "GOAP/Action/Movement/RecievePassMyLeftSideAction")]
public class RecievePassMyLeftSideActionSO : GoapActionSO
{
    private float _goodPositionRadius = 0.5f;
    public float GoodPositionRadius
    {
        get
        {
            _goodPositionRadius = 0.5f;
            Debug.Log($"GoodPositionRadius: {_goodPositionRadius}");
            return _goodPositionRadius;
        }
    }

    // 一度だけ設定する目標位置
    private Vector3 _myLeftSideTargetPosition;
    private bool _isTargetInitialized = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "RecievePassMyLeftSideAction";
        SetupPreconditionsAndEffects();
    }

    private void SetupPreconditionsAndEffects()
    {
        // 前提条件：移動可能、攻撃中、味方がボールを保持
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true), // 移動可能
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true), // チームがボールを持っている
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false), // 自分はボールを持っていない
            new GoapCondition(SymbolTag.Tactical.OFFENSIVE_MODE, true), // 攻撃中
            new GoapCondition(SymbolTag.Position.MY_FIELD_NOW, true), // 自分の陣地にいる
        });
        // 効果：良い位置にいる
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true), // パスを受け取ることができる
        });
    }

    // 動的にコストを計算する
    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        float adjustment = 0f;
        
        // 未初期化なら初期化（安全策）
        if (!_isTargetInitialized)
        {
            InitializeMyLeftSideTargetPosition();
        }
        
        // 現在位置と目標位置までの距離コスト
        float distanceCost = PassReceiveCostCalculator.CalculateDistanceCost(bb, _myLeftSideTargetPosition);
        adjustment += distanceCost;
        
        // 目標位置がボールに近いほどコストを高くする
        float ballProximityCost = PassReceiveCostCalculator.CalculateBallProximityCost(_myLeftSideTargetPosition);
        adjustment += ballProximityCost;
        
        // 目標位置周辺に自分以外のキャラ（敵と味方）が多いほどコストを高くする
        float characterDensityCost = PassReceiveCostCalculator.CalculateCharacterDensityCost(_myLeftSideTargetPosition, bb);
        adjustment += characterDensityCost;

        // 敵陣にいる場合のコスト調整
        float enemyFieldBonus = PassReceiveCostCalculator.CalculateEnemyFieldBonus(_myLeftSideTargetPosition);
        adjustment += enemyFieldBonus;
        
        Debug.Log($"[RecievePassMyLeftSideActionSO] 距離コスト: {distanceCost:F2}, ボール近接コスト: {ballProximityCost:F2}, キャラクター密度コスト: {characterDensityCost:F2}, 敵陣ボーナス: {enemyFieldBonus:F2}, 総調整値: {adjustment:F2}");
        
        return adjustment;
    }
    
    // 初回のみ実行する初期化
    private void InitializeMyLeftSideTargetPosition()
    {
        // フィールド情報の取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;

        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        
        // 横は三分割/たては半分
        float oneThirdWidth = fieldWidth / 10.0f * 3.0f;
        float halfLength = fieldLength * 0.5f;
        
        // 自陣左側（Xマイナス、Zマイナス）を狙う
        float targetX = fieldCenter.x - oneThirdWidth;   // 1/3分左に寄った位置
        float targetZ = fieldCenter.z - halfLength * 0.5f;  // 自陣側の1/2分寄った位置
        float targetY = 0f; // Y座標は0
        
        _myLeftSideTargetPosition = new Vector3(targetX, targetY, targetZ);
        _isTargetInitialized = true;
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new RecievePassMyLeftSideActionRuntime(this, debugName);
    }
}

