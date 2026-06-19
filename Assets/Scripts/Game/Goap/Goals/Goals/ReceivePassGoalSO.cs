using System.Collections.Generic;
using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// パスを受けるという基本的なゴール
    /// 他のアクションと組み合わせて使用できる再利用可能なゴール
    /// </summary>
    [CreateAssetMenu(fileName = "ReceivePassGoalSO", menuName = "GOAP/Goals/ReceivePassGoalSO")]
    public class ReceivePassGoalSO : GoapGoalSO
    {
        [Header("Receive Pass Settings")]
        [SerializeField] private float _basePriority = 50f;
        [SerializeField] private float _teamHasBallBonus = 30f;
        [SerializeField] private float _offensiveModeBonus = 20f;
        [SerializeField] private float _optimalDistanceBonus = 25f;
        [SerializeField] private float _pressureBonus = 15f;
        [SerializeField] private float _tooClosePenalty = -40f;
        [SerializeField] private float _tooFarPenalty = -30f;
        [SerializeField] private float _enemyNearbyPenalty = -25f;
        [SerializeField] private float _alreadyInPositionPenalty = -50f;

        [Header("Distance Settings (Field-size relative)")]
        [SerializeField] private float _optimalDistanceRatio = 0.25f;  // 25% of field length
        [SerializeField] private float _minDistanceRatio = 0.1f;      // 10% of field length
        [SerializeField] private float _maxDistanceRatio = 0.4f;      // 40% of field length
        [SerializeField] private float _enemyDangerDistanceRatio = 0.05f; // 5% of field length

        // GoalNameは基底クラスで自動設定されるため、ここでは設定しない
        // RequiredFactsは基底クラスのフィールドを直接操作する

        /// <summary>
        /// ScriptableObject作成時の初期化
        /// </summary>
        protected override void OnEnable()
        {
            // 基底クラスの初期化を呼び出し
            base.OnEnable();
            
            // RequiredFactsを設定
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),
                new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
          // 基本優先度(ゴール条件も含めた値にする)
            float priority = _basePriority;

            // TODO:オフェンスモード = 積極攻撃の場合のボーナス
          //   if (bb.GetFact(new Fact(SymbolTag.Tactical.OFFENSIVE_MODE, "true")) == true)
          //   {
          //       priority += _offensiveModeBonus;
          //   }

            // ボールとの距離による調整
            float ballDistance = bb.BallState.BallDistance;
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null) return Mathf.Max(0f, priority);
            float fieldLength = teamBB.FieldInfo.FieldLength;
            
            float optimalDistance = fieldLength * _optimalDistanceRatio;
            float minDistance = fieldLength * _minDistanceRatio;
            float maxDistance = fieldLength * _maxDistanceRatio;
            float enemyDangerDistance = fieldLength * _enemyDangerDistanceRatio;

            // 最適距離に近い場合のボーナス
            float distanceScore = 1f - Mathf.Abs(ballDistance - optimalDistance) / optimalDistance;
            if (distanceScore > 0.8f)
            {
                priority += _optimalDistanceBonus * distanceScore;
            }

            // 距離によるペナルティ
            if (ballDistance < minDistance)
            {
                priority += _tooClosePenalty;
            }
            else if (ballDistance > maxDistance)
            {
                priority += _tooFarPenalty;
            }

            // 敵が近くにいる場合のペナルティ（SymbolTagを使用）
            if (bb.GetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_NO_BALL, "true")) == true)
            {
                priority += _enemyNearbyPenalty;
            }

            // // ボール保持者へのプレッシャーが高い場合のボーナス（パスを受ける必要性が高い）
            // if (bb.GetFact(new Fact(SymbolTag.Tactical.PRESSURE_HIGH, "true")) == true)
            // {
            //     priority += _pressureBonus;
            // }

            // 既にパスを受ける位置にいる場合のペナルティ
            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
            {
                priority += _alreadyInPositionPenalty;
            }

            return Mathf.Max(0f, priority);
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            // 基本的な前提条件をチェック
            if (bb.GetFact(new Fact(SymbolTag.Tactical.TEAM_HAS_BALL, "true")) != true)
                return false;

            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
                return false;

            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
                return false;

            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
                return false;

            // ボールとの距離が適切な範囲内かチェック
            float ballDistance = bb.BallState.BallDistance;
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null) return false;
            float fieldLength = teamBB.FieldInfo.FieldLength;
            
            float minDistance = fieldLength * _minDistanceRatio;
            float maxDistance = fieldLength * _maxDistanceRatio;

            if (ballDistance < minDistance || ballDistance > maxDistance)
                return false;

            return true;
        }

        public override string GetGoalDescription()
        {
            return "パスを受ける位置に移動して、ボールを受け取る準備をする";
        }

        public override string GetGoalCategory()
        {
            return "Basic";
        }
    }
}
