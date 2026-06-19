using System.Collections.Generic;
using UnityEngine;

// サメのスペシャル：条件なし・その場で待機（泡エフェクトは <see cref="SpecialBubbleEffect"/> + EffectMaker）
public class SharkSpecialAction : AnimalSpecialActionBase
{
    private const int MaxRandomPickAttempts = 64;

    [SerializeField] private AnimalFacade _myFacade;
    [SerializeField] private int _bubbleCount = 5;
    [SerializeField] private float _bubbleMinSeparation = 3f;
    [SerializeField] private float _bubblePlaceRadius = 8f;
    [SerializeField] private float _bubblePlaceMinRadius = 0.5f;
    [Tooltip("RPC 受信側で Instantiate するプレハブ。未設定時はオーナー側の effect から取得を試みる。")]
    [SerializeField] private GameObject _sharkBubblePrefab;

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    private bool IsLocalPhotonOwner()
    {
        PhotonAnimalFacade photonFacade = _myFacade != null ? _myFacade.GetPhotonAnimalFacade() : null;
        if (photonFacade == null)
        {
            return true;
        }

        Photon.Pun.PhotonView view = photonFacade.GetComponent<Photon.Pun.PhotonView>();
        return view == null || view.IsMine;
    }

    public override void SetEffectCallback(GameObject effect)
    {
        if (effect == null || !effect.name.Contains("SharkSPBubble"))
        {
            return;
        }

        if (effect.GetComponent<SpecialBubbleEffect>() == null)
        {
            return;
        }

        if (!IsLocalPhotonOwner())
        {
            Destroy(effect);
            return;
        }

        float baseY = effect.transform.position.y;
        List<Vector2> xzPositions = ComputeBubbleXzPositions(baseY);
        SpawnBubblesAtPositions(effect, baseY, xzPositions);

        PhotonAnimalFacade photonFacade = _myFacade != null ? _myFacade.GetPhotonAnimalFacade() : null;
        if (photonFacade != null)
        {
            float[] posX = new float[xzPositions.Count];
            float[] posZ = new float[xzPositions.Count];
            for (int i = 0; i < xzPositions.Count; i++)
            {
                posX[i] = xzPositions[i].x;
                posZ[i] = xzPositions[i].y;
            }

            photonFacade.BroadcastSharkBubblePositions(baseY, posX, posZ);
        }
    }

    /// <summary><see cref="PhotonAnimalFacade"/> の RPC から呼ばれる。同期済み座標で泡を生成する。</summary>
    public void ApplyNetworkBubblePositions(float baseY, float[] posX, float[] posZ)
    {
        if (posX == null || posZ == null || posX.Length == 0 || posX.Length != posZ.Length)
        {
            return;
        }

        GameObject prefab = ResolveBubblePrefab(null);
        if (prefab == null)
        {
            Debug.LogWarning("[SharkSpecialAction] 泡プレハブが未設定のため RPC 生成をスキップしました。");
            return;
        }

        var positions = new List<Vector2>(posX.Length);
        for (int i = 0; i < posX.Length; i++)
        {
            positions.Add(new Vector2(posX[i], posZ[i]));
        }

        Transform parent = GetFieldParent();
        GameObject first = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
        SpawnBubblesAtPositions(first, baseY, positions);
    }

    private List<Vector2> ComputeBubbleXzPositions(float baseY)
    {
        int count = Mathf.Max(1, _bubbleCount);
        float halfFieldX = ConstData.FIELD_SIZE_X * 0.5f;
        float halfFieldZ = ConstData.FIELD_SIZE_Z * 0.5f;

        Vector3 sharkPos = _myFacade != null ? _myFacade.transform.position : Vector3.zero;
        sharkPos.y = baseY;
        var sharkXz = new Vector2(sharkPos.x, sharkPos.z);
        float maxDiskInField = Mathf.Min(
            halfFieldX - Mathf.Abs(sharkXz.x),
            halfFieldZ - Mathf.Abs(sharkXz.y));
        maxDiskInField = Mathf.Max(0f, maxDiskInField);
        float rOuter = Mathf.Min(Mathf.Max(0.01f, _bubblePlaceRadius), maxDiskInField);
        float rInner = Mathf.Clamp(_bubblePlaceMinRadius, 0f, Mathf.Max(0f, rOuter - 0.01f));

        return BuildNonOverlappingBubbleXz(
            count,
            sharkXz,
            halfFieldX,
            halfFieldZ,
            Mathf.Max(0.01f, _bubbleMinSeparation),
            rInner,
            rOuter);
    }

    private void SpawnBubblesAtPositions(GameObject effect, float baseY, List<Vector2> xzPositions)
    {
        if (effect == null || xzPositions == null || xzPositions.Count == 0)
        {
            return;
        }

        Transform parent = GetFieldParent();

        void SetupOne(GameObject go, Vector2 xz)
        {
            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: true);
                go.transform.localScale = Vector3.one;
            }

