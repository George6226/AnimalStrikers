using System.Collections.Generic;
using UnityEngine;

// TeamBlackboardの計算処理を担当するクラス
public class TeamBlackboardCalculator
{
    // // チームフォーメーションの中心を計算
    // public static Vector3 CalculateTeamFormationCenter(List<Vector3> teammatePositions)
    // {
    //     if (teammatePositions.Count == 0)
    //         return Vector3.zero;
        
    //     Vector3 center = Vector3.zero;
    //     foreach (var pos in teammatePositions)
    //     {
    //         center += pos;
    //     }
    //     return center / teammatePositions.Count;
    // }
    
    // // 攻守モードの更新
    // public static void UpdateTeamMode(TeamTacticalInfo tacticalInfo, TeamBallInfo ballInfo)
    // {
    //     bool isOffensive = false;
    //     bool isDefensive = false;
        
    //     if (ballInfo.BallBelongTeam == BallManager.BELONG_TEAM.PLAYER)
    //     {
    //         isOffensive = true;
    //         isDefensive = false;
    //     }
    //     else if (ballInfo.BallBelongTeam == BallManager.BELONG_TEAM.ENEMY)
    //     {
    //         isOffensive = false;
    //         isDefensive = true;
    //     }
        
    //     // 既存の値を保持して更新
    //     //tacticalInfo.Update(isOffensive, isDefensive, tacticalInfo.TeamFormationCenter, tacticalInfo.TeamPressure,
    //     //                   tacticalInfo.BestPassTarget, tacticalInfo.BestShootPosition, 
    //     //                   tacticalInfo.PassSuccessRate, tacticalInfo.ShootSuccessRate);
    // }
    
    // // ゴール危険度の計算
    // public static float CalculateGoalDanger(Vector3 ballPosition, Vector3 goalPosition, float fieldLength)
    // {
    //     float distance = Vector3.Distance(ballPosition, goalPosition);
    //     return Mathf.Max(0, 1 - (distance / fieldLength));
    // }
    
    // // ゴール機会の計算
    // public static float CalculateGoalOpportunity(Vector3 ballPosition, Vector3 goalPosition, float fieldLength)
    // {
    //     float distance = Vector3.Distance(ballPosition, goalPosition);
    //     return Mathf.Max(0, 1 - (distance / fieldLength));
    // }
    
    // // 最適なパス先の計算
    // public static Vector3 CalculateBestPassTarget(List<Vector3> teammatePositions, Vector3 ballPosition)
    // {
    //     if (teammatePositions.Count == 0)
    //         return Vector3.zero;
        
    //     // 最も空いている味方の位置を返す（簡易実装）
    //     return teammatePositions[0];
    // }
    
    // // 最適なシュート位置の計算
    // public static Vector3 CalculateBestShootPosition(Vector3 enemyGoalPosition)
    // {
    //     return enemyGoalPosition;
    // }
    
    // // チーム圧力レベルの計算
    // public static float CalculateTeamPressure(List<Vector3> enemyPositions, Vector3 ballPosition, float pressureRadius = 10f)
    // {
    //     float pressure = 0f;
        
    //     foreach (var enemyPos in enemyPositions)
    //     {
    //         float distance = Vector3.Distance(ballPosition, enemyPos);
    //         if (distance < pressureRadius)
    //         {
    //             pressure += (pressureRadius - distance) / pressureRadius;
    //         }
    //     }
        
    //     return Mathf.Clamp01(pressure);
    // }
    
    // // 敵フォーメーション中心の計算
    // public static Vector3 CalculateEnemyFormationCenter(List<Vector3> enemyPositions)
    // {
    //     if (enemyPositions.Count == 0)
    //         return Vector3.zero;
        
    //     Vector3 center = Vector3.zero;
    //     foreach (var pos in enemyPositions)
    //     {
    //         center += pos;
    //     }
    //     return center / enemyPositions.Count;
    // }
    
    // // 敵圧力レベルの計算
    // public static float CalculateEnemyPressure(List<Vector3> enemyPositions, Vector3 teamFormationCenter, float pressureRadius = 8f)
    // {
    //     float pressure = 0f;
        
    //     foreach (var enemyPos in enemyPositions)
    //     {
    //         float distance = Vector3.Distance(teamFormationCenter, enemyPos);
    //         if (distance < pressureRadius)
    //         {
    //             pressure += (pressureRadius - distance) / pressureRadius;
    //         }
    //     }
        
    //     return Mathf.Clamp01(pressure);
    // }
    
    // // フィールド中心の計算
    // public static Vector3 CalculateFieldCenter(Vector3 ownGoalPosition, Vector3 enemyGoalPosition)
    // {
    //     return (ownGoalPosition + enemyGoalPosition) / 2f;
    // }
    
