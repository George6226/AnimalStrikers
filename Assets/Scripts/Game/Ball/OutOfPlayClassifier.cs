using UnityEngine;

/// <summary>
/// 6-B P0: ボール位置からアウトオブプレイ種別を判定する（純関数・ランタイム未接続）。
/// フィールドは中心原点、味方ゴール −Z / 敵ゴール +Z。
/// </summary>
public static class OutOfPlayClassifier
{
    public readonly struct Result
    {
        public readonly SetPieceKind Kind;
        public readonly float SideSignX;
        public readonly float EndSignZ;
        public readonly bool HasRestartTeam;
        public readonly bool RestartTeamIsOther;

        public bool IsOutOfPlay => Kind != SetPieceKind.None;

        public Result(
            SetPieceKind kind,
            float sideSignX,
            float endSignZ,
            bool hasRestartTeam,
            bool restartTeamIsOther)
        {
            Kind = kind;
            SideSignX = sideSignX;
            EndSignZ = endSignZ;
            HasRestartTeam = hasRestartTeam;
            RestartTeamIsOther = restartTeamIsOther;
        }

        public static Result InPlay => new Result(SetPieceKind.None, 0f, 0f, false, false);
    }

    /// <param name="lastTouchByOtherTeam">
    /// スローイン再始動側の決定に使う。null のときスローインはチーム未確定。
    /// ゴールキック／コーナーは幾何（どのエンドラインか）で決まるため不要。
    /// </param>
    public static Result Classify(
        Vector3 ballPosition,
        TeamFieldInfo field,
        float goalMouthHalfWidth = -1f,
        float margin = 0.05f,
        bool? lastTouchByOtherTeam = null)
    {
        if (field == null || field.FieldLength < 0.01f || field.FieldWidth < 0.01f)
        {
            return Result.InPlay;
        }

        if (goalMouthHalfWidth < 0f)
        {
            goalMouthHalfWidth = ConstData.GOAL_MOUTH_HALF_WIDTH;
        }

        Vector3 center = field.FieldCenter;
        float halfL = field.FieldLength * 0.5f;
        float halfW = field.FieldWidth * 0.5f;
        float localX = ballPosition.x - center.x;
        float localZ = ballPosition.z - center.z;
        float edge = Mathf.Max(0.01f, margin);

        bool outX = Mathf.Abs(localX) > halfW + edge;
        bool outZ = Mathf.Abs(localZ) > halfL + edge;
        if (!outX && !outZ)
        {
            return Result.InPlay;
        }

        // エンドライン優先（コーナー／ゴールキック）。両方はみ出してもエンド扱い。
        if (outZ)
        {
            float endSign = Mathf.Sign(localZ);
            if (Mathf.Approximately(endSign, 0f))
            {
                endSign = 1f;
            }

            // +Z 端 = 敵ゴール裏 → 守備は敵チーム。−Z 端 = 味方ゴール裏 → 守備は味方。
            bool defendingIsOther = endSign > 0f;
            if (Mathf.Abs(localX) <= goalMouthHalfWidth)
            {
                return new Result(
                    SetPieceKind.GoalKick,
                    sideSignX: 0f,
                    endSignZ: endSign,
                    hasRestartTeam: true,
                    restartTeamIsOther: defendingIsOther);
            }

            // 枠外エンド → コーナーは攻撃側
            return new Result(
                SetPieceKind.CornerKick,
                sideSignX: Mathf.Sign(localX),
                endSignZ: endSign,
                hasRestartTeam: true,
                restartTeamIsOther: !defendingIsOther);
        }

        float sideSign = Mathf.Sign(localX);
        if (Mathf.Approximately(sideSign, 0f))
        {
            sideSign = 1f;
        }

        if (lastTouchByOtherTeam.HasValue)
        {
            // 最終接触した側の相手がスローイン
            return new Result(
                SetPieceKind.ThrowIn,
                sideSignX: sideSign,
                endSignZ: 0f,
                hasRestartTeam: true,
                restartTeamIsOther: !lastTouchByOtherTeam.Value);
        }

        return new Result(
            SetPieceKind.ThrowIn,
            sideSignX: sideSign,
            endSignZ: 0f,
            hasRestartTeam: false,
            restartTeamIsOther: false);
    }
}
