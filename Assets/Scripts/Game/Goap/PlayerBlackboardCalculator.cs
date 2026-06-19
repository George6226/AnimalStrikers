using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// PlayerBlackboardの計算処理を担当するクラス
public class PlayerBlackboardCalculator
{
    // // プレッシャーレベルの計算
    // public static float CalculatePressureLevel(PlayerSurroundingInfo surroundingInfo)
    // {
    //     float pressure = 0f;
        
    //     //// 近くの敵によるプレッシャー
    //     //foreach (var enemy in surroundingInfo._nearbyEnemies)
    //     //{
    //     //    if (enemy != null)
    //     //    {
    //     //        // プレイヤーの位置を取得（仮想的な位置としてVector3.zeroを使用）
    //     //        Vector3 playerPos = Vector3.zero; // 実際の使用時はプレイヤーの位置を渡す
    //     //        float distance = Vector3.Distance(playerPos, enemy.transform.position);
    //     //        if (distance < 3f)
    //     //        {
    //     //            pressure += (3f - distance) / 3f;
    //     //        }
    //     //    }
    //     //}
        
    //     return Mathf.Clamp01(pressure);
    // }
    
    // // パス成功率の計算
    // public static float CalculatePassSuccessRate(PlayerBasicData basicData, PlayerPhysicalState physicalState, 
    //     PlayerBallState ballState, PlayerSurroundingInfo surroundingInfo, float passAccuracy = 0.8f)
    // {
    //     if (!ballState.HasBall) return 0f;
        
    //     float baseRate = passAccuracy;
        
    //     // プレッシャーによる影響
    //     //baseRate *= (1f - surroundingInfo._pressureLevel * 0.3f);
        
    //     // 距離による影響
    //     if (ballState.BallDistance > 0f)
    //     {
    //         baseRate *= Mathf.Max(0.5f, 1f - ballState.BallDistance / 20f);
    //     }
        
    //     return Mathf.Clamp01(baseRate);
    // }
    
    // // シュート成功率の計算
    // public static float CalculateShootSuccessRate(PlayerBasicData basicData, PlayerBallState ballState, 
    //     PlayerSurroundingInfo surroundingInfo, float shootPower = 0.7f, float goalDistance = 30f)
    // {
    //     if (!ballState.HasBall) return 0f;
        
    //     float baseRate = shootPower;
        
    //     // 距離による影響
    //     baseRate *= Mathf.Max(0.3f, 1f - goalDistance / 30f);
        
    //     // プレッシャーによる影響
    //     //baseRate *= (1f - surroundingInfo._pressureLevel * 0.4f);
        
    //     return Mathf.Clamp01(baseRate);
    // }
    
    // // タックル成功率の計算
    // public static float CalculateTackleSuccessRate(PlayerBallState ballState, PlayerSurroundingInfo surroundingInfo, 
    //     float tackleRange = 2f)
    // {
    //     if (ballState.HasBall) return 0f;
        
    //     float baseRate = 0.6f;
        
    //     // 最も近い敵との距離を計算
    //     float nearestEnemyDistance = float.MaxValue;
    //     //foreach (var enemy in surroundingInfo._nearbyEnemies)
    //     //{
    //     //    if (enemy != null)
    //     //    {
    //     //        Vector3 playerPos = Vector3.zero; // 実際の使用時はプレイヤーの位置を渡す
    //     //        float distance = Vector3.Distance(playerPos, enemy.transform.position);
    //     //        nearestEnemyDistance = Mathf.Min(nearestEnemyDistance, distance);
    //     //    }
    //     //}
        
    //     // 距離による影響
    //     if (nearestEnemyDistance < tackleRange)
    //     {
    //         baseRate *= (tackleRange - nearestEnemyDistance) / tackleRange;
    //     }
    //     else
    //     {
    //         baseRate = 0f;
    //     }
        
    //     return Mathf.Clamp01(baseRate);
    // }
    
    // // 最適なパス先の計算
    // public static Vector3 CalculateBestPassTarget(PlayerPhysicalState physicalState, PlayerSurroundingInfo surroundingInfo)
    // {
    //     Vector3 bestTarget = Vector3.zero;
    //     float bestScore = 0f;
        