    // // ボール保持時間の計算
    // public static float CalculatePossessionTime(float lastPossessionChangeTime, bool teamHasBall)
    // {
    //     if (!teamHasBall)
    //         return 0f;
        
    //     return Time.time - lastPossessionChangeTime;
    // }
    
    // // ボール奪取の可能性の計算
    // public static float CalculateBallRecoveryChance(List<Vector3> teammatePositions, Vector3 ballPosition, float recoveryRadius = 5f)
    // {
    //     float chance = 0f;
        
    //     foreach (var teammatePos in teammatePositions)
    //     {
    //         float distance = Vector3.Distance(teammatePos, ballPosition);
    //         if (distance < recoveryRadius)
    //         {
    //             chance += (recoveryRadius - distance) / recoveryRadius;
    //         }
    //     }
        
    //     return Mathf.Clamp01(chance);
    // }
    
    // // チームメンバーの状態を更新
    // public static void UpdateTeamMemberStates(TeamMemberState memberState, List<GameObject> teammates, Vector3 ballPosition)
    // {
    //     List<bool> canReceivePass = new();
    //     List<float> stamina = new();
    //     List<Vector3> optimalPositions = new();
        
    //     foreach (var teammate in teammates)
    //     {
    //         if (teammate != null)
    //         {
    //             // パス受信可能状態（簡易判定）
    //             float distanceToBall = Vector3.Distance(teammate.transform.position, ballPosition);
    //             canReceivePass.Add(distanceToBall < 10f);
                
    //             // スタミナ（仮の値）
    //             stamina.Add(100f);
                
    //             // 最適位置（現在位置を仮の最適位置とする）
    //             optimalPositions.Add(teammate.transform.position);
    //         }
    //     }
        
    //     //memberState.Update(canReceivePass, stamina, optimalPositions);
    // }
    
    // // パス成功率の予測
    // public static float CalculateTeamPassSuccessRate(Vector3 passerPosition, Vector3 receiverPosition, float baseAccuracy = 0.8f)
    // {
    //     float distance = Vector3.Distance(passerPosition, receiverPosition);
    //     float distanceFactor = Mathf.Max(0.5f, 1f - distance / 30f);
    //     return baseAccuracy * distanceFactor;
    // }
    
    // // シュート成功率の予測
    // public static float CalculateTeamShootSuccessRate(Vector3 shooterPosition, Vector3 goalPosition, float basePower = 0.7f)
    // {
    //     float distance = Vector3.Distance(shooterPosition, goalPosition);
    //     float distanceFactor = Mathf.Max(0.3f, 1f - distance / 40f);
    //     return basePower * distanceFactor;
    // }
    
    // // チーム戦術スコアの計算
    // public static float CalculateTeamTacticalScore(TeamTacticalInfo tacticalInfo, TeamBallInfo ballInfo, Vector3 ballPosition, Vector3 enemyGoalPosition)
    // {
    //     float score = 0f;
        
    //     //// 攻撃モード時のスコア
    //     //if (tacticalInfo.IsOffensiveMode)
    //     //{
    //     //    float distanceToGoal = Vector3.Distance(ballPosition, enemyGoalPosition);
    //     //    score += Mathf.Max(0, 1f - distanceToGoal / 50f);
    //     //}
        
    //     //// 守備モード時のスコア
    //     //if (tacticalInfo.IsDefensiveMode)
    //     //{
    //     //    // 守備時の評価（敵からの距離など）
    //     //    score += 0.5f;
    //     //}
        
    //     return Mathf.Clamp01(score);
    // }
    
    // // フィールド位置の正規化
    // public static Vector3 NormalizeFieldPosition(Vector3 position, Vector3 fieldCenter, float fieldLength, float fieldWidth)
    // {
    //     Vector3 normalized = position - fieldCenter;
    //     normalized.x = Mathf.Clamp(normalized.x, -fieldWidth/2, fieldWidth/2);
    //     normalized.z = Mathf.Clamp(normalized.z, -fieldLength/2, fieldLength/2);
    //     return fieldCenter + normalized;
    // }
    
    // // 時間関連の更新（統合されたGameStateクラスに対応）
    // public static void UpdateTimeInfo(TeamGameState gameState, bool teamHasBall, float lastPossessionChangeTime)
    // {
    //     float possessionTime = 0f;
    //     if (teamHasBall)
    //     {
    //         possessionTime = CalculatePossessionTime(lastPossessionChangeTime, true);
    //     }
        
    //     //gameState.Update(gameState.TeamScore, gameState.EnemyScore, Time.time, gameState.IsGameActive, 
    //     //                gameState.LastGoalTime, possessionTime, Time.time);
    // }
} 