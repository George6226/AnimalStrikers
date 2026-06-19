using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

// ゴールを守るアクションのランタイム
public class DefendGoalActionRuntime : GoapActionRuntime
{
    private bool _isDefending = false;
    private float _defenseStartTime;
    private float _defenseRadius;
    private float _interceptRange;
    private float _tackleRange;
    private float _markDistance;
    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    private Vector3 _defenseTargetPosition;
    private GameObject _targetEnemy;
    
    public DefendGoalActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName) 
    {
        var defendSO = origin as DefendGoalActionSO;
        if (defendSO != null)
        {
            // ScriptableObjectから設定値を取得
            _defenseRadius = defendSO.DefenseRadius;
            _interceptRange = defendSO.InterceptRange;
            _tackleRange = defendSO.TackleRange;
            _markDistance = defendSO.MarkDistance;
        }
    }
    
    public override bool CanExecute(PlayerBlackboard bb)
    {
        // 基本的な実行可能条件
        if (bb.BallState.HasBall) return false; // ボールを持っている場合は守備しない
        
        // 敵がボールを持っているか、ボールがフリー状態で敵陣に近い場合
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        bool enemyHasBall = teamBB != null && teamBB.BallInfo.EnemyHasBall;
        bool ballNearEnemyGoal = teamBB != null && IsBallNearEnemyGoal(teamBB);
        
        return enemyHasBall || ballNearEnemyGoal;
    }
    
    public override void Execute(PlayerBlackboard bb)
    {
        // PlayerBlackboardを保存
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        
        // 守備開始
        _isDefending = true;
        _defenseStartTime = Time.time;

        // 効果を適用
        // _originSO.ApplyEffects(bb);
        
        DebugLogger.Log($"[{_debugName}(DefendGoalActionRuntime)] 守備開始: {bb.BasicData.Self.name}");
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isDefending || _playerBlackboard == null) return;
        
        // 守備戦略を決定
        DetermineDefenseStrategy();
        
        // 守備行動を実行
        ExecuteDefenseAction();
    }
    
    private void DetermineDefenseStrategy()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;

        Vector3 ballPosition = teamBB.BallInfo.BallPosition;
        Vector3 ownGoalPosition = teamBB.FieldInfo.OwnGoalPosition;
        Vector3 currentPosition = _playerBlackboard.PhysicalState.Position;
        
        // 1. ボールが近くにある場合：インターセプトを試行
        float distanceToBall = Vector3.Distance(currentPosition, ballPosition);
        if (distanceToBall <= _interceptRange && teamBB.BallInfo.BallFree)
        {
            _defenseTargetPosition = ballPosition;
            return;
        }
        
        // 2. 敵がボールを持っている場合：その敵をマーク
        if (teamBB.BallInfo.EnemyHasBall)
        {
            _targetEnemy = FindBallHolderEnemy();
            if (_targetEnemy != null)
            {
                Vector3 enemyPos = _targetEnemy.transform.position;
                _defenseTargetPosition = CalculateMarkPosition(enemyPos, ownGoalPosition);
                return;
            }
        }
        
        // 3. デフォルト：ボールとゴールの間の位置に移動
        _defenseTargetPosition = CalculateDefensePosition(ballPosition, ownGoalPosition);
    }
    
    private void ExecuteDefenseAction()
    {
        Vector3 currentPos = _playerBlackboard.PhysicalState.Position;
        Vector3 direction = (_defenseTargetPosition - currentPos).normalized;
        
        // 移動の実行
        float per = 1.0f; // 最大速度で移動
        float speedMag = 1.0f; // 通常速度
        
        _animalComponent.Animal.move(per, speedMag);
        
        // キャラクターの向きを調整
        float radian = Mathf.Atan2(-direction.x, direction.z);
        _animalComponent.Animal.rotate(radian);
        
        // タックル判定
        if (ShouldAttemptTackle())
        {
            AttemptTackle();
        }
        
        // アクション進行度を更新
        float distanceToTarget = Vector3.Distance(currentPos, _defenseTargetPosition);
        _playerBlackboard.ActionState.SetActionProgress(Mathf.Clamp01(1f - (distanceToTarget / _defenseRadius)));
    }
    
    private Vector3 CalculateDefensePosition(Vector3 ballPosition, Vector3 goalPosition)
    {
        // ボールとゴールの中間点を計算
        Vector3 midPoint = (ballPosition + goalPosition) * 0.5f;
        
        // ゴールに近すぎないように調整
        float distanceFromGoal = Vector3.Distance(midPoint, goalPosition);
        if (distanceFromGoal < 3f)
        {
            Vector3 directionFromGoal = (midPoint - goalPosition).normalized;
            midPoint = goalPosition + directionFromGoal * 3f;
        }
        
        return midPoint;
    }
    
    private Vector3 CalculateMarkPosition(Vector3 enemyPosition, Vector3 goalPosition)
    {
        // 敵の位置からゴールへの方向ベクトルを計算
        Vector3 toGoal = (goalPosition - enemyPosition).normalized;
        
        // 敵の位置から横方向のベクトルを計算（90度回転）
        Vector3 perpendicular = new Vector3(-toGoal.z, 0, toGoal.x);
        
        // マーク位置を計算（敵の横でゴール側）
        Vector3 markPos = enemyPosition + 
            (perpendicular * _markDistance * 0.5f) + // 横方向のオフセット
            (toGoal * _markDistance * 0.5f);        // ゴール側へのオフセット
        
        return markPos;
    }
    
    private GameObject FindBallHolderEnemy()
    {
        // 実際の実装では、TeamBlackboardからボールを持っている敵を取得
        // ここでは簡易的な実装
        var teamReg = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamReg == null) return null;

        var enemies = teamReg.Enemies;
        foreach (var enemyFacade in enemies)
        {
            if (enemyFacade == null) continue;
            var bb = enemyFacade.GetComponent<PlayerBlackboard>();
            if (bb != null && bb.BallState.HasBall)
            {
                return enemyFacade.gameObject;
            }
        }
        return null;
    }
    
    private bool ShouldAttemptTackle()
    {
        if (_targetEnemy == null) return false;
        
        float distanceToEnemy = Vector3.Distance(_playerBlackboard.PhysicalState.Position, _targetEnemy.transform.position);
        return distanceToEnemy <= _tackleRange;
    }
    
    private void AttemptTackle()
    {
        // タックルアクションの実行
        DebugLogger.Log($"[{_debugName}(DefendGoalActionRuntime)] タックル実行: {_playerBlackboard.BasicData.Self.name}");
        
        // ここでタックルアニメーションやボール奪取ロジックを実行
        // 実際の実装では、AnimalComponentManagerのタックル機能を使用
    }
    
    private bool IsBallNearEnemyGoal(TeamBlackboard teamBB)
    {
        Vector3 ballPosition = teamBB.BallInfo.BallPosition;
        Vector3 ownGoalPosition = teamBB.FieldInfo.OwnGoalPosition;
        float distanceToGoal = Vector3.Distance(ballPosition, ownGoalPosition);
        
        return distanceToGoal <= _defenseRadius;
    }
    
    public override bool IsComplete()
    {
        if (!_isDefending) return true;
        
        // 完了条件
        bool isComplete = false;
        
        // 1. ボールを獲得した
        if (_playerBlackboard.BallState.HasBall)
        {
            isComplete = true;
            DebugLogger.Log($"[{_debugName}(DefendGoalActionRuntime)] ボール奪取成功: {_playerBlackboard.BasicData.Self.name}");
        }
        
        // 2. 守備が長時間続いている
        float elapsedTime = Time.time - _defenseStartTime;
        if (elapsedTime > 15f)
        {
            isComplete = true;
        }
        
        // 3. 守備が不要になった
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null && !teamBB.BallInfo.EnemyHasBall && !IsBallNearEnemyGoal(teamBB))
        {
            isComplete = true;
        }
        
        if (isComplete)
        {
            _isDefending = false;
            _animalComponent.Animal.stand();
            DebugLogger.Log($"[{_debugName}(DefendGoalActionRuntime)] 守備終了: {_playerBlackboard.BasicData.Self.name}");
        }
        
        return isComplete;
    }
    
    public override void Cancel()
    {
        _isDefending = false;
        if (_animalComponent != null)
        {
            _animalComponent.Animal.stand();
        }
        DebugLogger.Log($"[{_debugName}(DefendGoalActionRuntime)] 守備キャンセル");
    }
} 