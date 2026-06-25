/// <summary>
/// 守備検証パターン番号 = enum 値（0〜3 連番）。
/// #0=Baseline, #1=Custom, #2=ClusteredAllies, #3=SpreadMidfield
/// </summary>
public enum GoapDefenseLayoutPatternId
{
    Baseline = 0,
    Custom = 1,
    EnemyOwner_ClusteredAllies = 2,
    EnemyOwner_SpreadMidfield = 3,
}
