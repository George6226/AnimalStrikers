using UnityEngine;
using Game.Goap;
using System.Collections.Generic;

/// <summary>
/// パス受けアクションのコスト計算を共通化するユーティリティクラス
/// </summary>
public static class PassReceiveCostCalculator
{
    /// <summary>
    /// 任意ターゲットへの距離に応じたコストを計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <param name="targetPosition">目標位置</param>
    /// <returns>距離に応じたコスト</returns>
    public static float CalculateDistanceCost(PlayerBlackboard bb, Vector3 targetPosition)
    {
        Vector3 currentPosition = bb.PhysicalState.Position;
        float distance = Vector3.Distance(currentPosition, targetPosition);
        float distanceCost = distance * 0.1f;
        Debug.Log($"[PassReceiveCostCalculator] 任意ターゲット距離: {distance:F2}, 距離コスト: {distanceCost:F2}");
        return distanceCost;
    }

    /// <summary>
    /// 目標位置がボールに近いほどコストを高くする
    /// </summary>
    /// <param name="targetPosition">目標位置</param>
    /// <returns>ボール近接コスト</returns>
    public static float CalculateBallProximityCost(Vector3 targetPosition)
    {
        return 0.0f;
        // ボールの位置をTeamBlackboardから取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ballPosition = teamBB.BallInfo.BallPosition;

        // 目標位置とボールの距離を計算
        float distanceToBall = Vector3.Distance(targetPosition, ballPosition);

        // 距離が近いほどコストを高くする（逆数による増加）
        float ballProximityCost = 0f;
        if (distanceToBall > 0.1f) // 0除算を防ぐ
        {
            ballProximityCost = 5.0f / distanceToBall;
        }
        else
        {
            ballProximityCost = 50.0f; // 非常に近い場合の最大コスト
        }

        // コストを適切な範囲に制限
        ballProximityCost = Mathf.Clamp(ballProximityCost, 0f, 20f);

        Debug.Log($"[PassReceiveCostCalculator] ボール距離: {distanceToBall:F2}, ボール近接コスト: {ballProximityCost:F2}");
        return ballProximityCost;
    }

    /// <summary>
    /// 目標位置周辺の自分以外のキャラ（敵と味方）が近くにいる場合にコストを高くする
    /// </summary>
    /// <param name="targetPosition">目標位置</param>
    /// <param name="currentPlayer">現在のプレイヤー（自分）</param>
    /// <param name="detectionRadius">検出する半径</param>
    /// <returns>敵と味方の密度に基づくコスト</returns>
    public static float CalculateCharacterDensityCost(Vector3 targetPosition, PlayerBlackboard currentPlayer, float detectionRadius = 5.0f)
    {
        return 0.0f;
        // チームブラックボードから敵と味方の位置情報を取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        List<Vector3> teammatePositions = teamBB.BasicInfo.TeammatePositions;
        
        int totalCharacterCount = 0;
        float totalIndividualCost = 0f;
        
        // 目標位置周辺の敵をカウント
        foreach (var enemyPosition in enemyPositions)
        {
            float distanceToTarget = Vector3.Distance(enemyPosition, targetPosition);
            if (distanceToTarget <= detectionRadius)
            {
                totalCharacterCount++;
                
                // 敵一人当たりのコストを計算（敵はより危険なので高コスト）
                float individualCost = 2.5f; // 敵の基本コスト
                float distanceMultiplier = Mathf.Max(0f, detectionRadius - distanceToTarget) / detectionRadius;
                float individualTotalCost = individualCost + (distanceMultiplier * 3.5f);
                
                totalIndividualCost += individualTotalCost;
                
                Debug.Log($"[PassReceiveCostCalculator] 敵{totalCharacterCount}: 距離={distanceToTarget:F2}, 個別コスト={individualTotalCost:F2}");
            }
        }
        
        // 目標位置周辺の味方をカウント（自分とボール所持者を除く）
        foreach (var teammatePosition in teammatePositions)
        {
            // 自分の位置と比較して自分を除外
            if (currentPlayer != null && currentPlayer.PhysicalState != null)
            {
                float distanceToSelf = Vector3.Distance(teammatePosition, currentPlayer.PhysicalState.Position);
                if (distanceToSelf < 0.1f) // 自分を除外
                {
                    continue;
                }
            }
            
            float distanceToTarget = Vector3.Distance(teammatePosition, targetPosition);
            if (distanceToTarget <= detectionRadius)
            {
                totalCharacterCount++;
                
                // 味方一人当たりのコストを計算（味方は敵より低コストだが、同じ場所を取り合う）
                float individualCost = 1.5f; // 味方の基本コスト
                float distanceMultiplier = Mathf.Max(0f, detectionRadius - distanceToTarget) / detectionRadius;
                float individualTotalCost = individualCost + (distanceMultiplier * 2.0f);
                
                totalIndividualCost += individualTotalCost;
                
                Debug.Log($"[PassReceiveCostCalculator] 味方{totalCharacterCount}: 距離={distanceToTarget:F2}, 個別コスト={individualTotalCost:F2}");
            }
        }
        
        // キャラクター密度コストを計算
        float characterDensityCost = 0f;
        
        if (totalCharacterCount > 0)
        {
            // 各キャラクターの個別コストの合計を使用
            characterDensityCost = totalIndividualCost;
        }
        
        // コストを適切な範囲に制限
        characterDensityCost = Mathf.Clamp(characterDensityCost, 0f, 20f);
        
        Debug.Log($"[PassReceiveCostCalculator] 目標位置周辺の総キャラクター数: {totalCharacterCount}, キャラクター密度コスト合計: {characterDensityCost:F2}");
        return characterDensityCost;
    }

    /// <summary>
    /// 目標地点が敵陣にある場合のコスト調整を計算
    /// </summary>
    /// <param name="targetPosition">目標位置</param>
    /// <returns>敵陣コスト調整値（負の値でコストを下げる）</returns>
    public static float CalculateEnemyFieldBonus(Vector3 targetPosition)
    {
        // フィールド中心のZ座標を取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        
        // 目標位置が敵陣（Z軸が正）にあるかチェック
        if (targetPosition.z > fieldCenter.z)
        {
            // 敵陣にある場合、コストを下げる（ボーナス）
            float enemyFieldBonus = -3.0f; // 負の値でコストを下げる
            
            Debug.Log($"[PassReceiveCostCalculator] 目標位置が敵陣にあります。敵陣ボーナス: {enemyFieldBonus:F2}");
            return enemyFieldBonus;
        }
        else
        {
            // 自陣にある場合、ボーナスなし
            Debug.Log($"[PassReceiveCostCalculator] 目標位置が自陣にあります。敵陣ボーナス: 0.0");
            return 0f;
        }
    }

    // TODO:自陣にある場合のコスト調整を計算
} 