using UnityEngine;
using Game.Goap;
using System.Collections.Generic;

/// <summary>
/// GOAP推論用のWorkingMemoryを更新する責務を持つクラス。
/// PlayerBlackboardの状態およびTeamBlackboardの共有情報からFactを組み立てる。
/// </summary>
public class PlayerWorkingMemoryUpdater
{
    /// <summary>
    /// PlayerBlackboardの現在の状態からWorkingMemoryを更新する
    /// </summary>
    /// <param name="playerBB">更新対象のPlayerBlackboard</param>
    public void Update(PlayerBlackboard playerBB)
    {
        if (playerBB == null || playerBB._workingMemory == null)
        {
            return;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return;
        }
        var workingMemory = playerBB._workingMemory;

        // ローカル関数でFact設定を簡潔に記述
        void SetFact(Fact fact, bool value)
        {
            workingMemory.AssertFact(fact, value);
        }

        // bool条件を "true/false" の両キーで同期して null を防ぐ
        void SetBooleanFact(string tag, bool value)
        {
            SetFact(new Fact(tag, "true"), value);
            SetFact(new Fact(tag, "false"), !value);
        }

        /***** Basic情報 ******/
        // 自分がボールを所持しているか
        SetBooleanFact(SymbolTag.Basic.HAS_BALL, playerBB.BallState.HasBall);
        // 自分が動いているか?
        SetBooleanFact(SymbolTag.Basic.IS_MOVING, playerBB.PhysicalState.IsMoving);

        /***** Action情報 ******/
        // 移動可能か TODO:スタン状態でない
        SetBooleanFact(SymbolTag.Action.CAN_MOVE, true);
        // パスを受ける位置にいるか?
        SetBooleanFact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION,
            CalculateIsInPassReceivePosition(playerBB, teamBB));
        // 保持者とのサポート関係を維持しているか（味方ボール・非保持者向け）
        SetBooleanFact(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP,
            CalculateIsMaintainingSupportRelationship(playerBB, teamBB));
        // 守備位置にいるか?
        SetBooleanFact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION,
            CalculateIsInDefensivePosition(playerBB, teamBB));

        /***** Position情報 *****/
        SetBooleanFact(SymbolTag.Position.NEAR_BALL, playerBB.BallState.BallDistance < 3f);
        SetBooleanFact(SymbolTag.Position.NEAR_ENEMY_NO_BALL,
            IsNearEnemyNoBall(playerBB, teamBB));
        SetBooleanFact(SymbolTag.Position.NEAR_ENEMY_HAS_BALL,
            IsNearEnemyHasBall(playerBB, teamBB));

        /***** Tactical情報 *****/
        bool mirrored = GoapFieldNpcPerspective.IsMirrored(playerBB);
        SetBooleanFact(SymbolTag.Tactical.TEAM_HAS_BALL,
            GoapFieldNpcPerspective.EffectiveTeamHasBall(teamBB, mirrored));
        SetBooleanFact(SymbolTag.Tactical.ENEMY_HAS_BALL,
            GoapFieldNpcPerspective.EffectiveEnemyHasBall(teamBB, mirrored));

        bool myFieldNow = mirrored
            ? playerBB.PhysicalState.Position.z > teamBB.FieldInfo.FieldCenter.z
            : playerBB.PhysicalState.Position.z < teamBB.FieldInfo.FieldCenter.z;
        SetBooleanFact(SymbolTag.Position.MY_FIELD_NOW, myFieldNow);

        // トランジションはアクションのコスト/ゴール切替で表現するため、Factでは管理しない
    }

    /// <summary>
    /// ゴール条件のリストを受け取り、現在の状態との比較結果をDictionaryとして返す
    /// </summary>
    /// <param name="playerBB">対象プレイヤーのブラックボード</param>
    /// <param name="goalFacts">ゴール条件のリスト</param>
    /// <returns>ゴール条件と現在の状態の比較結果</returns>
    public Dictionary<GoapCondition, bool> GetGoalConditionStates(PlayerBlackboard playerBB, List<GoapCondition> goalFacts)
    {
        var result = new Dictionary<GoapCondition, bool>();

        if (playerBB == null || playerBB._workingMemory == null || goalFacts == null)
        {
            return result;
        }

        var workingMemory = playerBB._workingMemory;

        foreach (var goal in goalFacts)
        {
            // WorkingMemoryからゴール条件の現在の値を取得
            var factObj = new Fact(goal.Tag, goal.ExpectedValue.ToString().ToLower());
            var currentValue = workingMemory.GetFact(factObj);

            // 現在の値と期待値を比較
            bool isSatisfied = currentValue == goal.ExpectedValue;
            result[goal] = isSatisfied;
        }

        return result;
    }

    // ボールを持たない敵が近くにいるかチェック
    private bool IsNearEnemyNoBall(PlayerBlackboard playerBB, TeamBlackboard teamBB)
    {
        bool mirrored = GoapFieldNpcPerspective.IsMirrored(playerBB);
        GoapFieldNpcPerspective.ResolveTeamPositions(
            teamBB,
            mirrored,
            out _,
            out var opponentPositions);
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        return PlayerBlackboardCalculator.IsNearEnemyNoBall(
            playerBB.PhysicalState.Position,
            ballOwnerPosition,
            opponentPositions
        );
    }

    // ボールを持つ敵が近くにいるかチェック
    private bool IsNearEnemyHasBall(PlayerBlackboard playerBB, TeamBlackboard teamBB)
    {
        bool mirrored = GoapFieldNpcPerspective.IsMirrored(playerBB);
        return PlayerBlackboardCalculator.IsNearEnemyHasBall(
            playerBB.PhysicalState.Position,
            GoapFieldNpcPerspective.EffectiveEnemyHasBall(teamBB, mirrored),
            teamBB.BallInfo.BallOwnerPosition
        );
    }

    /// <summary>
    /// パス受信位置にいるかを計算
    /// </summary>
    /// <returns>パス受信位置にいるかどうか</returns>
    private bool CalculateIsInPassReceivePosition(PlayerBlackboard playerBB, TeamBlackboard teamBB)
    {
        return TeammateNpcSupportPlanning.EvaluatePassReceivePosition(playerBB, teamBB);
    }

    private static bool CalculateIsMaintainingSupportRelationship(PlayerBlackboard playerBB, TeamBlackboard teamBB)
    {
        return TeammateNpcSupportPlanning.EvaluateMaintainingSupportRelationship(playerBB, teamBB);
    }

    /// <summary>
    /// 守備位置にいるかを計算
    /// </summary>
    /// <returns>守備位置にいるかどうか</returns>
    private bool CalculateIsInDefensivePosition(PlayerBlackboard playerBB, TeamBlackboard teamBB)
    {
        bool mirrored = GoapFieldNpcPerspective.IsMirrored(playerBB);
        GoapFieldNpcPerspective.ResolveTeamPositions(
            teamBB,
            mirrored,
            out _,
            out var opponentPositions);
        // 味方 NPC は従来どおり EnemyGoalPosition 基準（守備バッチ #2/#3 との整合）。
        // 敵 NPC のみ鏡像の DefendGoal を使う。
        Vector3 referenceGoalPosition = mirrored
            ? GoapFieldNpcPerspective.GetDefendGoalPosition(teamBB, mirrored)
            : teamBB.FieldInfo.EnemyGoalPosition;
        bool teamHasBall = mirrored
            ? GoapFieldNpcPerspective.EffectiveTeamHasBall(teamBB, mirrored)
            : teamBB.BallInfo.TeamHasBall;
        return PlayerBlackboardCalculator.CalculateIsInDefensivePosition(
            teamHasBall,
            playerBB.BallState.HasBall,
            playerBB.ActionState.IsStunned,
            teamBB.FieldInfo.FieldLength,
            playerBB.PhysicalState.Position,
            teamBB.BallInfo.BallOwnerPosition,
            opponentPositions,
            referenceGoalPosition
        );
    }
}

