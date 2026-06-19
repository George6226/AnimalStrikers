using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// フリーになるアクションのランタイム実装
/// ボール保持者から適切な距離と角度でフリーになり、パスオプションを提供する
/// </summary>
public class GetOpenActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "GetOpen";

    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _executionTime;
    private float _movementSpeed;
    private float _optimalDistanceRatio;
    private float _minDistanceRatio;
    private float _maxDistanceRatio;
    private float _angleTolerance;
    
    private PlayerBlackboard _playerBlackboard;
    private bool _motorResolved;
    private Vector3 _targetPosition;
    private float _moveIntensity = 1f;
    
    // コンストラクタ
    public GetOpenActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName) 
    {
        var getOpenSO = origin as GetOpenActionSO;
        if (getOpenSO != null)
        {
            _executionTime = getOpenSO.ExecutionTime;
            _movementSpeed = getOpenSO.MovementSpeed;
            _optimalDistanceRatio = getOpenSO.OptimalDistanceRatio;
            _minDistanceRatio = getOpenSO.MinDistanceRatio;
            _maxDistanceRatio = getOpenSO.MaxDistanceRatio;
            _angleTolerance = getOpenSO.AngleTolerance;
        }
    }
    
    // 実行可能かどうか
    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (!TeammateNpcSupportPlanning.PassesTacticalAttackRuntimeGate(bb))
        {
            return false;
        }

        return GoapTacticalMoveHelper.TryResolveMotor(bb);
    }
    
    // 実行開始
    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _motorResolved = GoapTacticalMoveHelper.TryResolveMotor(bb);
        _moveIntensity = Mathf.Clamp(_movementSpeed / 5f, 0.5f, 1f);
        _targetPosition = CalculateTargetPosition(bb);
        _isExecuting = true;
        _executionStartTime = Time.time;
        GoapMovementDiagnostic.Log(DiagCategory, $"Execute target={GoapMovementDiagnostic.FormatVector(_targetPosition)}", bb);
    }
    
    // 更新処理
    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null || !_motorResolved) return;
        GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
    }
    
    // 完了判定
    public override bool IsComplete()
    {
        if (!_isExecuting) return true;
        
        bool arrived = _motorResolved
            && GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
        bool timedOut = Time.time - _executionStartTime >= _executionTime;
        if (!arrived && !timedOut) return false;

        Finish(arrived);
        return true;
    }
    
    // 中断処理
    public override void Cancel()
    {
        Finish(false);
    }

    private void Finish(bool arrived)
    {
        if (_playerBlackboard != null)
        {
            GoapTacticalMoveHelper.Stop(_playerBlackboard, DiagCategory);
            GoapTacticalMoveHelper.ApplyPassReceiveFact(_playerBlackboard);
            GoapMovementDiagnostic.Log(
                DiagCategory,
                arrived
                    ? "Finish arrived=true (tactical complete)"
                    : "Finish arrived=false (timeout)",
                _playerBlackboard);
        }

        _isExecuting = false;
    }
    
    /// <summary>
    /// ターゲット位置を計算
    /// フィールドを格子状に探索して、敵味方が少なく、かつ近い位置を見つける
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>ターゲット位置</returns>
    private Vector3 CalculateTargetPosition(PlayerBlackboard bb)
    {
        Vector3 myPosition = bb.PhysicalState.Position;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return myPosition;

        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        
        // 格子状に分割する数（X軸3分割、Z軸6分割 = 18点）
        int gridCountX = 3;
        int gridCountZ = 6;
        float cellSizeX = fieldWidth / gridCountX;
        float cellSizeZ = fieldLength / gridCountZ;
        
        Vector3 bestPosition = myPosition; // デフォルトは現在位置
        float bestScore = float.MinValue;
        int evaluatedPositions = 0;
        
        // フィールドを格子状に探索（-1, -1, -1から1, 1, 1まで）
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -3; z <= 2; z++)
            {
                // グリッドの中心位置を計算（0,0を中心に）
                Vector3 cellPosition = new Vector3(
                    x * cellSizeX,
                    0f,
                    z * cellSizeZ
                );
                
                // この位置のスコアを評価
                float score = EvaluateGridPosition(cellPosition, bb);
                evaluatedPositions++;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = cellPosition;
                }
            }
        }
        
        DebugLogger.Log($"[{_debugName}] GetOpen格子探索完了: 評価位置数={evaluatedPositions}, 最適位置={bestPosition}, スコア={bestScore:F2}");
        
        return bestPosition;
    }
    
    /// <summary>
    /// グリッド位置の適切性を評価
    /// 敵味方が少なく、かつ近い位置ほど高スコア
    /// </summary>
    /// <param name="position">評価する位置</param>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>スコア（高いほど適切）</returns>
    private float EvaluateGridPosition(Vector3 position, PlayerBlackboard bb)
    {
        float score = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        
        // 1. 現在位置からの距離スコア（近いほど高スコア）
        float myDistance = Vector3.Distance(position, bb.PhysicalState.Position);
        float maxDistance = fieldLength; // フィールド長を最大距離とする
        float proximityScore = 1f - Mathf.Clamp01(myDistance / maxDistance);
        score += proximityScore * 3f; // 距離スコアに高い重み
        
        // 2. 敵が少ないほど高スコア
        int nearbyEnemyCount = 0;
        float checkRadius = fieldLength * 0.15f; // チェック範囲をフィールドの15%とする
        
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            float distance = Vector3.Distance(position, enemyPos);
            if (distance < checkRadius)
            {
                nearbyEnemyCount++;
            }
        }
        
        // 敵が近くにいないほど高スコア
        float enemyScore = 1f - (nearbyEnemyCount / 4f); // 最大4人の敵
        score += enemyScore * 2f;
        
        // 3. 味方が少ないほど高スコア（重複回避）
        int nearbyTeammateCount = 0;
        
        foreach (Vector3 teammatePos in teamBB.BasicInfo.TeammatePositions)
        {
            if (teammatePos == bb.PhysicalState.Position) continue; // 自分は除外
            
            float distance = Vector3.Distance(position, teammatePos);
            if (distance < checkRadius)
            {
                nearbyTeammateCount++;
            }
        }
        
        // 味方が近くにいないほど高スコア
        float teammateScore = 1f - (nearbyTeammateCount / 3f); // 最大3人の味方
        score += teammateScore * 1.5f;
        
        // 4. ボール保持者から近いほど高スコア（パス受けやすさ）
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        float ballDistance = Vector3.Distance(position, ballOwnerPosition);
        float optimalDistance = fieldLength * _optimalDistanceRatio;
        
        // 最適距離に近いほど高スコア
        float optimalDistanceScore = 1f - Mathf.Clamp01(Mathf.Abs(ballDistance - optimalDistance) / optimalDistance);
        score += optimalDistanceScore * 1f;

        // スコアのデバッグ表示
        DebugLogger.Log($"[{_debugName}] Position Score Breakdown:");
        DebugLogger.Log($"  - Proximity Score: {proximityScore * 3f:F2} (raw: {proximityScore:F2})");
        DebugLogger.Log($"  - Enemy Score: {enemyScore * 2f:F2} (raw: {enemyScore:F2}, enemies: {nearbyEnemyCount})");
        DebugLogger.Log($"  - Teammate Score: {teammateScore * 1.5f:F2} (raw: {teammateScore:F2}, teammates: {nearbyTeammateCount})");
        DebugLogger.Log($"  - Optimal Distance Score: {optimalDistanceScore:F2}");
        DebugLogger.Log($"  - Total Score: {score:F2}");
        
        return score;
    }
    
}
