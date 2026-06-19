using UnityEngine;

/// <summary>
/// パスの物理挙動（キックベクトルの計算など）を担当する補助クラス。
/// </summary>
public class AnimalPass_Physics : MonoBehaviour
{
    /// <summary>
    /// needsLob フラグに応じて、ボールのキックベクトルを計算して返す。
    /// </summary>
    public Vector3 CalcKick(Vector3 dir, float distance, bool needsLob, float passStat)
    {
        Vector3 adjustedDir = ApplyAccuracySpread(dir, passStat);
        if (needsLob)
        {
            return CalcLobKick(adjustedDir, distance);
        }
        else
        {
            return CalcNormalKick(adjustedDir, distance);
        }
    }

    /// <summary>
    /// ロブパス用のキックベクトルを計算して返す。
    /// </summary>
    public Vector3 CalcLobKick(Vector3 dir, float distance)
    {
        // 放物線の最大高さを5.0fに制限したパス
        float maxHeight = 5.0f;
        float g = Physics.gravity.magnitude;

        // 水平方向の距離から必要な初速を計算
        float horizontalSpeed = Mathf.Sqrt(distance * g / Mathf.Sin(2 * Mathf.PI / 4));

        Vector3 horizontalDir = new Vector3(dir.x, 0, dir.z).normalized;
        Vector3 kickDir = horizontalDir * horizontalSpeed * Mathf.Cos(Mathf.PI / 4);
        kickDir.y = horizontalSpeed * Mathf.Sin(Mathf.PI / 4);

        // 最大高さが5.0fを超える場合は速度を調整
        float calculatedMaxHeight = (kickDir.y * kickDir.y) / (2 * g);
        if (calculatedMaxHeight > maxHeight)
        {
            float scale = Mathf.Sqrt(maxHeight / calculatedMaxHeight);
            kickDir *= scale;
        }

        return kickDir;
    }

    /// <summary>
    /// 通常パス用のキックベクトルを計算して返す。
    /// </summary>
    public Vector3 CalcNormalKick(Vector3 dir, float distance)
    {
        // 距離に応じて到達時間（秒）を変える
        // 近距離ほど時間を短くし、遠距離は少し長めにするイメージ
        // 例:
        //  - 0〜3m: 0.4秒
        //  - 3〜8m: 0.7秒
        //  - 8m以上: 1.0秒
        float passTime;
        if (distance <= 3.0f)
        {
            passTime = 0.4f;
        }
        else if (distance <= 8.0f)
        {
            passTime = 0.7f;
        }
        else
        {
            passTime = 1.0f;
        }

        // 距離 / 時間 で目標速度を決める
        float speed = distance / passTime;

        // 方向ベクトルを正規化して速度を掛ける
        return dir.normalized * speed;
    }

    private Vector3 ApplyAccuracySpread(Vector3 dir, float passStat)
    {
        float clampedPass = Mathf.Clamp(passStat, 0f, 100f);
        float inaccuracy = 1.0f - (clampedPass / 100.0f);
        float spreadAngle = Random.Range(-ConstData.MAX_PASS_SPREAD_ANGLE, ConstData.MAX_PASS_SPREAD_ANGLE) * inaccuracy;
        return Quaternion.AngleAxis(spreadAngle, Vector3.up) * dir.normalized;
    }
}

