using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPDestory : MonoBehaviour
{
    public CellManager cellManager;
    public Camera mainCamera;
    public bool isDestorying = false;
    public BuildingPanelWiget buildingPanelWiget;

    // 每帧检查是否处于拆解模式，并在点击时拆除目标建筑。
    void Update()
    {
        if (!isDestorying) return;
        if (cellManager == null) return;
        if (MouseToCell.IsPointerOverUI()) return;

        Vector2Int cellPos;
        if (!MouseToCell.TryGetMouseCell(cellManager, mainCamera, out cellPos)) return;

        if (Input.GetMouseButtonDown(0))
        {
            DestroyBuildingAt(cellPos.x, cellPos.y);
        }
    }

    // 拆除指定格子所在的建筑，并清理对应格子的贴图和建筑数据。
    private bool DestroyBuildingAt(int x, int y)
    {
        Cell target = cellManager.GetCell(x, y);
        if (target == null || target.state != 2) return false;

        int instanceId = target.buildingInstanceId;
        string buildingId = target.buildingTag;

        if (!CanDestroyDormitory(buildingId, instanceId))
        {
            EnqueueNoDestroyTiaoZi(buildingId, instanceId);
            return false;
        }

        for (int cx = 0; cx < cellManager.columns; cx++)
        {
            for (int cy = 0; cy < cellManager.rows; cy++)
            {
                Cell c = cellManager.GetCell(cx, cy);
                if (c == null || c.state != 2) continue;

                bool isSameBuilding = instanceId >= 0
                    ? c.buildingInstanceId == instanceId
                    : (cx == x && cy == y);

                if (!isSameBuilding) continue;

                ClearCellSprite(c);
                ClearCellBuildingData(c);
                cellManager.SetCellState(cx, cy, 1);
            }
        }

        RefundHalfConsume(buildingId);
        RemoveRoomRecord(buildingId, instanceId);

        return true;
    }

    #region 宿舍拆除判定
    // 拆除宿舍前检查容量是否足够容纳现有鼠鼠；不够则禁止拆除。
    private bool CanDestroyDormitory(string buildingId, int instanceId)
    {
        int bedCount = GetDormitoryBedCount(buildingId);
        if (bedCount <= 0)
        {
            return true;
        }

        BaseData data = BaseData.instance;
        if (data == null)
        {
            return true;
        }

        int currentMax = data.MaxShuShu;
        int currentCount = data.shushuList != null ? data.shushuList.Count : 0;
        int maxAfterDestroy = currentMax - bedCount;
        return maxAfterDestroy >= currentCount;
    }

    // 根据宿舍建筑id返回床位数量（501=5，502=9）。
    private int GetDormitoryBedCount(string buildingId)
    {
        if (buildingId == "501") return 5;
        if (buildingId == "502") return 9;
        return 0;
    }

    // 禁止拆除时向对应房间推送“不让拆”跳字。
    private void EnqueueNoDestroyTiaoZi(string buildingId, int instanceId)
    {
        if (GetDormitoryBedCount(buildingId) <= 0)
        {
            return;
        }

        BaseData data = BaseData.instance;
        if (data == null || data.roomList == null)
        {
            return;
        }

        string iid = instanceId.ToString();
        for (int i = 0; i < data.roomList.Count; i++)
        {
            Room room = data.roomList[i];
            if (room == null)
            {
                continue;
            }

            if (room.buildingId == buildingId && room.instanceId == iid)
            {
                if (room.tiaozi == null)
                {
                    room.tiaozi = new Queue<string>();
                }
                room.tiaozi.Enqueue("不让拆");
                break;
            }
        }
    }
    #endregion

    // 拆毁后返还部分消耗。
    private void RefundHalfConsume(string buildingId)
    {
        BaseData data = BaseData.instance;
        if (data == null || string.IsNullOrEmpty(buildingId)) return;
        if (buildingPanelWiget == null || buildingPanelWiget.buildingPool == null) return;

        int id;
        if (!int.TryParse(buildingId, out id)) return;

        Building building = buildingPanelWiget.buildingPool.GetBuildingById(id);
        if (building == null) return;

        data.natureEnergy += Mathf.RoundToInt(building.magicToBuild * 0.7f);
    }

    // 从房间列表中移除被拆毁建筑记录。
    private void RemoveRoomRecord(string buildingId, int instanceId)
    {
        BaseData data = BaseData.instance;
        if (data == null || data.roomList == null) return;

        string iid = instanceId.ToString();
        for (int i = data.roomList.Count - 1; i >= 0; i--)
        {
            Room room = data.roomList[i];
            if (room != null && room.buildingId == buildingId && room.instanceId == iid)
            {
                ReleaseWorkersFromRoom(data, room);
                data.roomList.RemoveAt(i);
                break;
            }
        }
    }

    // 释放房间中的鼠鼠，让其回到可分配状态。
    private void ReleaseWorkersFromRoom(BaseData data, Room room)
    {
        if (data == null || data.shushuList == null || room == null || room.shushuIds == null) return;

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            string id = room.shushuIds[i];
            if (string.IsNullOrEmpty(id)) continue;

            for (int j = 0; j < data.shushuList.Count; j++)
            {
                Shushu shu = data.shushuList[j];
                if (shu != null && shu.Id == id)
                {
                    shu.haveJob = false;
                    break;
                }
            }
        }
    }

    // 清除单个格子的精灵贴图。
    private void ClearCellSprite(Cell c)
    {
        if (c == null || c.visual == null) return;

        SpriteRenderer sr = c.visual.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            GameObject defaultPrefab = cellManager.cellPrefab;
            Sprite defaultSprite = null;
            Vector3 defaultScale = Vector3.one;
            SpriteRenderer prefabSr = defaultPrefab.GetComponent<SpriteRenderer>();
            defaultSprite = prefabSr.sprite;

            defaultScale = defaultPrefab.transform.localScale;



            sr.sprite = defaultSprite;
            sr.color = Color.white;
            c.visual.transform.localScale = defaultScale;
        }
    }

    // 清除单个格子的建筑相关数据。
    private void ClearCellBuildingData(Cell c)
    {
        if (c == null) return;

        c.buildingTag = string.Empty;
        c.buildingInstanceId = -1;
        c.relativeAnchorX = 0f;
        c.relativeAnchorY = 0f;
    }

}