    //     //foreach (var teammate in surroundingInfo._nearbyTeammates)
    //     //{
    //     //    if (teammate != null)
    //     //    {
    //     //        float score = CalculatePassTargetScore(physicalState.Position, teammate.transform.position, surroundingInfo);
    //     //        if (score > bestScore)
    //     //        {
    //     //            bestScore = score;
    //     //            bestTarget = teammate.transform.position;
    //     //        }
    //     //    }
    //     //}
        
    //     return bestTarget;
    // }
    
    // // パス先のスコア計算
    // private static float CalculatePassTargetScore(Vector3 playerPos, Vector3 targetPos, PlayerSurroundingInfo surroundingInfo)
    // {
    //     float score = 0f;
        
    //     // 距離によるスコア
    //     float distance = Vector3.Distance(playerPos, targetPos);
    //     score += 1f / (1f + distance);
        
    //     // 敵からの距離によるスコア
    //     float minEnemyDistance = float.MaxValue;
    //     //foreach (var enemy in surroundingInfo._nearbyEnemies)
    //     //{
    //     //    if (enemy != null)
    //     //    {
    //     //        float enemyDist = Vector3.Distance(targetPos, enemy.transform.position);
    //     //        minEnemyDistance = Mathf.Min(minEnemyDistance, enemyDist);
    //     //    }
    //     //}
    //     score += minEnemyDistance / 10f;
        
    //     return score;
    // }
    
    // // 最適なシュート位置の計算
    // public static Vector3 CalculateBestShootPosition(PlayerPhysicalState physicalState, Vector3 goalPosition)
    // {
    //     Vector3 direction = (physicalState.Position - goalPosition).normalized;
    //     return physicalState.Position + direction * 2f; // ゴール方向に2m進んだ位置
    // }
    
    // // ボール距離の計算
    // public static float CalculateBallDistance(Vector3 playerPosition, Vector3 ballPosition)
    // {
    //     return Vector3.Distance(playerPosition, ballPosition);
    // }
    
    // // ボール方向の計算
    // public static Vector3 CalculateBallDirection(Vector3 playerPosition, Vector3 ballPosition)
    // {
    //     return (ballPosition - playerPosition).normalized;
    // }
    
    // // 移動速度の計算
    // public static float CalculateMoveSpeed(Vector3 velocity)
    // {
    //     return velocity.magnitude;
    // }
    
    // // 移動中かどうかの判定
    // public static bool IsMoving(float moveSpeed, float threshold = 0.1f)
    // {
    //     return moveSpeed > threshold;
    // }
    
    // // 周辺の味方を検出
    // public static List<GameObject> DetectNearbyTeammates(Vector3 playerPosition, float detectionRange = 8f)
    // {
    //     List<GameObject> nearbyTeammates = new List<GameObject>();
    //     var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
    //     foreach (var player in allPlayers)
    //     {
    //         if (player != null)
    //         {
    //             float distance = Vector3.Distance(playerPosition, player.transform.position);
    //             if (distance < detectionRange)
    //             {
    //                 nearbyTeammates.Add(player);
    //             }
    //         }
    //     }
        
    //     return nearbyTeammates;
    // }
    
    // // 周辺の敵を検出
    // public static List<GameObject> DetectNearbyEnemies(Vector3 playerPosition, float detectionRange = 8f)
    // {
    //     List<GameObject> nearbyEnemies = new List<GameObject>();
    //     var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
    //     foreach (var enemy in allEnemies)
    //     {
    //         if (enemy != null)
    //         {
    //             float distance = Vector3.Distance(playerPosition, enemy.transform.position);
    //             if (distance < detectionRange)
    //             {
    //                 nearbyEnemies.Add(enemy);
    //             }
    //         }
    //     }
        
    //     return nearbyEnemies;
    // }
    
    // // フォーメーション内にいるかの判定
    // public static bool IsInFormation(Vector3 playerPosition, Vector3 formationCenter, float formationRadius = 5f)
    // {
    //     float distance = Vector3.Distance(playerPosition, formationCenter);
    //     return distance <= formationRadius;
    // }
    
