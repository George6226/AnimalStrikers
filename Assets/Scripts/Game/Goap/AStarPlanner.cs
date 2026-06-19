using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Game.Goap;

// プラナーのA*探索
// DebugLoggerクラスが同じ名前空間か、またはusingで参照できるようにする
public class AStarPlanner
{
    private string _agentName;
    private List<GoapActionSO> _availableActions;

    public AStarPlanner(string agentName = "")
    {
        _agentName = agentName;
    }

    /// <summary>
    /// プランの生成（後方連鎖方式）
    /// </summary>
    /// <param name="actions">アクションリスト</param>
    /// <param name="playerBlackboard">プレイヤーブラックボード</param>
    /// <param name="goalFacts">ゴールFactリスト</param>
    /// <returns>複数のプランのリスト</returns>
    public List<Queue<GoapActionSO>> Plan(
        List<GoapActionSO> actions,
        PlayerBlackboard playerBlackboard,
        List<GoapCondition> goalFacts)
    {
        // 利用可能なアクションを設定
        _availableActions = actions;
        
        // 後方連鎖でプランを生成
        var plans = BackwardChainPlans(goalFacts, playerBlackboard, new Queue<GoapActionSO>(), 0);

        // plansがnullの場合のチェック
        if (plans == null)
        {
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] 警告: プラン生成に失敗しました。plansがnullです。");
            return new List<Queue<GoapActionSO>>();
        }

        // プランリストを表示
        var planStringsB = plans.Select(plan => string.Join("->", plan.Select(a => a.name)));
        DebugLogger.Log($"[{_agentName}(AStarPlanner)] プラン削除前: {string.Join(", ", planStringsB)}");

        // プランリストの重複を削除する(A->BとB->Aは同じプラン)
        var uniquePlans = new HashSet<string>();
        plans = plans
            .Where(IsValidPlan)
            .Where(plan =>
            {
                var key = string.Join(",", plan.Select(a => a.ActionName).OrderBy(n => n));
                if (uniquePlans.Contains(key))
                {
                    return false;
                }

                uniquePlans.Add(key);
                return true;
            })
            .ToList();

        // プランリストを表示
        var planStringsA = plans.Select(plan => string.Join("->", plan.Select(a => a.ActionName)));
        DebugLogger.Log($"[{_agentName}(AStarPlanner)] プラン削除後: {string.Join(", ", planStringsA)}");
        
