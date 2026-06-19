using UnityEngine;

public class BallPositionRing : MonoBehaviour
{
    [SerializeField]
    private float rotateSpeed = 30f;  // リングの回転速度
    
    [SerializeField]
    private float height = 0.01f;     // 地面からの高さ

    private void Update()
    {
        // リングを回転
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // ボールの位置を取得して追従
        if (TeamFacade.Instance.BallManager.Ball == null){
            return;
        }

        Vector3 ballPosition = TeamFacade.Instance.BallManager.Ball.transform.position;
        
        // X,Z座標のみ追従
        transform.position = new Vector3(
            ballPosition.x,
            height,
            ballPosition.z
        );
    }
} 