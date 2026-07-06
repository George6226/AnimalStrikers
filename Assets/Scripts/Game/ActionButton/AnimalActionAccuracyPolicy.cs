using UnityEngine;

/// <summary>
/// パス・シュートのパラメーター由来角度ブレ。
/// GOAP 検証では再現性のためブレなし、本番プレイでは従来どおり適用する。
/// </summary>
public static class AnimalActionAccuracyPolicy
{
    public static bool UseDeterministicDirection =>
        GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive;

    public static Vector3 ApplyHorizontalSpread(Vector3 dir, float stat0to100, float maxSpreadAngle)
    {
        if (UseDeterministicDirection)
        {
            return dir.normalized;
        }

        float clamped = Mathf.Clamp(stat0to100, 0f, 100f);
        float inaccuracy = 1.0f - (clamped / 100.0f);
        float spreadAngle = Random.Range(-maxSpreadAngle, maxSpreadAngle) * inaccuracy;
        return Quaternion.AngleAxis(spreadAngle, Vector3.up) * dir.normalized;
    }
}
