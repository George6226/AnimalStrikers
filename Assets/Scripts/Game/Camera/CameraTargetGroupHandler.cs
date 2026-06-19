using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

// カメラに映すグループ
public class CameraTargetGroupHandler : MonoBehaviour
{
    // カメラグループ
    [SerializeField] private CinemachineTargetGroup targetGroup;

    void Start()
    {
        targetGroup = GetComponent<CinemachineTargetGroup>();
        if (targetGroup != null)
        {
            // Group Centerの計算をZ軸のみに設定
            targetGroup.m_PositionMode = CinemachineTargetGroup.PositionMode.GroupCenter;
            targetGroup.m_RotationMode = CinemachineTargetGroup.RotationMode.GroupAverage;
        }
    }

    void LateUpdate()
    {
        if (targetGroup != null && targetGroup.m_Targets.Length > 0)
        {
            float zSum = 0;
            float weightSum = 0;

            // Z軸の重み付き平均を計算
            for (int i = 0; i < targetGroup.m_Targets.Length; ++i)
            {
                var target = targetGroup.m_Targets[i];
                if (target.target != null)
                {
                    zSum += target.target.position.z * target.weight;
                    weightSum += target.weight;
                }
            }

            // グループの位置を更新（Z軸のみ）
            Vector3 position = transform.position;
            position.x = 0f;
            position.z = weightSum > 0 ? zSum / weightSum : 0f;
            transform.position = position;
        }
    }

    // ターゲットの追加
    public void AddTarget(Transform target, float weight, float radius)
    {
        if (targetGroup != null && target != null)
        {
            targetGroup.AddMember(target, weight, radius);
        }
    }

    // ターゲットの削除
    public void RemoveTarget(Transform target)
    {
        if (targetGroup != null && target != null)
        {
            targetGroup.RemoveMember(target);
        }
    }
}
