using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 裏抜けランを実行するアクションのランタイム実装
/// 敵の最終ラインを突破し、スルーパスや決定機を狙うための裏抜けランを実行する
/// </summary>
public class MakeRunBehindActionRuntime : GoapActionRuntime
{
    private const string DiagCategory = "RunBehind";

    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _executionTime;
    private float _runSpeed;
    private float _minRunDistanceRatio;
    private float _maxRunDistanceRatio;
    private float _runAngleTolerance;
    private float _timingWindow;
    private float _enemyLineDistanceRatio;
    private float _breakthroughThreshold;
    
    private PlayerBlackboard _playerBlackboard;
    private bool _motorResolved;
    private Vector3 _targetPosition;
    private bool _isWaitingForTiming = false;
    private float _timingStartTime;
    private RunDirection _selectedDirection;
    private float _moveIntensity = 1f;
    
    // ラン方向定義
    private enum RunDirection
    {
        Left,       // 左サイド
        Center,     // 中央
        Right,      // 右サイド
        RightDiagonal,  // 右斜め前
        LeftDiagonal    // 左斜め前
    }
    
    // コンストラクタ
    public MakeRunBehindActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName) 
    {
        var makeRunBehindSO = origin as MakeRunBehindActionSO;
        if (makeRunBehindSO != null)
        {
            _executionTime = makeRunBehindSO.ExecutionTime;
            _runSpeed = makeRunBehindSO.RunSpeed;
            _minRunDistanceRatio = makeRunBehindSO.MinRunDistanceRatio;
            _maxRunDistanceRatio = makeRunBehindSO.MaxRunDistanceRatio;
            _runAngleTolerance = makeRunBehindSO.RunAngleTolerance;
            _timingWindow = makeRunBehindSO.TimingWindow;
            _enemyLineDistanceRatio = makeRunBehindSO.EnemyLineDistanceRatio;
            _breakthroughThreshold = makeRunBehindSO.BreakthroughThreshold;
        }
    }
    
    // 実行可能かどうか
    public override bool CanExecute(PlayerBlackboard bb)
    {
        if (!TeammateNpcSupportPlanning.PassesTacticalAttackRuntimeGate(bb))
        {
            return false;
        }

        // ボールから適切な距離にいるかチェック
        float ballDistance = bb.BallState.BallDistance;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        float fieldLength = teamBB != null ? teamBB.FieldInfo.FieldLength : 40f;
        float minDistance = fieldLength * _minRunDistanceRatio;
        float maxDistance = fieldLength * _maxRunDistanceRatio;
        
        if (ballDistance < minDistance || ballDistance > maxDistance * 1.2f)
            return false;
        
        // 突破可能性が閾値以上かチェック
        float breakthroughPotential = CalculateBreakthroughPotential(bb);
        if (breakthroughPotential < _breakthroughThreshold * 0.5f)
            return false;

        return GoapTacticalMoveHelper.TryResolveMotor(bb);
    }
    
    // 実行開始
    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _motorResolved = GoapTacticalMoveHelper.TryResolveMotor(bb);
        _moveIntensity = Mathf.Clamp(_runSpeed / 6f, 0.6f, 1f);
        TryComputeBestRunBehindTarget(bb, out _targetPosition, out _selectedDirection);

        // タイミング待機を開始
        _isWaitingForTiming = true;
        _timingStartTime = Time.time;
        
        // 実行開始
        _isExecuting = true;
        _executionStartTime = Time.time;
        GoapMovementDiagnostic.Log(DiagCategory, $"Execute dir={_selectedDirection} target={GoapMovementDiagnostic.FormatVector(_targetPosition)}", bb);
    }
    
    // 更新処理
    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null) return;
        
        // タイミング待機中
        if (_isWaitingForTiming)
        {
            if (IsOptimalTiming())
            {
                _isWaitingForTiming = false;
                DebugLogger.Log($"[{_debugName}] タイミング良好、裏抜けラン開始: {_playerBlackboard.BasicData.Self.name}");
            }
            else if (Time.time - _timingStartTime > _timingWindow)
            {
                _isWaitingForTiming = false;
                DebugLogger.Log($"[{_debugName}] タイミングウィンドウ終了、強制実行: {_playerBlackboard.BasicData.Self.name}");
            }
            else
            {
                DebugLogger.Log($"[{_debugName}] タイミング待機中: {_playerBlackboard.BasicData.Self.name}");
                return;
            }
        }
        
        if (_motorResolved)
        {
            GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
        }
    }
    
    // 完了判定
    public override bool IsComplete()
    {
        if (!_isExecuting) return true;
        
        bool arrived = _motorResolved
            && GoapTacticalMoveHelper.MoveToward(_playerBlackboard, _targetPosition, _moveIntensity, DiagCategory, 0.5f);
        bool timedOut = Time.time - _executionStartTime >= _executionTime;
        if (!arrived && !timedOut) return false;

        Finish();
        return true;
    }
    
    // 中断処理
    public override void Cancel()
    {
        Finish();
    }

    private void Finish()
    {
        if (_playerBlackboard != null)
        {
            GoapTacticalMoveHelper.Stop(_playerBlackboard, DiagCategory);
            GoapTacticalMoveHelper.ApplyPassReceiveFact(_playerBlackboard);
        }

        _isExecuting = false;
        _isWaitingForTiming = false;
    }
    
    
    
    /// <summary>
    /// 最適なタイミングかチェック
    /// </summary>
    /// <returns>最適なタイミングかどうか</returns>
    private bool IsOptimalTiming()
    {
        // ボール保持者の状況をチェック
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return false;

        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        
        // ボール保持者に敵が近づいていないかチェック
        float minEnemyDistanceToBallOwner = float.MaxValue;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distance = Vector3.Distance(enemyPos, ballOwnerPosition);
            minEnemyDistanceToBallOwner = Mathf.Min(minEnemyDistanceToBallOwner, distance);
        }
        
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float pressureThreshold = fieldLength * 0.1f; // フィールド長の10%
        
        // ボール保持者がプレッシャーを受けていない場合
        if (minEnemyDistanceToBallOwner > pressureThreshold)
        {
            return true;
        }
        
        // 自分が敵から十分離れている場合
        float minEnemyDistanceToMe = float.MaxValue;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distance = Vector3.Distance(enemyPos, _playerBlackboard.PhysicalState.Position);
            minEnemyDistanceToMe = Mathf.Min(minEnemyDistanceToMe, distance);
        }
        
        if (minEnemyDistanceToMe > fieldLength * 0.15f) // フィールド長の15%
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// ターゲット位置を計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <param name="direction">選択された方向</param>
    /// <returns>ターゲット位置</returns>
    private Vector3 CalculateTargetPosition(PlayerBlackboard bb, RunDirection direction)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return bb.PhysicalState.Position;

        Vector3 myPosition = bb.PhysicalState.Position;
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoalPosition = teamBB.FieldInfo.EnemyGoalPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        
        Vector3 toGoal = (enemyGoalPosition - ballOwnerPosition).normalized;
        Vector3 rightDirection = Vector3.Cross(toGoal, Vector3.up).normalized;
        
        // 裏抜けのターゲット位置（自分の位置から計算）
        Vector3 targetPosition = myPosition;
        
        switch (direction)
        {
            case RunDirection.Left:
                targetPosition += rightDirection * (fieldWidth * 0.2f);
                break;
            case RunDirection.Center:
                // 中央はそのまま
                break;
            case RunDirection.Right:
                targetPosition -= rightDirection * (fieldWidth * 0.2f);
                break;
            case RunDirection.RightDiagonal:
                targetPosition -= rightDirection * (fieldWidth * 0.1f); // 右斜め前
                targetPosition += toGoal * (fieldLength * 0.1f);
                break;
            case RunDirection.LeftDiagonal:
                targetPosition += rightDirection * (fieldWidth * 0.1f); // 左斜め前
                targetPosition += toGoal * (fieldLength * 0.1f);
                break;
        }
        
        return targetPosition;
    }

    /// <summary>
    /// 裏抜けターゲットをディフェンスライン起点で算出
    /// </summary>
    private bool TryComputeBestRunBehindTarget(PlayerBlackboard bb, out Vector3 bestTarget, out RunDirection bestDirection)
    {
        bestTarget = bb.PhysicalState.Position;
        bestDirection = RunDirection.Center;
        
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return false;

        Vector3 myPosition = bb.PhysicalState.Position;
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoalPosition = teamBB.FieldInfo.EnemyGoalPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        
        // 1) ディフェンスライン（敵ゴールに最も近いDFの距離）
        float defenseLineToGoal = float.MaxValue;
        foreach (var e in enemies)
        {
            float d = Vector3.Distance(e, enemyGoalPosition);
            if (d < defenseLineToGoal) defenseLineToGoal = d;
        }
        if (defenseLineToGoal == float.MaxValue) return false;
        
        // 2) 裏（ラインよりさらにゴール側）の候補点を生成
        Vector3 toGoalFromBallOwner = (enemyGoalPosition - ballOwnerPosition).normalized;
        Vector3 rightDir = Vector3.Cross(toGoalFromBallOwner, Vector3.up).normalized;
        
        float behindMargin = fieldLength * 0.06f;   // ライン裏へ最低進入距離
        float depth = Mathf.Clamp(defenseLineToGoal - behindMargin, fieldLength * 0.05f, fieldLength * 0.9f);
        
        // ライン位置をボール保持者基準の軸上に投影して近似（ゴールからdepth離れた位置）
        Vector3 linePointOnAxis = enemyGoalPosition - toGoalFromBallOwner * defenseLineToGoal;
        
        // 候補: 中央/左右/斜め（左右2種）× 深さ2段
        float[] lateralScales = new float[] { 0f, 0.15f, -0.15f, 0.08f, -0.08f };
        float[] depthOffsets = new float[] { behindMargin, behindMargin * 2.0f };
        
        Vector3 bestPos = myPosition;
        float bestScore = float.MinValue;
        foreach (var dOff in depthOffsets)
        {
            Vector3 basePoint = linePointOnAxis - toGoalFromBallOwner * dOff; // さらにゴール側へ
            foreach (var lat in lateralScales)
            {
                Vector3 candidate = basePoint + rightDir * (fieldWidth * lat);
                // フィールド内にクランプ
                candidate.x = Mathf.Clamp(candidate.x, -fieldWidth * 0.5f, fieldWidth * 0.5f);
                candidate.z = Mathf.Clamp(candidate.z, -fieldLength * 0.5f, fieldLength * 0.5f);
                
                float score = EvaluateRunBehindCandidate(candidate, bb, toGoalFromBallOwner, enemies);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = candidate;
                }
            }
        }
        if (bestScore == float.MinValue) return false;
        
        bestTarget = bestPos;
        // 方向のラベル化（ログ用途）
        Vector3 lateral = Vector3.Project(bestTarget - myPosition, rightDir);
        if (lateral.magnitude < fieldWidth * 0.02f)
            bestDirection = RunDirection.Center;
        else
            bestDirection = Vector3.Dot(lateral, rightDir) > 0f ? RunDirection.LeftDiagonal : RunDirection.RightDiagonal;
        
        return true;
    }
    
    /// <summary>
    /// 裏抜け候補の評価
    /// </summary>
    private float EvaluateRunBehindCandidate(Vector3 candidate, PlayerBlackboard bb, Vector3 toGoalAxis, List<Vector3> enemies)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        float fieldLength = teamBB.FieldInfo.FieldLength;
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        
        float score = 0f;
        
        // 角度: ボール保持者→候補がゴール方向に近いほど良い
        Vector3 toCand = (candidate - ballOwnerPosition).normalized;
        float angle = Vector3.Angle(toGoalAxis, toCand); // 小さいほど良い
        float angleScore = Mathf.Clamp01(1f - angle / 90f); // 0〜90度をスケール
        score += angleScore * 2.0f;
        
        // 距離: ボール保持者から遠すぎず近すぎず（10%〜45%）
        float dist = Vector3.Distance(ballOwnerPosition, candidate);
        float idealMin = fieldLength * 0.10f;
        float idealMax = fieldLength * 0.45f;
        float distScore = 0f;
        if (dist >= idealMin && dist <= idealMax)
        {
            float mid = (idealMin + idealMax) * 0.5f;
            distScore = 1f - Mathf.Abs(dist - mid) / (idealMax - idealMin);
        }
        score += distScore * 1.5f;
        
        // 敵からの最小距離（安全性）
        float minEnemy = float.MaxValue;
        foreach (var e in enemies)
            minEnemy = Mathf.Min(minEnemy, Vector3.Distance(candidate, e));
        score += Mathf.Clamp01(minEnemy / (fieldLength * 0.2f)) * 1.5f;
        
        // パスコースの通りやすさ（ボール保持者→候補）
        if (PlayerBlackboardCalculator.IsPassRouteClear(candidate, ballOwnerPosition, enemies))
            score += 1.0f;
        else
            score -= 0.5f;
        
        return score;
    }
    
    /// <summary>
    /// 突破可能性を計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>突破可能性（0-1の範囲）</returns>
    private float CalculateBreakthroughPotential(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 myPosition = bb.PhysicalState.Position;
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoalPosition = teamBB.FieldInfo.EnemyGoalPosition;
        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        
        float score = 0f;
        
        // ボール保持者より敵ゴールに近い場合は有利
        float distanceToEnemyGoal = Vector3.Distance(myPosition, enemyGoalPosition);
        float ballOwnerDistanceToEnemyGoal = Vector3.Distance(ballOwnerPosition, enemyGoalPosition);
        
        if (distanceToEnemyGoal < ballOwnerDistanceToEnemyGoal)
        {
            score += 0.5f; // 敵ゴールに近い位置にいる
        }
        
        // 敵の守備選手との距離を考慮
        float minEnemyDistance = float.MaxValue;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distanceToEnemy = Vector3.Distance(myPosition, enemyPos);
            minEnemyDistance = Mathf.Min(minEnemyDistance, distanceToEnemy);
        }
        
        if (minEnemyDistance > 5f)
        {
            score += 0.5f; // 敵から十分離れている
        }
        else if (minEnemyDistance < 2f)
        {
            score -= 0.3f; // 敵が近すぎる
        }
        
        return Mathf.Clamp(score, 0f, 1f);
    }
    
}
