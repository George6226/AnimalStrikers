/// <summary>
/// 守備検証パターン番号 = enum 値（0〜9 連番）。
/// #0=Baseline, #1=Custom, #2-#3=基本, #4-#6=戦術, #7-#8=敵保持ドライブ追従, #9=RetreatToDefensiveLine
/// </summary>
public enum GoapDefenseLayoutPatternId
{
    Baseline = 0,
    Custom = 1,
    EnemyOwner_ClusteredAllies = 2,
    EnemyOwner_SpreadMidfield = 3,
    EnemyOwner_MarkFreeTarget = 4,
    EnemyOwner_BlockPassLane = 5,
    EnemyOwner_BlockShotLane = 6,
    EnemyOwner_ClusteredAllies_DriveForward = 7,
    EnemyOwner_SpreadMidfield_DriveForward = 8,
    EnemyOwner_RetreatToDefensiveLine = 9,
}
