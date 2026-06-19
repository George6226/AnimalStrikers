using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "GOAP/Action/Movement/RecievePassEnemyCenterAction")]
public class RecievePassEnemyCenterActionSO : GoapActionSO
{
    [Header("パス受け移動設定")]
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
    private Vector3 _enemyCenterTargetPosition;
    private bool _isTargetInitialized = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "RecievePassEnemyCenterAction";
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
            new GoapCondition(SymbolTag.Position.MY_FIELD_NOW, false), // 敵陣地にいる
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
            InitializeEnemyCenterTargetPosition();
        }
        
        // 現在位置と目標位置までの距離コスト
        float distanceCost = PassReceiveCostCalculator.CalculateDistanceCost(bb, _enemyCenterTargetPosition);
        adjustment += distanceCost;
        
        // 目標位置がボールに近いほどコストを高くする
        float ballProximityCost = PassReceiveCostCalculator.CalculateBallProximityCost(_enemyCenterTargetPosition);
        adjustment += ballProximityCost;
        
        // 目標位置周辺に自分以外のキャラ（敵と味方）が多いほどコストを高くする
        float characterDensityCost = PassReceiveCostCalculator.CalculateCharacterDensityCost(_enemyCenterTargetPosition, bb);
        adjustment += characterDensityCost;

        // 敵陣にいる場合のコスト調整
        float enemyFieldBonus = PassReceiveCostCalculator.CalculateEnemyFieldBonus(_enemyCenterTargetPosition);
        adjustment += enemyFieldBonus;
        
        Debug.Log($"[RecievePassEnemyCenterActionSO] 距離コスト: {distanceCost:F2}, ボール近接コスト: {ballProximityCost:F2}, キャラクター密度コスト: {characterDensityCost:F2}, 敵陣ボーナス: {enemyFieldBonus:F2}, 総調整値: {adjustment:F2}");
        
        return adjustment;
    }
    
    // 初回のみ実行する初期化
    private void InitializeEnemyCenterTargetPosition()
    {
        // フィールド情報の取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;

        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        
        // 敵陣中央（X中央、Zプラス）を狙う
        float targetX = fieldCenter.x;  // 中央
        float targetZ = fieldCenter.z + fieldLength * 0.5f;  // 敵陣側の中央
        float targetY = 0f; // Y座標は0
        
        _enemyCenterTargetPosition = new Vector3(targetX, targetY, targetZ);
        _isTargetInitialized = true;
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new RecievePassEnemyCenterActionRuntime(this, debugName);
    }
}

