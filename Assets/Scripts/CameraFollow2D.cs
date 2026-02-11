using UnityEngine;

// 2D相机跟随（Unity原生API实现）
public class CameraFollow2D : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 要跟随的目标（拖入Player）
    [Header("相机偏移（X/Y）")]
    public Vector2 offset = new Vector2(0, 1); // 相机和目标的固定偏移
    [Header("平滑速度（0=无平滑）")]
    public float smoothSpeed = 5f; // 平滑跟随的速度，值越大越灵敏

    // 相机的Z轴固定值（2D相机Z轴不能变）
    private float cameraZ;

    void Start()
    {
        // 记录相机初始的Z轴值（2D相机Z轴必须固定，否则画面会错位）
        cameraZ = transform.position.z;
        // 确保相机是正交投影（2D游戏必备）
        Camera.main.orthographic = true;
    }

    // 相机跟随推荐用LateUpdate（在所有Update执行完后执行，避免物体移动和相机跟随不同步导致的抖动）
    void LateUpdate()
    {
        if (target == null) return; // 防止没拖入目标导致报错

        // 1. 计算目标位置（保持Z轴不变，只跟随X/Y）
        Vector3 targetPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            cameraZ
        );

        // 2. 平滑移动相机（用Lerp实现线性插值，无平滑则直接赋值）
        if (smoothSpeed > 0)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
}
