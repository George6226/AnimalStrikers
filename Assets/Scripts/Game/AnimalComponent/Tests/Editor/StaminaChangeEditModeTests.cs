#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>スタミナ増減ルール（通常移動=回復、ダッシュ=消費）。</summary>
public sealed class StaminaChangeEditModeTests
{
  [Test]
  public void ComputeStaminaChangePerSecond_NormalMove_Heals()
  {
    Assert.That(
      AnimalHandler.ComputeStaminaChangePerSecond(false),
      Is.EqualTo(ConstData.STAMINA_NORMAL_MOVE_HEAL_PER_SECOND).Within(0.001f));
  }

  [Test]
  public void ComputeStaminaChangePerSecond_Dash_Drains()
  {
    Assert.That(
      AnimalHandler.ComputeStaminaChangePerSecond(true),
      Is.EqualTo(-ConstData.STAMINA_DASH_DRAIN_PER_SECOND).Within(0.001f));
  }
}
#endif
