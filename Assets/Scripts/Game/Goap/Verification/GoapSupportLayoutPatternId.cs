/// <summary>味方サポートアクション検証用の共通初期配置パターン ID（番号 = enum 値、0〜18 連番）。</summary>
public enum GoapSupportLayoutPatternId
{
    /// <summary>#0 生成直後の位置。</summary>
    Baseline = 0,
    /// <summary>#1 Custom 座標。</summary>
    Custom = 1,
    /// <summary>#2 CF 保持・密集。</summary>
    CfOwner_Clustered = 2,
    /// <summary>#3 CF 保持・RW 逆サイド（両翼とも理想レーン外）。</summary>
    CfOwner_RwWrongSide = 3,
    /// <summary>#4 CF 保持・LW 逆サイド（両翼とも理想レーン外）。</summary>
    CfOwner_LwOnWrongSide = 4,
    /// <summary>#5 CF 保持・理想レーン手前。</summary>
    CfOwner_NearCorrectLanes = 5,
    /// <summary>#6 CF 保持・理想レーン上（GetOpen 検証では SKIP）。</summary>
    CfOwner_AtCorrectLanes = 6,
    /// <summary>#7 CF 保持・重なり。</summary>
    CfOwner_AllOverlapped = 7,
    /// <summary>#8 RW 保持。</summary>
    RwOwner_WingHold = 8,
    /// <summary>#9 LW 保持。</summary>
    LwOwner_WingHold = 9,
    /// <summary>#10 CF 保持・右サイド。</summary>
    CfOwner_OnRightWing = 10,
    /// <summary>#11 CF 保持・左サイド。</summary>
    CfOwner_OnLeftWing = 11,
    /// <summary>#12 CF 保持・翼が後方。</summary>
    CfOwner_WingsTooDeepBehind = 12,
    /// <summary>#13 CF 保持・理想レーン + 前進ドライブ。</summary>
    CfOwner_AtCorrectLanes_DriveForward = 13,
    /// <summary>#14 CF 保持・理想レーン + 前後ドライブ。</summary>
    CfOwner_AtCorrectLanes_DriveForwardBack = 14,
    /// <summary>#15 CF 保持・手前レーン + 前進ドライブ。</summary>
    CfOwner_NearCorrectLanes_DriveForward = 15,
    /// <summary>#16 CF 保持・理想レーン + 横移動ドライブ。</summary>
    CfOwner_AtCorrectLanes_DriveLateralRight = 16,
    /// <summary>#17 RW 保持 + 前後ドライブ。</summary>
    RwOwner_WingHold_DriveForward = 17,
    /// <summary>#18 LW 保持 + 前後ドライブ。</summary>
    LwOwner_WingHold_DriveForward = 18,
}

/// <summary>一括検証のパターン列（連続範囲以外の定番セット）。</summary>
public enum GoapSupportLayoutBatchPreset
{
    /// <summary>_batchPatternIndexStart〜End の enum 連続番号。</summary>
    ContiguousRange = 0,
    /// <summary>#2〜7,10〜12（CF 保持静止。翼 #8/#9 を除く）。</summary>
    CfOwnerStatic = 1,
    /// <summary>#8,9（翼保持静止）。</summary>
    WingOwner = 2,
    /// <summary>#17,18（翼保持ドライブ）。</summary>
    WingOwnerDrive = 3,
    /// <summary>#13〜18（全ドライブ）。</summary>
    AllDrive = 4,
    /// <summary>#2〜5,7,10〜12（CF 保持静止。翼 #8/#9 と #6 AtCorrectLanes を除く）。</summary>
    CfOwnerStaticGetOpen = 5,
    /// <summary>#6,8,9（CSA 本番選出回帰。理想レーン上・翼保持で CSA が選ばれるパターン）。</summary>
    CsaRegression = 6,
    /// <summary>#2〜12（GetOpen 8 + CSA 3。1 Play で両方の本番選出を検証）。</summary>
    CombinedSupportRegression = 7,
}