            go.transform.position = new Vector3(xz.x, baseY, xz.y);

            SpecialBubbleEffect b = go.GetComponent<SpecialBubbleEffect>();
            if (b != null)
            {
                b.SetOwnerAnimalFacade(_myFacade);
            }
        }

        SetupOne(effect, xzPositions[0]);
        for (int i = 1; i < xzPositions.Count; i++)
        {
            GameObject clone = parent != null ? Instantiate(effect, parent) : Instantiate(effect);
            SetupOne(clone, xzPositions[i]);
        }
    }

    private GameObject ResolveBubblePrefab(GameObject effectInstance)
    {
        if (_sharkBubblePrefab != null)
        {
            return _sharkBubblePrefab;
        }

        return effectInstance;
    }

    private Transform GetFieldParent()
    {
        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null)
        {
            return null;
        }

        FieldObject_Handler fieldHandler = teamFacade.FieldObjectHandler;
        return fieldHandler != null ? fieldHandler.transform : null;
    }

    /// <summary>Vector2 は (world X, world Z)。フィールドは原点中心の AABB と一致。候補はサメ中心の円環内（面積一様）。</summary>
    private static List<Vector2> BuildNonOverlappingBubbleXz(
        int count,
        Vector2 sharkXz,
        float halfFieldX,
        float halfFieldZ,
        float minSeparation,
        float diskInnerRadius,
        float diskOuterRadius)
    {
        var result = new List<Vector2>(count);
        float minSqr = minSeparation * minSeparation;
        float rIn = diskInnerRadius;
        float rOut = Mathf.Max(rIn + 0.01f, diskOuterRadius);

        for (int i = 0; i < count; i++)
        {
            Vector2 candidate = default;
            bool placed = false;

            for (int attempt = 0; attempt < MaxRandomPickAttempts; attempt++)
            {
                candidate = RandomPointInDiskAnnulusUniform(sharkXz, rIn, rOut);
                candidate.x = Mathf.Clamp(candidate.x, -halfFieldX, halfFieldX);
                candidate.y = Mathf.Clamp(candidate.y, -halfFieldZ, halfFieldZ);
                if (IsFarFromAll(candidate, result, minSqr))
                {
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                candidate = PickFallbackOnRing(
                    i,
                    count,
                    sharkXz,
                    halfFieldX,
                    halfFieldZ,
                    result,
                    minSeparation,
                    minSqr,
                    rIn,
                    rOut);
            }

            result.Add(candidate);
        }

        return result;
    }

    /// <summary>円環内の一様分布（面積）。Vector2 は (x, z)。</summary>
    private static Vector2 RandomPointInDiskAnnulusUniform(Vector2 center, float rInner, float rOuter)
    {
        float t = Random.Range(0f, 1f);
        float r = Mathf.Sqrt(Mathf.Lerp(rInner * rInner, rOuter * rOuter, t));
        float ang = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(center.x + Mathf.Cos(ang) * r, center.y + Mathf.Sin(ang) * r);
    }

    private static bool IsFarFromAll(Vector2 candidate, List<Vector2> placed, float minSqr)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if ((candidate - placed[i]).sqrMagnitude < minSqr)
            {
                return false;
            }
        }

        return true;
    }

    private static Vector2 PickFallbackOnRing(
        int index,
        int count,
        Vector2 sharkXz,
        float halfFieldX,
        float halfFieldZ,
        List<Vector2> placed,
        float minSeparation,
        float minSqr,
        float diskInnerRadius,
        float diskOuterRadius)
    {
        int segments = Mathf.Max(count * 6, 12);
        for (int ring = 1; ring <= 24; ring++)
        {
            float radius = Mathf.Clamp(minSeparation * ring * 0.85f, diskInnerRadius, diskOuterRadius);
            for (int s = 0; s < segments; s++)
            {
                float ang = Mathf.PI * 2f * ((s + (float)index / count) / segments + Random.Range(-0.02f, 0.02f));
                Vector2 c = new Vector2(
                    sharkXz.x + Mathf.Cos(ang) * radius,
                    sharkXz.y + Mathf.Sin(ang) * radius);
                c.x = Mathf.Clamp(c.x, -halfFieldX, halfFieldX);
                c.y = Mathf.Clamp(c.y, -halfFieldZ, halfFieldZ);
                if (IsFarFromAll(c, placed, minSqr))
                {
                    return c;
                }
            }
        }

        return new Vector2(
            Mathf.Clamp(sharkXz.x, -halfFieldX, halfFieldX),
            Mathf.Clamp(sharkXz.y, -halfFieldZ, halfFieldZ));
    }
}
