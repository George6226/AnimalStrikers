using System;
using System.Collections.Generic;
using UnityEngine;

public static class GoapDefenseLayoutPatternCatalog
{
    public const int NumberMin = 0;
    public const int NumberMax = 8;

    public static bool TryGetByNumber(int number, out GoapDefenseLayoutPatternId pattern)
    {
        if (!Enum.IsDefined(typeof(GoapDefenseLayoutPatternId), number))
        {
            pattern = GoapDefenseLayoutPatternId.Baseline;
            return false;
        }

        pattern = (GoapDefenseLayoutPatternId)number;
        return true;
    }

    public static int GetNumber(GoapDefenseLayoutPatternId pattern) => (int)pattern;

    public static List<GoapDefenseLayoutPatternId> BuildRange(int start, int end)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        start = Mathf.Clamp(start, NumberMin, NumberMax);
        end = Mathf.Clamp(end, NumberMin, NumberMax);

        var list = new List<GoapDefenseLayoutPatternId>();
        for (int number = start; number <= end; number++)
        {
            if (TryGetByNumber(number, out GoapDefenseLayoutPatternId pattern))
            {
                list.Add(pattern);
            }
        }

        return list;
    }

    /// <summary>Phase 5 守備基本: 相手保持・味方守備位置外の2パターン。</summary>
    public static List<GoapDefenseLayoutPatternId> BuildDefenseBaselineSuite() =>
        BuildRange(2, 3);

    /// <summary>Phase 5b 守備戦術: Mark / BlockPass / BlockShot 単体選出。</summary>
    public static List<GoapDefenseLayoutPatternId> BuildDefenseTacticalSuite() =>
        BuildRange(4, 6);

    /// <summary>Phase 6 守備ドライブ: 敵保持者前後ドライブ + 味方 Retarget 追従。</summary>
    public static List<GoapDefenseLayoutPatternId> BuildDefenseDriveSuite() =>
        BuildRange(7, 8);

    /// <summary>Phase 7a 守備統合本番選出: #2〜#6（全候補コスト競争）。</summary>
    public static List<GoapDefenseLayoutPatternId> BuildDefenseCombinedSuite() =>
        BuildRange(2, 6);
}
