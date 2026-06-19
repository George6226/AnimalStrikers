using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// ボール保持者がプレッシャーを受けているときに、
/// ボールを守るための安全なパスサポート位置に「移動するだけ」のアクションのランタイム実装。
/// 受け取りや返しは行わず、到達したら完了する。
/// </summary>
public class ProtectPassSupportMoveActionRuntime : GoapActionRuntime
{
    private bool _isExecuting = false;
    private float _executionStartTime;
    private float _executionTime = 2.5f; // デフォルト（SO未設定時）
    private float _moveSpeed = 3.5f;      // デフォルト（SO未設定時）
    private float _supportDistanceRatio = 0.18f;  // ボール保持者基準の前後距離（フィールド長比）
    private float _lateralAdjustRatio = 0.10f;    // 横方向調整（フィールド幅比）
    private float _pressureThreshold = 0.5f;      // ボール保持者へのプレッシャー閾値（0-1）

    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    private AnimalHandler _animalHandler;
    private Vector3 _supportPosition;
    private bool _reached = false;

    // 将来的に専用SOがある場合に値を取得（無ければデフォルトを使用）
    public ProtectPassSupportMoveActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        var so = origin as ProtectPassSupportMoveActionSO; // 存在しない場合はnull
        if (so != null)
        {
            _executionTime = so.ExecutionTime;
            _moveSpeed = so.MoveSpeed;
            _supportDistanceRatio = so.SupportDistanceRatio;
            _lateralAdjustRatio = so.LateralAdjustRatio;
            _pressureThreshold = so.PressureThreshold;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        // チームがボールを保持
        if (bb.GetFact(new Fact(SymbolTag.Tactical.TEAM_HAS_BALL, "true")) != true)
            return false;

        // 自分はボールを持っていない
        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
            return false;

        // 移動可能
        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
            return false;

        // 既に受けやすい位置扱いなら不要
        if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
            return false;


        // ボール保持者が十分なプレッシャーを受けているか
        float ownerPressure = CalculateBallOwnerPressureLevel();  
        DebugLogger.Log($"[{_debugName}] ボール保持者プレッシャー: {ownerPressure} 閾値: {_pressureThreshold}");
        if (ownerPressure < _pressureThreshold)
            return false;

        return true;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        _animalHandler = _animalComponent != null ? _animalComponent.Animal : null;

        _supportPosition = CalculateSupportPosition(bb);
        _reached = false;

        _isExecuting = true;
        _executionStartTime = Time.time;

        DebugLogger.Log($"[{_debugName}] ProtectPassSupportMove 開始: {bb.BasicData.Self.name} 目標={_supportPosition}");
    }

