using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 壁パスのためにボール保持者へ素早く接近し、短距離でのパス交換を行いやすい位置を確保するアクション。
/// 実際のパス受け渡しは別アクションに委ね、ここでは「適切な距離まで近づく」ことに専念する。
/// </summary>
public class WallPassActionRuntime : GoapActionRuntime
{
    private bool _isExecuting;
    private bool _hasReachedTarget;
    private float _executionStartTime;
    private float _executionTime;
    private float _moveSpeed;
    private float _desiredDistanceRatio;
    private float _maxDistanceRatio;
    private float _pressureThreshold;
    private float _angleTolerance;

    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    private AnimalHandler _animalHandler;
    private Vector3 _approachPosition;

    public WallPassActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        if (origin is WallPassActionSO wallPassSO)
        {
            _executionTime = wallPassSO.ExecutionTime;
            _moveSpeed = wallPassSO.ReturnSpeed;
            _desiredDistanceRatio = wallPassSO.WallPassDistanceRatio;
            _maxDistanceRatio = wallPassSO.MaxWallPassDistanceRatio;
            _angleTolerance = wallPassSO.AngleTolerance;
            _pressureThreshold = wallPassSO.PressureThreshold;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        DebugLogger.Log($"[WallPass][CanExecute] 実行可能かどうか: {bb.BasicData.Self.name}");
        // チームがボールを保持していない
        if (bb.GetFact(new Fact(SymbolTag.Tactical.TEAM_HAS_BALL, "true")) != true)
        {
            DebugLogger.Log("[WallPass][CanExecute] 実行不可: チームがボール保持していない");
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            DebugLogger.Log("[WallPass][CanExecute] 実行不可: 既に自分がボールを保持している");
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            DebugLogger.Log("[WallPass][CanExecute] 実行不可: 移動できない状態");
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
        {
            DebugLogger.Log("[WallPass][CanExecute] 実行不可: すでにパスを受ける位置にいる");
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            // return bb.PhysicalState.Position;
            return false;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float desiredDistance = Mathf.Max(fieldLength * _desiredDistanceRatio, fieldLength * 0.02f);
        float maxDistance = Mathf.Max(fieldLength * _maxDistanceRatio, desiredDistance);
        float distanceToOwner = Vector3.Distance(bb.PhysicalState.Position, ownerPos);

        if (distanceToOwner <= desiredDistance * 1.05f)
        {
            DebugLogger.Log($"[WallPass] 実行不可: すでに十分近い (距離: {distanceToOwner:F2}, 閾値: {desiredDistance * 1.05f:F2})");
            return false; // すでに十分近い
        }

        if (distanceToOwner > maxDistance * 1.2f)
        {
            DebugLogger.Log($"[WallPass] 実行不可: 離れすぎ (距離: {distanceToOwner:F2}, 閾値: {maxDistance * 1.2f:F2})");
            return false; // 離れすぎ
        }

        float pressure = CalculatePressureLevel();
        if (pressure < _pressureThreshold)
        {
            DebugLogger.Log($"[WallPass] 実行不可: プレッシャー不足 (pressure: {pressure:F2}, 閾値: {_pressureThreshold:F2})");
            return false; // ボール保持者へのプレッシャーが低い
        }

        return true;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        _animalHandler = _animalComponent != null ? _animalComponent.Animal : null;

        _approachPosition = CalculateApproachPosition(bb);
        _hasReachedTarget = false;
        _isExecuting = true;
        _executionStartTime = Time.time;

        DebugLogger.Log($"[{_debugName}] WallPassアプローチ開始: {bb.BasicData.Self.name} -> 目標 {_approachPosition}");
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null) return;

        bool reachedNow = MoveToPosition(_approachPosition, _moveSpeed, deltaTime);
        if (reachedNow)
        {
            _hasReachedTarget = true;
            _isExecuting = false;
            DebugLogger.Log($"[{_debugName}] 壁パス距離に到達: {_playerBlackboard.BasicData.Self.name}");
            return;
        }

        float elapsed = Time.time - _executionStartTime;
        if (elapsed % 0.5f < deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(_executionTime, 0.01f));
            DebugLogger.Log($"[{_debugName}] 接近中: {progress:P0} - {_playerBlackboard.BasicData.Self.name}");
        }
    }

