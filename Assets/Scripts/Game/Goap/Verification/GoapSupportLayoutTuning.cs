using System;
using UnityEngine;

[Serializable]
public class GoapSupportLayoutTuning
{
    public float OwnerForwardRatio = 0.12f;
    public float ClusterForwardRatio = 0.04f;
    public float ClusterLateralRatio = 0.06f;
    public float WrongSideLateralRatio = 0.22f;
    public float NearLaneBackOffsetRatio = 0.10f;
    public float OverlapMicroLateralRatio = 0.04f;
    public float OverlapMicroForwardRatio = 0.02f;
    public float WingSideOwnerLateralRatio = 0.16f;
    public float BehindOwnerBackOffsetRatio = 0.08f;
}
