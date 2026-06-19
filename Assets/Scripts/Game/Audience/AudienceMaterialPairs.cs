using UnityEngine;

[CreateAssetMenu(fileName = "AudienceMaterialPairs", menuName = "Game/Audience Material Pairs")]
public class AudienceMaterialPairs : ScriptableObject
{
    [System.Serializable]
    public class MaterialPair
    {
        public Material material1;
        public Material material2;
    }

    public MaterialPair[] materialPairs;  // 2コマセットの配列
} 