    // // 最適な位置の計算
    // public static Vector3 CalculateOptimalPosition(Vector3 formationCenter, Vector3 ballPosition, 
    //     Vector3 goalPosition, float formationRadius = 5f)
    // {
    //     // フォーメーション中心とボール位置の中間点を基本とする
    //     Vector3 basePosition = (formationCenter + ballPosition) / 2f;
        
    //     // ゴール方向に少し寄せる
    //     Vector3 goalDirection = (goalPosition - basePosition).normalized;
    //     Vector3 optimalPosition = basePosition + goalDirection * formationRadius * 0.3f;
        
    //     return optimalPosition;
    // }
    
    // // アクション進行度の計算
    // public static float CalculateActionProgress(float startTime, float duration)
    // {
    //     float elapsedTime = Time.time - startTime;
    //     return Mathf.Clamp01(elapsedTime / duration);
    // }
    
    // // スタン時間の残り計算
    // public static float CalculateRemainingStunTime(float stunStartTime, float stunDuration)
    // {
    //     float elapsedTime = Time.time - stunStartTime;
    //     return Mathf.Max(0f, stunDuration - elapsedTime);
    // }
    
    // // プレイヤーIDの設定
    // public static int AssignPlayerID(GameObject playerObject)
    // {
    //     // 簡易的なID生成（実際の実装ではより複雑なロジックが必要）
    //     return playerObject.GetInstanceID();
    // }
    
    // === PlayerBlackboardから移譲された計算処理 ===

    /// <summary>
    /// 攻撃トランジション状態か（時間窓判定）
    /// </summary>
    public static bool IsOffenseTransition(bool teamHasBall, float timeSinceSwitch, float windowSeconds = 2.0f)
    {
        if (!teamHasBall) return false;
        return timeSinceSwitch >= 0f && timeSinceSwitch <= windowSeconds;
    }

    /// <summary>
    /// 守備トランジション状態か（時間窓判定）
    /// </summary>
    public static bool IsDefenseTransition(bool teamHasBall, float timeSinceSwitch, float windowSeconds = 2.0f)
    {
        if (teamHasBall) return false;
        return timeSinceSwitch >= 0f && timeSinceSwitch <= windowSeconds;
    }

    /// <summary>
    /// 直近ポゼッション切替からの経過時間を返す（未切替なら負値）
    /// </summary>
    public static float GetTimeSincePossessionSwitch(float lastSwitchTime)
    {
        if (lastSwitchTime <= 0f) return -1f;
        return Time.time - lastSwitchTime;
    }
    
