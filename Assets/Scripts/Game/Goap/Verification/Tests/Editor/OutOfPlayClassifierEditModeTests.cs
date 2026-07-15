#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class OutOfPlayClassifierEditModeTests
{
    [Test]
    public void Classify_InBounds_ReturnsNone()
    {
        var field = CreateField();
        var result = OutOfPlayClassifier.Classify(new Vector3(1f, 0f, 2f), field);
        Assert.That(result.IsOutOfPlay, Is.False);
        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.None));
    }

    [Test]
    public void Classify_PastSideline_IsThrowIn()
    {
        var field = CreateField();
        // halfW = 7
        var result = OutOfPlayClassifier.Classify(
            new Vector3(8f, 0f, 0f),
            field,
            lastTouchByOtherTeam: false);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.ThrowIn));
        Assert.That(result.SideSignX, Is.EqualTo(1f).Within(0.01f));
        Assert.That(result.HasRestartTeam, Is.True);
        Assert.That(result.RestartTeamIsOther, Is.True, "味方が最終接触 → 敵がスローイン");
    }

    [Test]
    public void Classify_PastOwnGoalMouth_IsAllyGoalKick()
    {
        var field = CreateField();
        // halfL = 20, OwnGoal z=-20
        var result = OutOfPlayClassifier.Classify(new Vector3(0f, 0f, -21f), field);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.GoalKick));
        Assert.That(result.EndSignZ, Is.EqualTo(-1f).Within(0.01f));
        Assert.That(result.HasRestartTeam, Is.True);
        Assert.That(result.RestartTeamIsOther, Is.False);
    }

    [Test]
    public void Classify_PastEnemyGoalMouth_IsEnemyGoalKick()
    {
        var field = CreateField();
        var result = OutOfPlayClassifier.Classify(new Vector3(1f, 0f, 21f), field);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.GoalKick));
        Assert.That(result.RestartTeamIsOther, Is.True);
    }

    [Test]
    public void Classify_PastOwnEndOutsideMouth_IsEnemyCorner()
    {
        var field = CreateField();
        // |x| > goal mouth → corner for attacking (other) team
        var result = OutOfPlayClassifier.Classify(new Vector3(5f, 0f, -21f), field);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.CornerKick));
        Assert.That(result.RestartTeamIsOther, Is.True);
        Assert.That(result.SideSignX, Is.EqualTo(1f).Within(0.01f));
    }

    [Test]
    public void Classify_PastEnemyEndOutsideMouth_IsAllyCorner()
    {
        var field = CreateField();
        var result = OutOfPlayClassifier.Classify(new Vector3(-5f, 0f, 21f), field);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.CornerKick));
        Assert.That(result.RestartTeamIsOther, Is.False);
    }

    [Test]
    public void Classify_ThrowInWithoutLastTouch_HasNoRestartTeam()
    {
        var field = CreateField();
        var result = OutOfPlayClassifier.Classify(new Vector3(-8f, 0f, 3f), field);

        Assert.That(result.Kind, Is.EqualTo(SetPieceKind.ThrowIn));
        Assert.That(result.HasRestartTeam, Is.False);
    }

    [Test]
    public void ResolveGoalKickBallPosition_Ally_NearOwnGoalTowardCenter()
    {
        var field = CreateField();
        Vector3 pos = SetPieceAssignmentRules.ResolveGoalKickBallPosition(
            field,
            restartTeamIsOther: false,
            homeDepth: ConstData.GK_SPAWN_DEPTH_ALLY);

        Assert.That(pos.z, Is.GreaterThan(field.OwnGoalPosition.z));
        Assert.That(pos.z, Is.LessThan(0f));
        Assert.That(pos.z, Is.EqualTo(-20f + ConstData.GK_SPAWN_DEPTH_ALLY).Within(0.05f));
    }

    private static TeamFieldInfo CreateField()
    {
        var field = new TeamFieldInfo();
        field.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);
        return field;
    }
}
#endif
