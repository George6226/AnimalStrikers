using UnityEngine;
using System.Collections.Generic;

public class AudienceManager : MonoBehaviour
{
    private static AudienceManager instance;
    public static AudienceManager Instance => instance;

    private List<Matrix4x4[]> allMatrices = new List<Matrix4x4[]>();
    private List<Mesh> meshes = new List<Mesh>();
    private List<AudienceMaterialPairs> materialPairs = new List<AudienceMaterialPairs>();

    [SerializeField] private float switchInterval = 0.5f;
    private float timer;
    private int currentFrame;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterAudience(Matrix4x4[] matrices, Mesh mesh, AudienceMaterialPairs materials)
    {
        allMatrices.Add(matrices);
        meshes.Add(mesh);
        materialPairs.Add(materials);
    }

    public void UnregisterAudience(Matrix4x4[] matrices)
    {
        int index = allMatrices.IndexOf(matrices);
        if (index != -1)
        {
            allMatrices.RemoveAt(index);
            meshes.RemoveAt(index);
            materialPairs.RemoveAt(index);
        }
    }

    void Update()
    {
        // フレーム切り替え処理
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            timer = 0;
            currentFrame = (currentFrame + 1) % 2;
        }

        // 登録された全ての観客を描画
        for (int audienceIndex = 0; audienceIndex < allMatrices.Count; audienceIndex++)
        {
            var matrices = allMatrices[audienceIndex];
            var mesh = meshes[audienceIndex];
            var materials = materialPairs[audienceIndex];

            int instanceCount = matrices.Length;
            int batchSize = 1023;
            int batchCount = Mathf.CeilToInt((float)instanceCount / batchSize);

            for (int i = 0; i < batchCount; i++)
            {
                int remainingInstances = instanceCount - (i * batchSize);
                int currentBatchSize = Mathf.Min(batchSize, remainingInstances);

                Matrix4x4[] batchedMatrices = new Matrix4x4[currentBatchSize];
                System.Array.Copy(matrices, i * batchSize, batchedMatrices, 0, currentBatchSize);

                for (int j = 0; j < currentBatchSize; j++)
                {
                    int pairIndex = (i * batchSize + j) % materials.materialPairs.Length;
                    Material currentMaterial = currentFrame == 0 ? 
                        materials.materialPairs[pairIndex].material1 : 
                        materials.materialPairs[pairIndex].material2;

                    Graphics.DrawMeshInstanced(mesh, 0, currentMaterial, 
                        new Matrix4x4[] { batchedMatrices[j] }, 1, null);
                }
            }
        }
    }
} 