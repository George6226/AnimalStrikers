using UnityEngine;

/// <summary>
/// 6-A: 敵フィールド NPC の攻撃コストバイアスと計画間隔。
/// <see cref="EnemySquadControlController"/> が難易度を適用する。
/// </summary>
public static class EnemyAiBalance
{
    public const float NormalPassPenalty = 0.40f;
    public const float NormalShootDiscount = 0.28f;
    public const float NormalPlanningIntervalSeconds = 5f;

    public static EnemyAiDifficulty Difficulty { get; private set; } = EnemyAiDifficulty.Normal;

    public static float PassPenalty { get; private set; } = NormalPassPenalty;

    public static float ShootDiscount { get; private set; } = NormalShootDiscount;

    public static float PlanningIntervalSeconds { get; private set; } = NormalPlanningIntervalSeconds;

    /// <summary>難易度プリセットを適用。Inspector / 起動時から呼ぶ。</summary>
    public static void Apply(EnemyAiDifficulty difficulty)
    {
        Difficulty = difficulty;
        switch (difficulty)
        {
            case EnemyAiDifficulty.Easy:
                // バイアスを弱め、判断も遅め → 攻めが荒くなりにくい。
                PassPenalty = 0.15f;
                ShootDiscount = 0.08f;
                PlanningIntervalSeconds = NormalPlanningIntervalSeconds * 1.3f;
                break;
            case EnemyAiDifficulty.Hard:
                // パス連携を残しつつシュートをさらに優遇、判断は速め。
                PassPenalty = 0.22f;
                ShootDiscount = 0.40f;
                PlanningIntervalSeconds = NormalPlanningIntervalSeconds * 0.7f;
                break;
            default:
                PassPenalty = NormalPassPenalty;
                ShootDiscount = NormalShootDiscount;
                PlanningIntervalSeconds = NormalPlanningIntervalSeconds;
                break;
        }
    }

    /// <summary>
    /// SerializeField の計画間隔を Normal 基準に、難易度倍率を掛ける。
    /// </summary>
    public static float ResolvePlanningInterval(
        EnemyAiDifficulty difficulty,
        float normalIntervalSeconds)
    {
        float baseInterval = normalIntervalSeconds > 0.01f
            ? normalIntervalSeconds
            : NormalPlanningIntervalSeconds;
        return difficulty switch
        {
            EnemyAiDifficulty.Easy => baseInterval * 1.3f,
            EnemyAiDifficulty.Hard => baseInterval * 0.7f,
            _ => baseInterval,
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Apply(EnemyAiDifficulty.Normal);
    }
}
