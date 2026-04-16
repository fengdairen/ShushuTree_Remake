using System.Collections.Generic;
using UnityEngine;

public class TiaoZI : MonoBehaviour
{
    public CellManager cellManager;
    private float floatDistance = 0.5f;
    private float lifeTime = 0.6f;
    private float jumpGap = 0.5f;
    public int sortingOrder = 500;
    private readonly Dictionary<string, float> nextSpawnTimeByRoom = new Dictionary<string, float>();
    private readonly Dictionary<string, GameObject> persistentItems = new Dictionary<string, GameObject>();
    private readonly List<JumpItem> activeItems = new List<JumpItem>();
    private readonly HashSet<string> roomIdsInFrame = new HashSet<string>();

    private class JumpItem
    {
        public string roomInstanceId;
        public Transform transform;
        public Vector3 startPos;
        public float age;
        public float life;
    }

    // 每帧驱动跳字动画并消费房间跳字队列。
    private void Update()
    {
        UpdateJumpItems();
        TryConsumeRoomTiaoZi();
    }

    // 扫描 Room.tiaozi：根据文本内容分流为上浮跳字或常驻提示。
    private void TryConsumeRoomTiaoZi()
    {
        BaseData baseData = BaseData.instance;
        if (baseData == null || baseData.roomList == null || cellManager == null) return;

        roomIdsInFrame.Clear();

        for (int i = 0; i < baseData.roomList.Count; i++)
        {
            Room room = baseData.roomList[i];
            if (room == null || string.IsNullOrEmpty(room.instanceId)) continue;
            roomIdsInFrame.Add(room.instanceId);

            RefreshPersistentPosition(room.instanceId);

            if (room.tiaozi == null || room.tiaozi.Count == 0) continue;

            float roomNextTime;
            if (nextSpawnTimeByRoom.TryGetValue(room.instanceId, out roomNextTime) && Time.time < roomNextTime) continue;

            string jumpText = room.tiaozi.Peek();
            if (string.IsNullOrEmpty(jumpText))
            {
                DequeueRoomTiaoZi(room);
                MarkRoomSpawnCooldown(room.instanceId);
                continue;
            }

            Vector3 spawnPos;
            if (!TryGetBuildingCenter(room.instanceId, out spawnPos)) continue;

            if (IsPersistentText(jumpText))
            {
                ShowPersistentSprite(jumpText, room.instanceId, spawnPos);
                DequeueRoomTiaoZi(room);
                MarkRoomSpawnCooldown(room.instanceId);
                continue;
            }

            SpawnJumpSprite(jumpText, room.instanceId, spawnPos);
            if (IsFeedJumpText(jumpText))
            {
                RemovePersistent(room.instanceId);
            }
            DequeueRoomTiaoZi(room);
            MarkRoomSpawnCooldown(room.instanceId);
        }

        CleanupStalePersistentItems();
    }

    // 将房间跳字队列头部出队。
    private void DequeueRoomTiaoZi(Room room)
    {
        if (room.tiaozi == null || room.tiaozi.Count == 0) return;
        room.tiaozi.Dequeue();
    }


    #region 生成在该在的地方
    // 获取指定建筑实例（可能多格）的重心位置。
    private bool TryGetBuildingCenter(string roomInstanceId, out Vector3 center)
    {
        center = Vector3.zero;
        int instanceId;
        if (!int.TryParse(roomInstanceId, out instanceId)) return false;

        int count = 0;
        Vector3 sum = Vector3.zero;

        for (int x = 0; x < cellManager.columns; x++)
        {
            for (int y = 0; y < cellManager.rows; y++)
            {
                Cell c = cellManager.GetCell(x, y);
                if (c == null || c.state != 2 || c.buildingInstanceId != instanceId || c.visual == null) continue;
                sum += c.visual.transform.position;
                count++;
            }
        }

        if (count <= 0) return false;
        center = sum / count;
        return true;
    }

    // 生成一个上浮后自动销毁的跳字精灵。
    private void SpawnJumpSprite(string spriteName, string instanceId, Vector3 position)
    {
        Sprite sprite = Resources.Load<Sprite>("JumpText/" + spriteName);
        if (sprite == null) return;

        GameObject go = new GameObject("JumpText_" + instanceId + "_" + spriteName);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = new Color(1f, 1f, 1f, 1f);

        JumpItem item = new JumpItem();
        item.roomInstanceId = instanceId;
        item.transform = go.transform;
        item.startPos = position;
        item.age = 0f;
        item.life = lifeTime;
        activeItems.Add(item);
    }

    #endregion

    // 判断是否为持续提示类跳字（不跳动，只固定显示）。
    private bool IsPersistentText(string text)
    {
        return text == "投料失败"
               || text == "剩余时间不足以生产"
               || text == "罢工"
               || text == "等待分配";
    }

    // 判断是否为投料跳字，用于清理常驻失败提示。
    private bool IsFeedJumpText(string text)
    {
        return text == "投料"||text=="产出"||text=="不需要耗材的虚空投料";
    }

    // 显示或更新持续提示类跳字。
    private void ShowPersistentSprite(string spriteName, string instanceId, Vector3 position)
    {
        Sprite sprite = Resources.Load<Sprite>("JumpText/" + spriteName);
        if (sprite == null) return;

        GameObject go;
        if (!persistentItems.TryGetValue(instanceId, out go) || go == null)
        {
            go = new GameObject("PersistentJumpText_" + instanceId);
            persistentItems[instanceId] = go;
        }

        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = go.AddComponent<SpriteRenderer>();
        }

        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = new Color(1f, 1f, 1f, 1f);
    }

    // 每帧把常驻提示同步到建筑重心。
    private void RefreshPersistentPosition(string instanceId)
    {
        GameObject go;
        if (!persistentItems.TryGetValue(instanceId, out go) || go == null) return;

        Vector3 center;
        if (TryGetBuildingCenter(instanceId, out center))
        {
            go.transform.position = center;
        }
    }

    // 清理已经不存在于房间列表中的常驻提示。
    private void CleanupStalePersistentItems()
    {
        if (persistentItems.Count == 0) return;

        List<string> removeIds = null;
        foreach (KeyValuePair<string, GameObject> pair in persistentItems)
        {
            if (roomIdsInFrame.Contains(pair.Key)) continue;
            if (removeIds == null) removeIds = new List<string>();
            removeIds.Add(pair.Key);
        }

        if (removeIds == null) return;
        for (int i = 0; i < removeIds.Count; i++)
        {
            RemovePersistent(removeIds[i]);
        }
    }

    // 移除指定房间的持续提示。
    private void RemovePersistent(string instanceId)
    {
        GameObject go;
        if (persistentItems.TryGetValue(instanceId, out go))
        {
            if (go != null) Destroy(go);
            persistentItems.Remove(instanceId);
        }
    }

    // 更新跳字上浮并在生命周期结束后销毁。
    private void UpdateJumpItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            JumpItem item = activeItems[i];
            if (item == null || item.transform == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            item.age += Time.deltaTime;
            float t = item.life > 0f ? Mathf.Clamp01(item.age / item.life) : 1f;
            Vector3 pos = item.startPos;
            pos.y += floatDistance * t;
            item.transform.position = pos;

            if (item.age >= item.life)
            {
                Destroy(item.transform.gameObject);
                activeItems.RemoveAt(i);
            }
        }
    }

    // 设置房间下次可消费队列的时间点。
    private void MarkRoomSpawnCooldown(string roomInstanceId)
    {
        if (string.IsNullOrEmpty(roomInstanceId)) return;
        nextSpawnTimeByRoom[roomInstanceId] = Time.time + jumpGap;
    }
}
