using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 検証パターン番号 = <see cref="GoapSupportLayoutPatternId"/> の enum 値（0〜18 連番）。
/// #0=Baseline, #1=Custom, #2=Clustered … #18=LwOwner_Drive
/// </summary>
public static class GoapSupportLayoutPatternCatalog
{
    public const int NumberMin = 0;
    public const int NumberMax = 18;

    public static bool TryGetByNumber(int number, out GoapSupportLayoutPatternId pattern)
    {
        if (!Enum.IsDefined(typeof(GoapSupportLayoutPatternId), number))
        {
            pattern = GoapSupportLayoutPatternId.Baseline;
            return false;
        }

        pattern = (GoapSupportLayoutPatternId)number;
        return true;
    }

    public static int GetNumber(GoapSupportLayoutPatternId pattern) => (int)pattern;

    public static List<GoapSupportLayoutPatternId> BuildRange(int start, int end)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        start = Mathf.Clamp(start, NumberMin, NumberMax);
        end = Mathf.Clamp(end, NumberMin, NumberMax);

        var list = new List<GoapSupportLayoutPatternId>();
        for (int number = start; number <= end; number++)
        {
            if (TryGetByNumber(number, out GoapSupportLayoutPatternId pattern))
            {
                list.Add(pattern);
            }
        }

        return list;
    }

    public static List<GoapSupportLayoutPatternId> BuildList(params int[] numbers)
    {
        var list = new List<GoapSupportLayoutPatternId>();
        if (numbers == null)
        {
            return list;
        }

        foreach (int number in numbers)
        {
            if (TryGetByNumber(number, out GoapSupportLayoutPatternId pattern))
            {
                list.Add(pattern);
            }
        }

        return list;
    }

    /// <summary>CF 保持静止（#2〜7, #10〜12）。翼 #8/#9 は CSA 専用のため除外。</summary>
    public static List<GoapSupportLayoutPatternId> BuildCfOwnerStaticSuite()
    {
        var list = BuildRange(2, 7);
        list.AddRange(BuildRange(10, 12));
        return list;
    }

    /// <summary>CF 保持静止（GetOpen 用。#6 AtCorrectLanes 除外）。</summary>
    public static List<GoapSupportLayoutPatternId> BuildCfOwnerStaticGetOpenSuite()
    {
        var list = BuildCfOwnerStaticSuite();
        list.Remove(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);
        return list;
    }

    public static List<GoapSupportLayoutPatternId> BuildWingOwnerSuite() =>
        BuildRange(8, 9);

    public static List<GoapSupportLayoutPatternId> BuildWingOwnerDriveSuite() =>
        BuildRange(17, 18);

    public static List<GoapSupportLayoutPatternId> BuildAllDriveSuite() =>
        BuildRange(13, 18);

    /// <summary>CSA 本番選出回帰（#6 理想レーン上 + #8/#9 翼保持）。GetOpen が正しく選ばれる逆サイド等は含めない。</summary>
    public static List<GoapSupportLayoutPatternId> BuildCsaRegressionSuite() =>
        BuildList(6, 8, 9);

    /// <summary>GetOpen + CSA + MTS 統合本番選出回帰（#2〜12 = 11 パターン、計 13 slot 評価）。</summary>
    public static List<GoapSupportLayoutPatternId> BuildCombinedSupportRegressionSuite() =>
        BuildRange(2, 12);

    /// <summary>統合リグレッションで CreateSupportAngle を期待するパターン（#6,8,9）。</summary>
    public static bool IsCombinedSupportRegressionCsaPattern(GoapSupportLayoutPatternId pattern) =>
        pattern == GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes
        || pattern == GoapSupportLayoutPatternId.RwOwner_WingHold
        || pattern == GoapSupportLayoutPatternId.LwOwner_WingHold;

    /// <summary>統合リグレッションで GetOpen を期待する CF 保持パターン（#2〜12 のうち #6,8,9 以外）。</summary>
    public static bool IsCombinedSupportRegressionGetOpenPattern(GoapSupportLayoutPatternId pattern)
    {
        if (IsCombinedSupportRegressionCsaPattern(pattern))
        {
            return false;
        }

        int number = GetNumber(pattern);
        return number >= 2 && number <= 12;
    }
}
