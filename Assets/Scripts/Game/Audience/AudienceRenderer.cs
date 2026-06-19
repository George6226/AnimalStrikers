using UnityEngine;
using System.Collections;  // 追加

public class AudienceRenderer : MonoBehaviour
{
    public enum SlopeDirection
    {
        ZAxis,  // Z軸方向に傾斜
        XAxis   // X軸方向に傾斜
    }

    [System.Serializable]
    public class MaterialPair
    {
        public Material material1;
        public Material material2;
    }

    [SerializeField] private AudienceMaterialPairs materialPairsData;  // ScriptableObjectの参照
    [SerializeField] private int gridSize = 16;  // グリッドサイズを16に
    [SerializeField] private float minY = 5f;           // 最小Y座標を上げる
    [SerializeField] private float planeSize = 1f;      // Planeのサイズ
    [SerializeField] private float spacingX = 1.2f;  // X軸方向の間隔
    [SerializeField] private float spacingZ = 1.5f;  // Z軸方向の間隔
    [SerializeField] private Vector3 rotation = new Vector3(90, 90, 90);  // 回転角度を設定可能に
    [SerializeField] private float heightIncrement = 0.5f;  // 1段ごとの高さ増分
    [SerializeField] private SlopeDirection slopeDirection;  // 傾斜方向の選択
    [SerializeField] private Vector3 customScale = Vector3.one;  // カスタムスケールを追加

    private Matrix4x4[] matrices;  // 変換行列の配列
    private Mesh planeMesh;  // Planeメッシュ

    void Start()
    {
        StartCoroutine(WaitForAudienceManager());
    }

    private IEnumerator WaitForAudienceManager()
    {
        while (AudienceManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        SetupAudience();
        Debug.Log("SetupAudience:materices:" + matrices + " planeMesh:" + planeMesh);
        if (matrices != null && planeMesh != null)
        {
            Debug.Log("登録:");
            AudienceManager.Instance.RegisterAudience(matrices, planeMesh, materialPairsData);
        }
    }

    void OnDisable()
    {
        if (matrices != null)
        {
            AudienceManager.Instance.UnregisterAudience(matrices);
        }
    }

    // 配置処理を関数化
    public void SetupAudience()
    {
        // GPUインスタンシングを有効化
        foreach (var pair in materialPairsData.materialPairs)
        {
            if (pair.material1 != null)
            {
                pair.material1.enableInstancing = true;
            }
            if (pair.material2 != null)
            {
                pair.material2.enableInstancing = true;
            }
        }

        // Planeメッシュの作成
        planeMesh = new Mesh();
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f * planeSize, 0, 0.5f * planeSize),
            new Vector3(0.5f * planeSize, 0, 0.5f * planeSize),
            new Vector3(-0.5f * planeSize, 0, -0.5f * planeSize),
            new Vector3(0.5f * planeSize, 0, -0.5f * planeSize)
        };
        planeMesh.vertices = vertices;

        int[] tris = new int[6] { 0, 2, 1, 2, 3, 1 };
        planeMesh.triangles = tris;

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(0, 0),
            new Vector2(1, 0)
        };
        planeMesh.uv = uvs;

        // メッシュの法線を上向きに設定
        Vector3[] normals = new Vector3[4]
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };
        planeMesh.normals = normals;

        planeMesh.RecalculateNormals();

        // インスタンス数を16x16に設定
        matrices = new Matrix4x4[gridSize * gridSize];

        // 16x16のグリッドに配置
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int index = z * gridSize + x;
                
                float totalWidthX = spacingX * (gridSize - 1);
                float totalWidthZ = spacingZ * (gridSize - 1);
                float startX = -totalWidthX / 2;
                float startZ = -totalWidthZ / 2;
                
                // 中心からの相対位置で高さを計算
                float heightOffset;
                if (slopeDirection == SlopeDirection.ZAxis)
                {
                    float centerZ = gridSize / 2f;
                    heightOffset = (z - centerZ) * heightIncrement;
                }
                else
                {
                    float centerX = gridSize / 2f;
                    heightOffset = (x - centerX) * heightIncrement;
                }
                
                Vector3 localPosition = new Vector3(
                    startX + (x * spacingX),
                    minY + heightOffset,
                    startZ + (z * spacingZ)
                );

                // ローカル座標をグローバル座標に変換
                Vector3 position = transform.TransformPoint(localPosition);
                Quaternion rot = transform.rotation * Quaternion.Euler(rotation.x, rotation.y, rotation.z);

                // スケールを適用
                Vector3 scale = Vector3.Scale(customScale, transform.lossyScale);
                matrices[index] = Matrix4x4.TRS(position, rot, scale);
            }
        }
    }
}