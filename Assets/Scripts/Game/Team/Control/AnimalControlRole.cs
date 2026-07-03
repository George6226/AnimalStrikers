/// <summary>
/// 4vs4（GK含む）における1体あたりの操作主体。
/// </summary>
public enum AnimalControlRole
{
    /// <summary>スポーン直後・未割当</summary>
    Unassigned = 0,
    /// <summary>人間プレイヤーが直接操作するフィールドプレイヤー（1体のみ）</summary>
    Human = 1,
    /// <summary>味方チームのNPCが操作するフィールドプレイヤー（2体）</summary>
    TeammateNpc = 2,
    /// <summary>ゴールキーパー用NPC（常にNPC）</summary>
    GoalkeeperNpc = 3,
    /// <summary>敵チームのフィールドNPC（3体）。鏡像 GOAP 対象。</summary>
    EnemyFieldNpc = 4,
}
