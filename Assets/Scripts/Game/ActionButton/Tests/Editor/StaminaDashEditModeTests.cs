#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>F2: スタミナ不足時のダッシュ禁止。</summary>
public sealed class StaminaDashEditModeTests
{
  [Test]
  public void CanDashFromStaminaRatio_Positive_ReturnsTrue()
  {
    Assert.That(AnimalAction_Dash.CanDashFromStaminaRatio(1f), Is.True);
    Assert.That(AnimalAction_Dash.CanDashFromStaminaRatio(0.01f), Is.True);
  }

  [Test]
  public void CanDashFromStaminaRatio_Zero_ReturnsFalse()
  {
    Assert.That(AnimalAction_Dash.CanDashFromStaminaRatio(0f), Is.False);
  }

  [Test]
  public void CanDashFromStaminaRatio_Negative_ReturnsFalse()
  {
    Assert.That(AnimalAction_Dash.CanDashFromStaminaRatio(-0.1f), Is.False);
  }
}
#endif