    /// <summary>
    /// ボールを持たない敵が近くにいるかチェック
    /// </summary>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <param name="detectionRange">検出範囲</param>
    /// <returns>ボールを持たない敵が近くにいるかどうか</returns>
    public static bool IsNearEnemyNoBall(Vector3 playerPosition, Vector3 ballOwnerPosition, 
        List<Vector3> enemyPositions, float detectionRange = 3f)
    {
        // 敵の位置リストをチェックして、ボールを持たない敵が近くにいるかを判定
        foreach (Vector3 enemyPos in enemyPositions)
        {
            // ボール所持者の位置と一致しない場合（ボールを持たない敵）
            if (ballOwnerPosition == Vector3.zero || Vector3.Distance(enemyPos, ballOwnerPosition) > 0.1f)
            {
                float distanceToEnemy = Vector3.Distance(playerPosition, enemyPos);
                if (distanceToEnemy < detectionRange)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// ボールを持つ敵が近くにいるかチェック
    /// </summary>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="enemyHasBall">敵がボールを持っているか</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="detectionRange">検出範囲</param>
    /// <returns>ボールを持つ敵が近くにいるかどうか</returns>
    public static bool IsNearEnemyHasBall(Vector3 playerPosition, bool enemyHasBall, 
        Vector3 ballOwnerPosition, float detectionRange = 3f)
    {
        // 敵がボールを持っているか? & 敵の位置が設定されているか
        if (enemyHasBall && ballOwnerPosition != Vector3.zero)
        {
            float distanceToEnemyWithBall = Vector3.Distance(playerPosition, ballOwnerPosition);
            return distanceToEnemyWithBall < detectionRange;
        }
        return false;
    }
    
    /// <summary>
    /// パス受信位置にいるかを計算
    /// </summary>
    /// <param name="teamHasBall">チームがボールを持っているか</param>
    /// <param name="hasBall">自分がボールを持っているか</param>
    /// <param name="isStunned">スタン状態か</param>
    /// <param name="ballDistance">ボールとの距離</param>
    /// <param name="fieldLength">フィールドの長さ</param>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="enemyGoalPosition">敵ゴールの位置</param>
    /// <returns>パス受信位置にいるかどうか</returns>
    public static bool CalculateIsInPassReceivePosition(bool teamHasBall, bool hasBall, bool isStunned,
        float ballDistance, float fieldLength, Vector3 playerPosition, List<Vector3> enemyPositions,
        Vector3 ballOwnerPosition, Vector3 enemyGoalPosition)
    {
        // 基本条件チェック
        if (!teamHasBall || hasBall || isStunned) {
            Debug.Log($"パス受信位置判定: 基本条件を満たさない (teamHasBall:{teamHasBall}, hasBall:{hasBall}, isStunned:{isStunned})");
            return false;
        }
        
        // 距離チェック（フィールドサイズ相対）
        // 判定をやや緩和: 受け位置として許容する距離レンジを拡張
        float minDistance = fieldLength * 0.07f;  // 7%
        float maxDistance = fieldLength * 0.55f;  // 55%
        
        if (ballDistance < minDistance || ballDistance > maxDistance) {
            Debug.Log($"パス受信位置判定: 距離が不適切 (ballDistance:{ballDistance:F1}, min:{minDistance:F1}, max:{maxDistance:F1})");
            return false;
        }
        
        // 敵からの距離チェック
        float minEnemyDistance = GetMinEnemyDistance(playerPosition, enemyPositions);
        if (minEnemyDistance < fieldLength * 0.035f) { // 3.5%以内は危険
            Debug.Log($"パス受信位置判定: 敵が近すぎる (minEnemyDistance:{minEnemyDistance:F1})");
            return false;
        }
        
        // パスコースチェック
        if (!IsPassRouteClear(playerPosition, ballOwnerPosition, enemyPositions, fieldLength * 0.06f)) {
            Debug.Log("パス受信位置判定: パスコースが確保できない");
            return false;
        }
        
        // 戦術的位置チェック
        if (!IsInTacticalPosition(playerPosition, ballOwnerPosition, enemyGoalPosition, 75f, 105f)) {
            Debug.Log("パス受信位置判定: 戦術的位置が不適切");
            return false;
        }

        // 保持者の移動に対してサポート関係を維持しているか（到達後にその場に留まるのを防ぐ）
        if (!IsMaintainingSupportRelationship(playerPosition, ballOwnerPosition, enemyGoalPosition, fieldLength)) {
            Debug.Log("パス受信位置判定: 保持者とのサポート関係が崩れている");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 保持者の現在位置に対してサポート位置を維持しているか。
    /// 保持者がドリブルしても NPC が旧位置に留まっている場合は false。
    /// </summary>
    public static bool IsMaintainingSupportRelationship(
        Vector3 playerPosition,
        Vector3 ballOwnerPosition,
        Vector3 enemyGoalPosition,
        float fieldLength)
    {
        Vector3 toPlayer = playerPosition - ballOwnerPosition;
        toPlayer.y = 0f;
        float ownerDistance = toPlayer.magnitude;
        if (ownerDistance < fieldLength * 0.08f || ownerDistance > fieldLength * 0.38f)
        {
            return false;
        }

        Vector3 toGoal = enemyGoalPosition - ballOwnerPosition;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude < 0.0001f || ownerDistance < 0.01f)
        {
            return true;
        }

        float forwardDot = Vector3.Dot(toPlayer / ownerDistance, toGoal.normalized);
        return forwardDot >= 0.12f;
    }
    
    /// <summary>
    /// 最も近い敵との距離を取得
    /// </summary>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <returns>最も近い敵との距離</returns>
    public static float GetMinEnemyDistance(Vector3 playerPosition, List<Vector3> enemyPositions)
    {
        float minDistance = float.MaxValue;
        
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distance = Vector3.Distance(playerPosition, enemyPos);
            minDistance = Mathf.Min(minDistance, distance);
        }
        
        return minDistance;
    }
    
    /// <summary>
    /// パスコースが確保されているかチェック
    /// </summary>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <param name="blockingRange">遮断範囲</param>
    /// <returns>パスコースが確保されているかどうか</returns>
    public static bool IsPassRouteClear(Vector3 playerPosition, Vector3 ballOwnerPosition, 
        List<Vector3> enemyPositions, float blockingRange = 2f)
    {
        // 敵がパスコースを遮断していないかチェック
        foreach (Vector3 enemyPos in enemyPositions)
        {
            // 敵がパスコース上にいるかチェック
            float distanceToPassLine = GetDistanceToLine(enemyPos, ballOwnerPosition, playerPosition);
            if (distanceToPassLine < blockingRange) // 指定範囲以内なら遮断
            {
                Debug.Log($"パス受信位置判定: パスコースが遮断されている (enemyPos: {enemyPos}, distanceToPassLine: {distanceToPassLine:F1})");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 戦術的に有効な位置にいるかチェック
    /// </summary>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="enemyGoalPosition">敵ゴールの位置</param>
    /// <param name="maxAngle">最大角度</param>
    /// <param name="minAngle">最小角度</param>
    /// <returns>戦術的に有効な位置にいるかどうか</returns>
    public static bool IsInTacticalPosition(Vector3 playerPosition, Vector3 ballOwnerPosition, 
        Vector3 enemyGoalPosition, float maxAngle = 60f, float minAngle = 120f)
    {
        // ボール保持者から敵ゴールへの角度を計算
        Vector3 toGoal = (enemyGoalPosition - ballOwnerPosition).normalized;
        Vector3 toPlayer = (playerPosition - ballOwnerPosition).normalized;
        
        // toGoalを基準(0度)として、toPlayerの角度を計算
        float angle = Vector3.Angle(toGoal, toPlayer);

        // デバッグ表示
        Debug.Log($"パス受信位置判定: 角度計算結果: {angle:F1}度, toGoal: {toGoal}, toPlayer: {toPlayer}");
        
        // 前方（0-60度）または後方（120-180度）の位置
        return angle <= maxAngle || angle >= minAngle;
    }
    
    /// <summary>
    /// 点から直線までの距離を計算
    /// </summary>
    /// <param name="point">点の位置</param>
    /// <param name="lineStart">直線の開始点</param>
    /// <param name="lineEnd">直線の終了点</param>
    /// <returns>点から直線までの距離</returns>
    public static float GetDistanceToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 segment = lineEnd - lineStart;
        float segmentLenSq = segment.sqrMagnitude;
        if (segmentLenSq < 0.0001f)
        {
            return Vector3.Distance(point, lineStart);
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, segment) / segmentLenSq);
        Vector3 closestPoint = lineStart + t * segment;
        return Vector3.Distance(point, closestPoint);
    }
    
    /// <summary>
    /// 守備位置にいるかを計算
    /// </summary>
    /// <param name="teamHasBall">チームがボールを持っているか</param>
    /// <param name="hasBall">自分がボールを持っているか</param>
    /// <param name="isStunned">スタン状態か</param>
    /// <param name="fieldLength">フィールドの長さ</param>
    /// <param name="playerPosition">プレイヤーの位置</param>
    /// <param name="ballOwnerPosition">ボール保持者の位置</param>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <param name="enemyGoalPosition">敵ゴールの位置</param>
    /// <returns>守備位置にいるかどうか</returns>
    public static bool CalculateIsInDefensivePosition(
        bool teamHasBall, bool hasBall, bool isStunned,
        float fieldLength, Vector3 playerPosition, Vector3 ballOwnerPosition,
        List<Vector3> enemyPositions, Vector3 enemyGoalPosition)
    {
        // DebugLogger.Log($"守備位置判定: 開始 (teamHasBall:{teamHasBall}, hasBall:{hasBall}, isStunned:{isStunned}), playerPosition:{playerPosition}, ballOwnerPosition:{ballOwnerPosition}, enemyPositions:{enemyPositions.Count}, enemyGoalPosition:{enemyGoalPosition}");
        // 基本条件チェック：守備時のみ（チームがボールを持っていない、自分もボールを持っていない）
        if (teamHasBall || hasBall || isStunned)
        {
            // DebugLogger.Log($"守備位置判定: 基本条件を満たさない (teamHasBall:{teamHasBall}, hasBall:{hasBall}, isStunned:{isStunned})");
            return false;
        }
        
        // 距離チェック（フィールドサイズ相対）
        // 判定をやや緩和: 守備成立距離レンジを拡張
        float minDist = fieldLength * 0.04f;  // 4%
        float maxDist = fieldLength * 0.40f;  // 40%
        float distToOwner = Vector3.Distance(playerPosition, ballOwnerPosition);
        
        if (distToOwner < minDist || distToOwner > maxDist)
        {
            // DebugLogger.Log($"守備位置判定: 距離が不適切 (distToOwner:{distToOwner:F1}, min:{minDist:F1}, max:{maxDist:F1})");
            return false;
        }
        
        // 守備位置の判定：プレッシャー、マーク、パス遮断のいずれかが適切な位置にある
        bool isInGoodPosition = false;
        
        // 1) プレッシャー位置チェック（保持者への最適距離）
        float optimalOwner = fieldLength * 0.15f;  // 15%
        // 距離スコア: 最適距離からの許容誤差を厳密に評価（許容範囲を狭める）
        float distanceTolerance = optimalOwner * 0.6f;  // 最適距離の60%以内を許容
        float distanceError = Mathf.Abs(distToOwner - optimalOwner);
        float pressureDistanceScore = Mathf.Clamp01(1f - (distanceError / Mathf.Max(distanceTolerance, 0.01f)));
        
        Vector3 toGoal = (enemyGoalPosition - ballOwnerPosition).normalized;
        Vector3 lateral = Vector3.Cross(toGoal, Vector3.up).normalized;
        Vector3 toPlayer = (playerPosition - ballOwnerPosition).normalized;
        // 横方向アライメント: より厳密に評価（完全に横方向に近いほど高スコア）
        float lateralAlign = Mathf.Abs(Vector3.Dot(toPlayer, lateral));
        // 前方/後方成分を減点（横方向に近いほど良い）
        float forwardComponent = Mathf.Abs(Vector3.Dot(toPlayer, toGoal));
        float lateralScore = lateralAlign * (1f - forwardComponent * 0.5f);  // 前方/後方成分があると減点
        
        // 総合スコア: 距離と横方向の両方が重要（重みを調整）
        float pressureScore = 0.55f * pressureDistanceScore + 0.45f * lateralScore;
        
        // より厳密な判定: 距離スコアと横方向スコアの両方が一定以上必要
        float minDistanceScore = 0.35f;  // 距離スコアの最低要求
        float minLateralScore = 0.35f;   // 横方向スコアの最低要求
        bool distanceOk = pressureDistanceScore >= minDistanceScore;
        bool lateralOk = lateralScore >= minLateralScore;
        
        // // プレッシャー計算に使った値を詳細にデバッグ表示
        // DebugLogger.Log(
        //     $"守備位置判定: プレッシャー計算詳細 " +
        //     $"(pressureScore:{pressureScore:F2}, distanceScore:{pressureDistanceScore:F2}, lateralScore:{lateralScore:F2}, " +
        //     $"distToOwner:{distToOwner:F2}, optimalOwner:{optimalOwner:F2}, distanceError:{distanceError:F2}, distanceTolerance:{distanceTolerance:F2}, " +
        //     $"lateralAlign:{lateralAlign:F2}, forwardComponent:{forwardComponent:F2}, " +
        //     $"distanceOk:{distanceOk}, lateralOk:{lateralOk}, " +
        //     $"player:{playerPosition}, owner:{ballOwnerPosition})"
        // );
        if (pressureScore > 0.45f && distanceOk && lateralOk)  // 45%以上の総合スコアかつ両条件を満たす
        {
            // DebugLogger.Log($"守備位置判定: プレッシャー位置が適切 (pressureScore:{pressureScore:F2}, distanceScore:{pressureDistanceScore:F2}, lateralScore:{lateralScore:F2}, distToOwner:{distToOwner:F1}), playerPosition:{playerPosition}, ballOwnerPosition:{ballOwnerPosition}");
            isInGoodPosition = true;
        }
        
        // 2) マーク位置チェック（最も危険な敵への距離）
        if (!isInGoodPosition && enemyPositions != null && enemyPositions.Count > 0)
        {
            Vector3 bestEnemy = ballOwnerPosition;
            float bestEnemyScore = -1f;
            foreach (var e in enemyPositions)
            {
                float forwardness = Vector3.Dot((e - ballOwnerPosition).normalized, toGoal);
                if (forwardness > bestEnemyScore)
                {
                    bestEnemyScore = forwardness;
                    bestEnemy = e;
                }
            }
            float distToEnemy = Vector3.Distance(playerPosition, bestEnemy);
            float idealMarkDist = fieldLength * 0.10f;  // 10%
            float markingScore = Mathf.Clamp01(1f - Mathf.Abs(distToEnemy - idealMarkDist) / Mathf.Max(idealMarkDist, 0.01f));
            
            if (markingScore > 0.35f)  // 35%以上のスコアで適切なマーク位置
            {
                // DebugLogger.Log($"守備位置判定: マーク位置が適切 (markingScore:{markingScore:F1}), playerPosition:{playerPosition}, bestEnemy:{bestEnemy}, enemyGoalPosition:{enemyGoalPosition}");
                isInGoodPosition = true;
            }
            
            // 3) パスコース遮断位置チェック
            if (!isInGoodPosition)
            {
                Vector3 a = ballOwnerPosition;
                Vector3 b = bestEnemy;
                Vector3 p = playerPosition;
                Vector3 ab = b - a;
                float abLenSq = Mathf.Max(0.001f, Vector3.Dot(ab, ab));
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq);
                Vector3 closest = a + ab * t;
                float laneDist = Vector3.Distance(p, closest);
                float laneWidth = fieldLength * 0.06f;  // 6%
                float passBlockScore = Mathf.Clamp01(1f - laneDist / Mathf.Max(laneWidth, 0.01f));
                
                if (passBlockScore > 0.35f)  // 35%以上のスコアで適切なパス遮断位置
                {
                    // DebugLogger.Log($"守備位置判定: パス遮断位置が適切 (passBlockScore:{passBlockScore:F1}), playerPosition:{playerPosition}, bestEnemy:{bestEnemy}, enemyGoalPosition:{enemyGoalPosition}");
                    isInGoodPosition = true;
                }
            }
        }

        // 4) ゴールとボール保持者の間の守備位置チェック
        if (!isInGoodPosition)
        {
            // ゴールとボール保持者を結ぶ直線に最も近い位置か
            Vector3 a = ballOwnerPosition;
            Vector3 b = enemyGoalPosition;
            Vector3 p = playerPosition;
            Vector3 ab = b - a;
            float abLenSq = Mathf.Max(0.001f, Vector3.Dot(ab, ab));
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq);
            Vector3 closest = a + ab * t;
            float laneDist = Vector3.Distance(p, closest);
            float laneWidth = fieldLength * 0.08f;  // ゴール前の守備幅（8%）
            float blockScore = Mathf.Clamp01(1f - laneDist / Mathf.Max(laneWidth, 0.01f));
            
            if (blockScore > 0.35f)  // 35%以上のスコアで適切な位置とみなす
            {
                // DebugLogger.Log($"守備位置判定: ゴールとボール保持者の間の守備位置が適切 (blockScore:{blockScore:F1}), playerPosition:{playerPosition}, ballOwnerPosition:{ballOwnerPosition}, enemyGoalPosition:{enemyGoalPosition}");
                isInGoodPosition = true;
            }
        }
        
        return isInGoodPosition;
    }
} 