    public override void Update(float deltaTime)
    {
        if (!_isExecuting || _playerBlackboard == null) return;

        bool reachedNow = MoveToPosition(_supportPosition, _moveSpeed, deltaTime);
        _reached = reachedNow;
        if (reachedNow)
        {
            _isExecuting = false;
            return;
        }

        float elapsed = Time.time - _executionStartTime;
        if (elapsed % 0.5f < deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / _executionTime);
            DebugLogger.Log($"[{_debugName}] 移動中: {progress:P0} - {_playerBlackboard.BasicData.Self.name}");
        }
    }

    public override bool IsComplete()
    {
        if (!_isExecuting) return true;
        float elapsed = Time.time - _executionStartTime;
        return _reached || elapsed >= _executionTime;
    }

    public override void Cancel()
    {
        _isExecuting = false;
        DebugLogger.Log($"[{_debugName}] ProtectPassSupportMove 中断: {_playerBlackboard?.BasicData.Self.name}");
    }

    /// <summary>
    /// ボール保持者へのプレッシャーレベル（0-1）
    /// </summary>
    private float CalculateBallOwnerPressureLevel()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        if (enemies.Count == 0) return 0f;

        float minDist = float.MaxValue;
        foreach (var e in enemies)
            minDist = Mathf.Min(minDist, Vector3.Distance(ownerPos, e));

        float fieldLength = teamBB.FieldInfo.FieldLength;
        float threshold = fieldLength * 0.20f; // 20%
        if (minDist <= threshold)
            return 1f - (minDist / threshold);
        return 0f;
    }

    /// <summary>
    /// 安全なサポート位置を算出。
    /// 前方と後方の両方を評価し、より安全でパスコースが通りやすい位置を選択。
    /// 敵の少ない側へ横方向に逃がして、パスコースの通りやすさも加味して微調整。
    /// </summary>
    private Vector3 CalculateSupportPosition(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return Vector3.zero;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoal = teamBB.FieldInfo.EnemyGoalPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;

        Vector3 toGoal = (enemyGoal - ownerPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;

        float forwardDist = fieldLength * _supportDistanceRatio;
        float lateralDist = fieldWidth * _lateralAdjustRatio;

        // 敵密度で左右判定（ボール保持者基準）
        float leftDensity = 0f, rightDensity = 0f;
        foreach (var e in teamBB.BasicInfo.EnemyPositions)
        {
            float dot = Vector3.Dot((e - ownerPos).normalized, right);
            if (dot > 0.1f) rightDensity += 1f;
            else if (dot < -0.1f) leftDensity += 1f;
        }

        DebugLogger.Log($"[{_debugName}] 左Density: {leftDensity}, 右Density: {rightDensity}");

        Vector3 lateral = (leftDensity < rightDensity) ? -right * lateralDist : right * lateralDist;
        DebugLogger.Log($"[{_debugName}] 横方向オフセット: {lateral}");

        // 前方と後方の候補位置を生成
        Vector3 forwardCandidate = ownerPos + toGoal * forwardDist + lateral;
        Vector3 backwardCandidate = ownerPos - toGoal * forwardDist + lateral;

        // パスコースの通りやすさが悪ければ横方向を増やす
        if (!PlayerBlackboardCalculator.IsPassRouteClear(forwardCandidate, ownerPos, teamBB.BasicInfo.EnemyPositions))
        {
            forwardCandidate += lateral * 0.6f;
            DebugLogger.Log($"[{_debugName}] 前方パスコースが悪い場合の候補位置: {forwardCandidate}");
        }

        if (!PlayerBlackboardCalculator.IsPassRouteClear(backwardCandidate, ownerPos, teamBB.BasicInfo.EnemyPositions))
        {
            backwardCandidate += lateral * 0.6f;
            DebugLogger.Log($"[{_debugName}] 後方パスコースが悪い場合の候補位置: {backwardCandidate}");
        }

        // フィールド内へクランプ
        forwardCandidate.x = Mathf.Clamp(forwardCandidate.x, -fieldWidth * 0.5f, fieldWidth * 0.5f);
        forwardCandidate.z = Mathf.Clamp(forwardCandidate.z, -fieldLength * 0.5f, fieldLength * 0.5f);
        backwardCandidate.x = Mathf.Clamp(backwardCandidate.x, -fieldWidth * 0.5f, fieldWidth * 0.5f);
        backwardCandidate.z = Mathf.Clamp(backwardCandidate.z, -fieldLength * 0.5f, fieldLength * 0.5f);

        // 前方と後方の候補を評価してより良い方を選択
        Vector3 currentPos = bb.PhysicalState.Position;
        float forwardScore = EvaluateSupportPosition(forwardCandidate, ownerPos, currentPos, true);
        float backwardScore = EvaluateSupportPosition(backwardCandidate, ownerPos, currentPos, false);

        DebugLogger.Log($"[{_debugName}] 前方スコア: {forwardScore}, 後方スコア: {backwardScore}");

        Vector3 bestCandidate = forwardScore >= backwardScore ? forwardCandidate : backwardCandidate;
        DebugLogger.Log($"[{_debugName}] 選択された候補位置: {bestCandidate} ({(forwardScore >= backwardScore ? "前方" : "後方")})");

        return bestCandidate;
    }

    /// <summary>
    /// サポート位置候補の評価
    /// </summary>
    private float EvaluateSupportPosition(Vector3 candidate, Vector3 ownerPos, Vector3 currentPos, bool isForward)
    {
        float score = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        float fieldLength = teamBB.FieldInfo.FieldLength;

        // パスコースの通りやすさ（重要度: 高）
        if (PlayerBlackboardCalculator.IsPassRouteClear(candidate, ownerPos, enemies))
        {
            score += 3.0f;
        }
        else
        {
            score -= 1.0f;
        }

        // 敵からの最小距離（安全性）
        float minEnemyDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(candidate, e);
            minEnemyDist = Mathf.Min(minEnemyDist, dist);
        }
        score += Mathf.Clamp01(minEnemyDist / (fieldLength * 0.2f)) * 2.0f;

        // 前方位置の場合は追加ボーナス（攻撃的）
        if (isForward)
        {
            score += 1.0f;
        }
        else
        {
            // 後方位置の場合は安全性を重視
            score += 0.5f;
        }

        // ボール保持者からの適切な距離（近すぎず遠すぎず）
        float distToOwner = Vector3.Distance(candidate, ownerPos);
        float idealMin = fieldLength * 0.08f;
        float idealMax = fieldLength * 0.25f;
        if (distToOwner >= idealMin && distToOwner <= idealMax)
        {
            score += 1.0f;
        }
        else if (distToOwner < idealMin)
        {
            score -= 0.5f; // 近すぎる
        }

        // 現在位置からの移動コスト（近いほどコストが低い）
        float distFromCurrent = Vector3.Distance(candidate, currentPos);
        float comfortableRange = fieldLength * 0.2f;
        float movementScore = 1f - Mathf.Clamp01(distFromCurrent / Mathf.Max(comfortableRange, 0.01f));
        score += movementScore * 1.5f;

        return score;
    }

    /// <summary>
    /// 指定位置へ移動（実際の移動はAnimal側に委譲予定）
    /// </summary>
    private bool MoveToPosition(Vector3 targetPosition, float speed, float deltaTime)
    {
        if (_animalComponent == null)
        {
            return false;
        }

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

        if (distance < 0.6f)
        {
            if (!_reached)
            {
                DebugLogger.Log($"[{_debugName}] 目標到達: {_playerBlackboard.BasicData.Self.name} (残距離={distance:F2})");
            }
            return true;
        }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(-toTarget.x, toTarget.z);
            _animalHandler.rotate(targetAngle);
        }

        float speedRatio = Mathf.Clamp(speed / Mathf.Max(0.01f, _moveSpeed), 0.5f, 1.5f);
        float moveIntensity = Mathf.Clamp01(speedRatio);
        if (moveIntensity <= 0.0f)
        {
            moveIntensity = 0.8f;
        }

        _animalHandler.move(moveIntensity, speedRatio);

        DebugLogger.Log($"[{_debugName}] サポート位置へ移動中: {currentPosition} -> {targetPosition} (距離={distance:F1}m, 速度={speed:F1})");
        return false;
    }
}

/// <summary>
/// 専用SO（存在しない場合に備えた軽量な参照用インターフェース）。
/// 本プロジェクトにSOを追加した場合、この定義は削除して実SOを使用してください。
/// </summary>
// 専用SOが追加されたため、暫定インターフェースは削除しました。


