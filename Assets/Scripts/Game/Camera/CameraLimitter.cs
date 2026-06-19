using UnityEngine;
using Cinemachine;

// カメラの制限
public class CameraLimitter : CinemachineExtension
{
    [SerializeField]
    private float fixedXPosition = 8f;  // X軸の固定値

    [SerializeField]
    private float centerZoneZ = 0f; // 中央ゾーンに移動するZ

    [SerializeField]
    private float zoneThreshold = 4f; // 左右判定のしきい値

    [SerializeField]
    private float minZPosition = -10f;  // Z軸の最小値

    [SerializeField]
    private float maxZPosition = 10f;   // Z軸の最大値

    [SerializeField]
    private float zMoveSpeed = 18f; // 目標ゾーンへ移動する速度

    private bool _isZInitialized;
    private float _currentCameraZ;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            var pos = state.RawPosition;
            pos.x = fixedXPosition;
            float targetZ = GetZoneTargetZByBall();
            float dt = deltaTime >= 0f ? deltaTime : Time.deltaTime;

            if (!_isZInitialized)
            {
                _currentCameraZ = pos.z;
                _isZInitialized = true;
            }

            _currentCameraZ = Mathf.MoveTowards(_currentCameraZ, targetZ, zMoveSpeed * dt);
            pos.z = _currentCameraZ;
            state.RawPosition = pos;
        }
    }

    private float GetZoneTargetZByBall()
    {
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        var ball = ballManager != null ? ballManager.Ball : null;

        if (ball == null)
        {
            return Mathf.Clamp(centerZoneZ, minZPosition, maxZPosition);
        }

        float ballZ = ball.transform.position.z;
        float targetZ = centerZoneZ;
        if (ballZ > zoneThreshold)
        {
            targetZ = maxZPosition;
        }
        else if (ballZ < -zoneThreshold)
        {
            targetZ = minZPosition;
        }

        return Mathf.Clamp(targetZ, minZPosition, maxZPosition);
    }
}
