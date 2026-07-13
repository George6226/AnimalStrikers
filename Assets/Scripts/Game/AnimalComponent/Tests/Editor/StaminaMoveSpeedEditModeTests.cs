#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

/// <summary>F1: スタミナ枯渇時の移動速度倍率。</summary>
public sealed class StaminaMoveSpeedEditModeTests
{
  private const float Threshold = 0.25f;
  private const float ExhaustedMul = 0.55f;

  [Test]
  public void ComputeStaminaMoveSpeedMultiplier_FullStamina_ReturnsOne()
  {
    Assert.That(
      AnimalHandler.ComputeStaminaMoveSpeedMultiplier(1f, Threshold, ExhaustedMul),
      Is.EqualTo(1f).Within(0.001f));
    Assert.That(
      AnimalHandler.ComputeStaminaMoveSpeedMultiplier(Threshold, Threshold, ExhaustedMul),
      Is.EqualTo(1f).Within(0.001f));
  }

  [Test]
  public void ComputeStaminaMoveSpeedMultiplier_Exhausted_ReturnsExhaustedMultiplier()
  {
    Assert.That(
      AnimalHandler.ComputeStaminaMoveSpeedMultiplier(0f, Threshold, ExhaustedMul),
      Is.EqualTo(ExhaustedMul).Within(0.001f));
  }

  [Test]
  public void ComputeStaminaMoveSpeedMultiplier_HalfThreshold_LerpsBetween()
  {
    float expected = Mathf.Lerp(ExhaustedMul, 1f, 0.5f);
    Assert.That(
      AnimalHandler.ComputeStaminaMoveSpeedMultiplier(Threshold * 0.5f, Threshold, ExhaustedMul),
      Is.EqualTo(expected).Within(0.001f));
  }
}
#endif