    public override bool IsComplete()
    {
        if (!_isExecuting) return true;

        float elapsed = Time.time - _executionStartTime;
        return _hasReachedTarget || elapsed >= _executionTime;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        DebugLogger.Log($"[{_debugName}] WallPassアプローチ中断: {_playerBlackboard?.BasicData.Self.name}");
    }

    private Vector3 CalculateApproachPosition(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return Vector3.zero;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoal = teamBB.FieldInfo.EnemyGoalPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        float desiredDistance = Mathf.Clamp(fieldLength * _desiredDistanceRatio, fieldLength * 0.03f, fieldLength * _maxDistanceRatio);

        Vector3 currentPos = bb.PhysicalState.Position;
        Vector3 toGoal = (enemyGoal - ownerPos).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, toGoal).normalized;

        List<Vector3> directions = new List<Vector3>();

        Vector3 ownerToPlayer = (currentPos - ownerPos);
        if (ownerToPlayer.sqrMagnitude > 0.01f)
            directions.Add(ownerToPlayer.normalized);

        directions.Add(toGoal);
        directions.Add(Quaternion.AngleAxis(_angleTolerance * 0.5f, Vector3.up) * toGoal);
        directions.Add(Quaternion.AngleAxis(-_angleTolerance * 0.5f, Vector3.up) * toGoal);
        directions.Add(lateral);
        directions.Add(-lateral);

        // 各アプローチ候補の位置（ワールド座標）・詳細をデバッグ表示
        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 dir = directions[i].normalized;
            Vector3 candidatePos = ownerPos + dir * desiredDistance;
            candidatePos.x = Mathf.Clamp(candidatePos.x, -fieldWidth * 0.5f, fieldWidth * 0.5f);
            candidatePos.z = Mathf.Clamp(candidatePos.z, -fieldLength * 0.5f, fieldLength * 0.5f);

            string placeHint = "";
            if (Vector3.Angle(dir, toGoal) < 1f)
            {
                placeHint = "ゴールへ";
            }
            else if (Vector3.Angle(dir, -toGoal) < 1f)
            {
                placeHint = "自陣へ";
            }
            else if (Vector3.Angle(dir, lateral) < 5f)
            {
                placeHint = "右サイド";
            }
            else if (Vector3.Angle(dir, -lateral) < 5f)
            {
                placeHint = "左サイド";
            }
            else if (i == 0)
            {
                placeHint = "現ポジション方向";
            }
            else if (Mathf.Abs(Vector3.Angle(dir, Quaternion.AngleAxis(_angleTolerance * 0.5f, Vector3.up) * toGoal)) < 2f)
            {
                placeHint = $"ゴール右{_angleTolerance * 0.5f}度";
            }
            else if (Mathf.Abs(Vector3.Angle(dir, Quaternion.AngleAxis(-_angleTolerance * 0.5f, Vector3.up) * toGoal)) < 2f)
            {
                placeHint = $"ゴール左{_angleTolerance * 0.5f}度";
            }