        return plans;
    }

    private List<Queue<GoapActionSO>> BackwardChainPlans(
        List<GoapCondition> goalFacts, 
        PlayerBlackboard playerBlackboard, 
        Queue<GoapActionSO> currentPlan,
        int actionIndex = 0)
    {
        DebugLogger.Log($"[{_agentName}(AStarPlanner)] 開始前ゴール条件: {string.Join(" , ", goalFacts.Select(g => $"{g.Tag}={g.ExpectedValue}"))}");
        DebugLogger.Log($"[{_agentName}(AStarPlanner)] 開始前現在のプラン: {string.Join(" -> ", currentPlan.Select(a => a.name))}");
        DebugLogger.Log($"[{_agentName}(AStarPlanner)] 開始前アクションインデックス: {actionIndex}");

        var allPlans = new List<Queue<GoapActionSO>>();

        // 全てのゴール条件を満たしているか?
        if (GoalSatisfied(playerBlackboard, goalFacts))
        {
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] ゴール条件を満たしたプラン: {string.Join(" -> ", currentPlan.Select(a => a.name))}");
            allPlans.Add(currentPlan);
            DebugLogger.Log($"-- 前の階層へ(条件を満たす) -- ({actionIndex} -> {actionIndex - 1})");
            return allPlans;
        }

        // 条件を満たしているアクションを探す(少なくとも1つのゴール条件を満たす)(PC)
        var actions = FindActionsForSomeGoals(goalFacts, playerBlackboard, currentPlan);
        if (actions.Count == 0){
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] 条件を満たしているアクションが見つかりません");
            DebugLogger.Log($"-- 前の階層へ(アクションなし) -- ({actionIndex} -> {actionIndex - 1})");
            return null;
        }

        // 複数のアクションを試行して複数のプランを生成(PC)
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] ゴール条件を変更前: {string.Join(" , ", goalFacts.Select(g => $"{g.Tag}={g.ExpectedValue}"))}");
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] アクション '{action.name}' の効果: {string.Join(" , ", action.Effects.Select(e => $"{e.Tag}={e.ExpectedValue}"))}");
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] アクション '{action.name}' の前提条件: {string.Join(" , ", action.Preconditions.Select(p => $"{p.Tag}={p.ExpectedValue}"))}");
            // ゴール条件を変更(満たした条件を削除し、前提の条件を追加)
            var newGoalFacts = UpdateGoalFacts(goalFacts, action);
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] ゴール条件を変更後: {string.Join(" , ", newGoalFacts.Select(g => $"{g.Tag}={g.ExpectedValue}"))}");

            // 条件を満たしているアクションをプランに追加 Queue[PA, PB]
            var newPlan = new Queue<GoapActionSO>();
            newPlan.Enqueue(action);
            foreach(var existingAction in currentPlan)
            {
                newPlan.Enqueue(existingAction);
            }
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] 新しいプランを作成: {string.Join(" -> ", newPlan.Select(a => a.name))}");

            DebugLogger.Log($"--- 次の階層へ ({actionIndex} -> {actionIndex + 1}) ---");
            // 次の階層へ再帰
            var newPlans = BackwardChainPlans(newGoalFacts, playerBlackboard, newPlan, actionIndex + 1);
            // 子プランが成功した場合、各子プランにアクションを後方に追加
            if (newPlans != null && newPlans.Count > 0){
                DebugLogger.Log($"[{_agentName}(AStarPlanner)] 子プランが成功した場合、各子プランにアクションを後方に追加: {string.Join(",", newPlans.Select(p => string.Join("->", p.Select(a => a.name))))}");
                allPlans.InsertRange(0, newPlans);
            }
        }

        // プランの内容をデバッグ表示
        foreach (var plan in allPlans)
        {
            var planStr = string.Join(" -> ", plan.Select(a => a.name));
            DebugLogger.Log($"[{_agentName}(AStarPlanner)] 生成されたプラン (階層: {actionIndex}): {planStr}");
        }

        DebugLogger.Log($"-- 前の階層へ(終了) -- ({actionIndex} -> {actionIndex - 1})");
        return allPlans;
    }

    // 全てのゴール条件が満たされているか?
    bool GoalSatisfied(PlayerBlackboard playerBlackboard, List<GoapCondition> goals)
    {
        string debugPrefix = $"[{_agentName}(AStarPlanner)]";

        DebugLogger.Log($"{debugPrefix} ゴール状態: {string.Join(", ", goals.Select(g => $"{g.Tag}={g.ExpectedValue}"))}");
        
        bool allSatisfied = true;
        foreach (var goal in goals)
        {
            bool satisfied = IsGoalSatisfied(playerBlackboard, goal);
            if (!satisfied)
            {
                DebugLogger.Log($"[{_agentName}(AStarPlanner)] ゴール '{goal.Tag}' が満たされていません");
                allSatisfied = false;
            }
        }
        
        if (allSatisfied)
        {
            DebugLogger.Log($"{debugPrefix} 全てのゴール条件が満たされています");
        }
        return allSatisfied;
    }
    
    // ゴール条件が満たされているか?(単一のゴール条件)
    bool IsGoalSatisfied(PlayerBlackboard playerBlackboard, GoapCondition goal)
    {
        // PlayerBlackboardからゴール条件の現在の値を取得
        var factObj = new Fact(goal.Tag, goal.ExpectedValue.ToString().ToLower());
        var currentValue = playerBlackboard.GetFact(factObj);

        DebugLogger.Log($"[{_agentName}(AStarPlanner)] ゴール '{goal.Tag}' の現在の値: {currentValue}, 期待値: {goal.ExpectedValue}");
        
        return currentValue == goal.ExpectedValue;
    }

    // 条件を満たしているアクションを探す(少なくとも1つのゴール条件を満たす)
    private List<GoapActionSO> FindActionsForSomeGoals(List<GoapCondition> goalFacts, PlayerBlackboard playerBlackboard, Queue<GoapActionSO> currentPlan)
    {
        var satisfyingActions = new List<GoapActionSO>();
        
        // 既に満たされているゴールはスキップ。ただし、反対効果を持つアクションが存在する場合は保護のため対象に含める
        var unsatisfiedGoals = new List<GoapCondition>();
        foreach (var g in goalFacts)
        {
            bool isSatisfied = IsGoalSatisfied(playerBlackboard, g);
            if (!isSatisfied)
            {
                unsatisfiedGoals.Add(g);
                continue;
            }

            // 反対効果（同タグで値が異なる）が存在するか？
            bool hasOppositeEffectSomewhere = false;
            foreach (var a in _availableActions)
            {
                foreach (var e in a.Effects)
                {
                    if (e.Tag == g.Tag && e.ExpectedValue != g.ExpectedValue)
                    {
                        hasOppositeEffectSomewhere = true;
                        break;
                    }
                }
                if (hasOppositeEffectSomewhere) break;
            }

            if (hasOppositeEffectSomewhere)
            {
                // 既に追加されていなければ追加
                bool alreadyAdded = false;
                foreach (var ug in unsatisfiedGoals)
                {
                    if (ug.Tag == g.Tag && ug.ExpectedValue == g.ExpectedValue)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (!alreadyAdded)
                {
                    unsatisfiedGoals.Add(g);
                }
            }
        }
        
        foreach (var action in _availableActions)
        {
            // すでにcurrentPlanに含まれているアクションは除外
            if (currentPlan.Contains(action)){
                continue;
            }
            
            // このアクションが少なくとも1つのゴール条件を満たすか、かつ
            // ゴールと同タグで反対条件の効果を含まないかチェック
            bool satisfiesAnyGoal = false;
            bool conflictsAnyGoal = false;

            // アクションとゴールのデバッグ出力用
            System.Text.StringBuilder debugInfo = new System.Text.StringBuilder();
            debugInfo.AppendLine($"[AStarPlanner] Action: {action.ActionName}");

            foreach (var goal in unsatisfiedGoals)
            {
                debugInfo.AppendLine($"  Goal: Tag={goal.Tag}, Value={goal.ExpectedValue}");

                foreach (var effect in action.Effects)
                {
                    debugInfo.AppendLine($"    Effect: Tag={effect.Tag}, Value={effect.ExpectedValue}");

                    if (effect.Tag != goal.Tag) continue;

                    if (effect.ExpectedValue == goal.ExpectedValue)
                    {
                        satisfiesAnyGoal = true;
                        debugInfo.AppendLine("      -> Satisfies this goal.");
                    }
                    else
                    {
                        // 同タグで反対条件の効果がある場合はコンフリクト
                        conflictsAnyGoal = true;
                        debugInfo.AppendLine("      -> Conflicts with this goal!");
                        break;
                    }
                }

                if (conflictsAnyGoal) break;
            }
            
            debugInfo.AppendLine($"  Result: SatisfiesAnyGoal={satisfiesAnyGoal}, ConflictsAnyGoal={conflictsAnyGoal}");
            UnityEngine.Debug.Log(debugInfo.ToString());

            if (satisfiesAnyGoal && !conflictsAnyGoal)
            {
                satisfyingActions.Add(action);
            }
        }
        
        satisfyingActions.Sort((a, b) =>
            a.CalculateDynamicCost(playerBlackboard).CompareTo(b.CalculateDynamicCost(playerBlackboard)));

        return satisfyingActions;
    }

    // ゴール条件を更新
    private List<GoapCondition> UpdateGoalFacts(List<GoapCondition> goalFacts, GoapActionSO action)
    {
        var newGoalFacts = new List<GoapCondition>();
        
        // アクションの効果で満たされないゴール条件のみを残す
        foreach (var goal in goalFacts)
        {
            bool isSatisfiedByAction = false;
            foreach (var effect in action.Effects)
            {
                if (effect.Tag == goal.Tag && effect.ExpectedValue == goal.ExpectedValue)
                {
                    isSatisfiedByAction = true;
                    break;
                }
            }
            
            if (!isSatisfiedByAction)
            {
                newGoalFacts.Add(goal);
            }
        }
        
        // アクションの前提条件を追加（重複を避ける）
        foreach (var precondition in action.Preconditions)
        {
            bool alreadyExists = false;
            foreach (var existingGoal in newGoalFacts)
            {
                if (existingGoal.Tag == precondition.Tag && existingGoal.ExpectedValue == precondition.ExpectedValue)
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            if (!alreadyExists)
            {
                newGoalFacts.Add(precondition);
            }
        }
        
        return newGoalFacts;
    }

    private static bool IsValidPlan(Queue<GoapActionSO> plan)
    {
        if (plan == null)
        {
            return false;
        }

        if (plan.Count == 0)
        {
            return true;
        }

        foreach (GoapActionSO action in plan)
        {
            if (action == null || string.IsNullOrEmpty(action.ActionName))
            {
                return false;
            }
        }

        return true;
    }
}
