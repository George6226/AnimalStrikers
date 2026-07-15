/// <summary>6-A: 敵フィールド NPC の難易度。</summary>
public enum EnemyAiDifficulty
{
    /// <summary>味方に近いコスト。シュート偏りが弱い。</summary>
    Easy = 0,

    /// <summary>現行既定（Pass ペナルティ + レーン空き時 Shoot 割引）。</summary>
    Normal = 1,

    /// <summary>連携しやすく、フィニッシュも積極的。</summary>
    Hard = 2,
}