            string detail = $"dir: {dir}, ownerPos: {ownerPos}, targetDist: {desiredDistance:F2}, fieldW:{fieldWidth}, fieldL:{fieldLength}";
            DebugLogger.Log($"[{_debugName}] 候補[{i}]: {candidatePos} ({placeHint}) ({detail})");
        }

        Vector3 bestCandidate = ownerPos + toGoal * desiredDistance;
        float bestScore = float.MinValue;

        foreach (var dir in directions)
        {
            if (dir.sqrMagnitude < 0.001f) continue;

            Vector3 candidate = ownerPos + dir.normalized * desiredDistance;
            candidate.x = Mathf.Clamp(candidate.x, -fieldWidth * 0.5f, fieldWidth * 0.5f);
            candidate.z = Mathf.Clamp(candidate.z, -fieldLength * 0.5f, fieldLength * 0.5f);

            float score = EvaluateApproachPosition(candidate, ownerPos, currentPos, desiredDistance, toGoal);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        DebugLogger.Log($"[{_debugName}] アプローチ候補選定: {bestCandidate} (score={bestScore:F2})");
        return bestCandidate;
    }

    private float EvaluateApproachPosition(Vector3 candidate, Vector3 ownerPos, Vector3 currentPos, float desiredDistance, Vector3 toGoal)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float score = 0f;

        if (PlayerBlackboardCalculator.IsPassRouteClear(candidate, ownerPos, enemies))
            score += 3.0f;
        else
            score -= 1.5f;

        float distToOwner = Vector3.Distance(candidate, ownerPos);
        float distanceScore = 1f - Mathf.Clamp01(Mathf.Abs(distToOwner - desiredDistance) / Mathf.Max(desiredDistance, 0.01f));
        score += distanceScore * 2.0f;

        float minEnemyDist = float.MaxValue;
        foreach (var enemy in enemies)
            minEnemyDist = Mathf.Min(minEnemyDist, Vector3.Distance(candidate, enemy));
        score += Mathf.Clamp01(minEnemyDist / (fieldLength * 0.2f)) * 1.5f;

        float moveDistance = Vector3.Distance(currentPos, candidate);
        float maxMove = fieldLength * Mathf.Max(_maxDistanceRatio, 0.1f);
        float movementScore = 1f - Mathf.Clamp01(moveDistance / Mathf.Max(maxMove, 0.01f));
        score += movementScore * 1.5f;

        Vector3 dir = (candidate - ownerPos).normalized;
        float angle = Vector3.Angle(dir, toGoal);
        float angleScore = 1f - Mathf.Clamp01(angle / Mathf.Max(_angleTolerance, 1f));
        score += angleScore;

        return score;
    }

    private float CalculatePressureLevel()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        if (enemies.Count == 0) return 0f;

        float minDist = float.MaxValue;
        foreach (var enemy in enemies)
            minDist = Mathf.Min(minDist, Vector3.Distance(ownerPos, enemy));

        float fieldLength = teamBB.FieldInfo.FieldLength;
        float threshold = fieldLength * 0.20f;
        if (minDist <= threshold)
            return 1f - (minDist / threshold);
        return 0f;
    }

    private bool MoveToPosition(Vector3 targetPosition, float speed, float deltaTime)
    {
        if (_animalComponent == null)
            return false;

        if (_animalHandler == null)
        {
            _animalHandler = _animalComponent.Animal;
            if (_animalHandler == null)
            {
                DebugLogger.Log($"[{_debugName}] AnimalHandler 未設定: {_playerBlackboard.BasicData.Self.name}");
                return false;
            }
        }

        Vector3 currentPosition = _playerBlackboard.PhysicalState.Position;
        Vector3 toTarget = targetPosition - currentPosition;
        float distance = toTarget.magnitude;

        if (distance < 0.5f)
        {
            return true;
        }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(-toTarget.x, toTarget.z);
            _animalHandler.rotate(targetAngle);
        }

        float baseSpeed = Mathf.Max(_moveSpeed, 0.01f);
        float speedRatio = Mathf.Clamp(speed / baseSpeed, 0.5f, 1.5f);
        float moveIntensity = Mathf.Clamp01(distance / (distance + 1f));

        _animalHandler.move(Mathf.Max(0.7f, moveIntensity), speedRatio);
        DebugLogger.Log($"[{_debugName}] 接近移動: {currentPosition} -> {targetPosition} (距離={distance:F2}m)");

        return false;
    }
}
