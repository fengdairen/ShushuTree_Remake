using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 10f; // 按住按键时的目标速度
    public float resistance = 5f; // 阻力，按键松开后减速的快慢（值越大越快停止）
    public Vector2 minBounds = new Vector2(-20f, -20f);
    public Vector2 maxBounds = new Vector2(20f, 20f);

    [Header("缩放")]
    public float zoomSpeed = 2f; // 鼠标滚轮每次缩放的速度
    public float zoomMin = 2f;
    public float zoomMax = 10f;

    // 内部状态
    Vector2 velocity = Vector2.zero;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        // 使用原始输入以获得即时响应
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 0.0001f)
        {
            // 按键时朝目标速度移动
            Vector2 target = input.normalized * moveSpeed;
            // 平滑过渡以获得更好的手感
            velocity = Vector2.Lerp(velocity, target, 12f * Time.deltaTime);
        }
        else
        {
            // 无输入时应用阻力模拟惯性（滑行并逐渐停下）
            velocity = Vector2.Lerp(velocity, Vector2.zero, resistance * Time.deltaTime);
            // 避免微小残留速度
            if (velocity.sqrMagnitude < 0.00001f) velocity = Vector2.zero;
        }

        // 移动摄像机
        Vector3 pos = transform.position;
        pos += (Vector3)(velocity * Time.deltaTime);

        // 限制在设定范围内
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);

        transform.position = pos;
    }

    void HandleZoom()
    {
        if (cam == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // 立即缩放（无惯性）
            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, zoomMin, zoomMax);
            }
            else
            {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, zoomMin, zoomMax);
            }
        }
    }
}
