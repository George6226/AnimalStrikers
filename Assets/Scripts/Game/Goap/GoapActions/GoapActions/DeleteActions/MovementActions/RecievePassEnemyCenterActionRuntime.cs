using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

public class RecievePassEnemyCenterActionRuntime : GoapActionRuntime
{
    private PlayerBlackboard _playerBlackboard;
    private AnimalComponentManager _animalComponent;
    private Vector3 _targetPosition;
    private float _goodPositionRadius;
    private bool _isMoving = false;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="origin">アクションのScriptableObjectインスタンス</param>
    /// <param name="debugName">デバッグ用の名前</param>
    public RecievePassEnemyCenterActionRuntime(GoapActionSO origin, string debugName) : base(origin, debugName)
    {
        // ScriptableObjectを正しい型にキャスト
        var so = origin as RecievePassEnemyCenterActionSO;
        if (so != null)
        {
            // パス受け位置の有効半径を設定
            _goodPositionRadius = so.GoodPositionRadius;
        }
    }

    public override bool CanExecute(PlayerBlackboard bb)
    {
        // ここで追加の実行可否判定があれば記述
        return true;
    }

    public override void Execute(PlayerBlackboard bb)
    {
        _playerBlackboard = bb;
        _animalComponent = bb.GetComponent<AnimalComponentManager>();
        _isMoving = true;
        UpdateTargetPosition();
        DebugLogger.Log($"[{_debugName}(RecievePassEnemyCenterActionRuntime)] パス受け移動開始");
    }

    public override void Update(float deltaTime)
    {
        if (!_isMoving || _playerBlackboard == null) return;
        
        // 目標位置への移動の実行
        Vector3 currentPos = _playerBlackboard.PhysicalState.Position;
        Vector3 direction = (_targetPosition - currentPos);
        float distance = direction.magnitude;
        
        if (distance > _goodPositionRadius)
        {
            // AnimalHandlerのmoveメソッドを使用
            // per: 移動の強度（1.0fで最大速度）
            // speedMag: 速度倍率（1.0fで通常速度、2.0fでダッシュ速度）
            float per = 1.0f; // 最大速度で移動
            float speedMag = 1.0f; // 通常速度
            
            _animalComponent.Animal.move(per, speedMag);

            // キャラクターの向きを目標位置の方向に調整
            float radian = Mathf.Atan2(-direction.x, direction.z);
            _animalComponent.Animal.rotate(radian);

            DebugLogger.Log($"[{_debugName}(RecievePassEnemyCenterActionRuntime)] radian: {radian} direction: {direction} distance: {distance}");
            
            // アクション進行度を更新
            _playerBlackboard.ActionState.SetActionProgress(Mathf.Clamp01(1f - (distance / _goodPositionRadius)));
        }
        else
        {
            _animalComponent.Animal.stand();
            _isMoving = false;
            _playerBlackboard.ActionState.SetActionProgress(1.0f); // 完了
            DebugLogger.Log($"[{_debugName}(RecievePassEnemyCenterActionRuntime)] 良い位置に到達: {_targetPosition}");
        }
    }

    private void UpdateTargetPosition()
    {
        // フィールド情報の取得
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;
        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        float fieldLength = teamBB.FieldInfo.FieldLength;

        float halfWidth = fieldWidth * 0.5f;
        float halfLength = fieldLength * 0.5f;

        // 敵陣中央を狙う
        // フィールドの中央、敵陣側の中腹あたりに基準点を置く
        float targetX = fieldCenter.x;   // フィールド中央
        float targetZ = fieldCenter.z + halfLength * 0.5f;  // 敵陣側の中央あたり
        float targetY = _playerBlackboard != null ? _playerBlackboard.PhysicalState.Position.y : 0f;

        // フィールド境界内にクランプ
        targetX = Mathf.Clamp(targetX, fieldCenter.x - halfWidth, fieldCenter.x + halfWidth);
        targetZ = Mathf.Clamp(targetZ, fieldCenter.z - halfLength, fieldCenter.z + halfLength);

        _targetPosition = new Vector3(targetX, targetY, targetZ);
    }

    public override bool IsComplete()
    {
        return !_isMoving;
    }

    public override void Cancel()
    {
        _isMoving = false;
        if (_animalComponent != null) _animalComponent.Animal.stand();
        DebugLogger.Log($"[{_debugName}(RecievePassEnemyCenterActionRuntime)] パス受け移動キャンセル");
    }
} 