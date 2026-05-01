using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseToCell : MonoBehaviour
{
    #region 鼠标到格子坐标转换
    // 尝试将当前鼠标位置解析为有效格子坐标。
    public static bool TryGetMouseCell(CellManager cellManager, Camera mainCamera, out Vector2Int cellPos)
    {
        cellPos = Vector2Int.zero;
        if (cellManager == null)
        {
            return false;
        }

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Vector2Int hitCell;
            if (TryParseCellFromName(hit.transform.name, out hitCell))
            {
                cellPos = hitCell;
                return cellManager.IsValid(cellPos.x, cellPos.y);
            }
        }

        Plane gridPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, cellManager.transform.position.z));
        float enter;
        if (!gridPlane.Raycast(ray, out enter))
        {
            return false;
        }

        Vector3 world = ray.GetPoint(enter);
        int x = Mathf.FloorToInt((world.x - cellManager.origin.x) / cellManager.cellSize.x + 0.5f);
        int y = Mathf.FloorToInt((world.y - cellManager.origin.y) / cellManager.cellSize.y + 0.5f);
        cellPos = new Vector2Int(x, y);
        return cellManager.IsValid(x, y);
    }

    // 从命名格式 Cell_x_y 的物体名称中解析格子坐标。
    public static bool TryParseCellFromName(string objectName, out Vector2Int cellPos)
    {
        cellPos = Vector2Int.zero;
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("Cell_"))
        {
            return false;
        }

        string[] parts = objectName.Split('_');
        if (parts.Length != 3)
        {
            return false;
        }

        int x;
        int y;
        if (!int.TryParse(parts[1], out x) || !int.TryParse(parts[2], out y))
        {
            return false;
        }

        cellPos = new Vector2Int(x, y);
        return true;
    }

    // 判断鼠标是否在 UI 上，避免点击穿透。
    public static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    #endregion
}
