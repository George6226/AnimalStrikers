using NUnit.Framework;

public class BallKickoffAssignmentEditModeTests
{
    [Test]
    public void TryDecodeStoredOwnerIndex_MasterLeader_IsAllySlotZero()
    {
        Assert.That(
            BallKickoffAssignment.TryDecodeStoredOwnerIndex(0, out bool isOtherTeam, out int slot),
            Is.True);
        Assert.That(isOtherTeam, Is.False);
        Assert.That(slot, Is.EqualTo(0));
    }

    [Test]
    public void TryDecodeStoredOwnerIndex_OtherLeader_IsEnemySlotZero()
    {
        Assert.That(
            BallKickoffAssignment.TryDecodeStoredOwnerIndex(4, out bool isOtherTeam, out int slot),
            Is.True);
        Assert.That(isOtherTeam, Is.True);
        Assert.That(slot, Is.EqualTo(0));
    }

    [Test]
    public void GetStoredOwnerIndexForTeamLeader_MapsMasterAndOther()
    {
        Assert.That(
            BallKickoffAssignment.GetStoredOwnerIndexForTeamLeader(true),
            Is.EqualTo(BallKickoffAssignment.MasterTeamLeaderStoredIndex));
        Assert.That(
            BallKickoffAssignment.GetStoredOwnerIndexForTeamLeader(false),
            Is.EqualTo(BallKickoffAssignment.OtherTeamLeaderStoredIndex));
    }

    [Test]
    public void PickRandomOpeningOwnerIndex_ReturnsLeaderSlotOnly()
    {
        for (int i = 0; i < 20; i++)
        {
            int index = BallKickoffAssignment.PickRandomOpeningOwnerIndex();
            Assert.That(index == 0 || index == 4, Is.True);
        }
    }
}
