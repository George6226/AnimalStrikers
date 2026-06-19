using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// チームの種類
public enum TEAM
{
    PLAYER = 0,
    ENEMY,
}

// サッカー設定
public class SoccerSettings : MonoBehaviour
{
    // 赤マテリアル
    [SerializeField] private Material _redMaterial;
    public Material RedMaterial
    {
        get { return _redMaterial; }
    }
    // 青マテリアル
    [SerializeField] private Material _blueMaterial;
    public Material BlueMaterial {
        get { return _blueMaterial; }
    }

    [SerializeField] private bool _randomizePlayersTeamForTraining = true;
    public bool RandomizePlayersTeamForTraining
    {
        get { return _randomizePlayersTeamForTraining; }
    }
    // 走る速度
    [SerializeField] private float _agentRunSpeed;
    public float AgentRunSpeed
    {
        get { return _agentRunSpeed; }
    }
}
