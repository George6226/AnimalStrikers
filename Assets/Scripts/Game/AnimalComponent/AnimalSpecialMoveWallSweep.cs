using UnityEngine;

/// <summary>
/// スペシャル移動時の Wall レイヤー / Wall タグへの掃引で、移動デルタをクランプし壁抜けを抑える。
/// </summary>
public sealed class AnimalSpecialMoveWallSweep
{
    private readonly Rigidbody _rb;
    private readonly float _sweepSkin;
    private readonly float _fallbackSphereRadius;
    private readonly int _wallLayerIndex;
    private readonly LayerMask _wallSweepMask;

    public AnimalSpecialMoveWallSweep(Rigidbody rb, float sweepSkin, float fallbackSphereRadius)
    {
        _rb = rb;
        _sweepSkin = sweepSkin;
        _fallbackSphereRadius = fallbackSphereRadius;

        _wallLayerIndex = LayerMask.NameToLayer(ConstData.WALL_LAYER_NAME);
        if (_wallLayerIndex >= 0)
        {
            _wallSweepMask = 1 << _wallLayerIndex;
        }
        else
        {
            _wallSweepMask = default;
            Debug.LogWarning("[AnimalSpecialMoveWallSweep] Layer '" + ConstData.WALL_LAYER_NAME + "' が未定義です。壁掃引はヒットしません。");
        }
    }

    /// <summary>
    /// 移動ベクトルを Capsule（または球）掃引でクランプする。
    /// </summary>
    public Vector3 ClampMoveDelta(Vector3 rigidbodyWorldPosition, Vector3 deltaWorld)
    {
        if (_rb == null || deltaWorld.sqrMagnitude <= 1e-10f)
        {
            return deltaWorld;
        }

        float moveDistance = deltaWorld.magnitude;
        Vector3 dir = deltaWorld / moveDistance;

        CapsuleCollider capsule = _rb.GetComponentInChildren<CapsuleCollider>();
        if (capsule != null)
        {
            GetCapsuleWorldEndpoints(capsule, out Vector3 p0, out Vector3 p1, out float radius);
            float allowed = GetAllowedSweepDistanceCapsule(p0, p1, radius, dir, moveDistance);
            return dir * allowed;
        }

        float r = _fallbackSphereRadius;
        Vector3 sphereOrigin = rigidbodyWorldPosition + Vector3.up * r;
        float castLength = Mathf.Max(0f, moveDistance);
        RaycastHit[] sphereHits = Physics.SphereCastAll(sphereOrigin, r, dir, castLength, _wallSweepMask, QueryTriggerInteraction.Ignore);
        float bestSphere = moveDistance;
        for (int i = 0; i < sphereHits.Length; i++)
        {
            if (IsOwnCollider(sphereHits[i].collider) || !IsBlockingWallCollider(sphereHits[i].collider))
            {
                continue;
            }

            float d = sphereHits[i].distance;
            if (d < bestSphere)
            {
                bestSphere = d;
            }
        }

        if (bestSphere < moveDistance)
        {
            return dir * Mathf.Max(0f, bestSphere - _sweepSkin);
        }

        return deltaWorld;
    }

    private float GetAllowedSweepDistanceCapsule(Vector3 p0, Vector3 p1, float radius, Vector3 dir, float moveDistance)
    {
        RaycastHit[] hits = Physics.CapsuleCastAll(p0, p1, radius, dir, moveDistance, _wallSweepMask, QueryTriggerInteraction.Ignore);
        float best = moveDistance;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsOwnCollider(hits[i].collider) || !IsBlockingWallCollider(hits[i].collider))
            {
                continue;
            }

            float d = hits[i].distance;
            if (d < best)
            {
                best = d;
            }
        }

        return Mathf.Max(0f, best - _sweepSkin);
    }

    private static void GetCapsuleWorldEndpoints(CapsuleCollider c, out Vector3 point0, out Vector3 point1, out float worldRadius)
    {
        Transform tr = c.transform;
        Vector3 dirLocal = c.direction == 0 ? Vector3.right : (c.direction == 1 ? Vector3.up : Vector3.forward);
        float halfHeightMinusR = Mathf.Max(0f, c.height * 0.5f - c.radius);
        Vector3 centerLocal = c.center;
        Vector3 aLocal = centerLocal - dirLocal * halfHeightMinusR;
        Vector3 bLocal = centerLocal + dirLocal * halfHeightMinusR;
        point0 = tr.TransformPoint(aLocal);
        point1 = tr.TransformPoint(bLocal);

        Vector3 scale = tr.lossyScale;
        float rScale;
        if (c.direction == 1)
        {
            rScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }
        else if (c.direction == 0)
        {
            rScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
        else
        {
            rScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        worldRadius = c.radius * rScale;
    }

    private bool IsOwnCollider(Collider col)
    {
        if (col == null)
        {
            return false;
        }

        Transform ct = col.transform;
        return ct == _rb.transform || ct.IsChildOf(_rb.transform);
    }

    private bool IsBlockingWallCollider(Collider col)
    {
        if (col == null || _wallLayerIndex < 0)
        {
            return false;
        }

        if (col.gameObject.layer != _wallLayerIndex)
        {
            return false;
        }

        return col.CompareTag(ConstData.WALL_TAG);
    }
}
