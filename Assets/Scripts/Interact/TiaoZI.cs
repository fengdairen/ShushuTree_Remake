using System.Collections.Generic;
using UnityEngine;

public class TiaoZI : MonoBehaviour
{
    public CellManager cellManager;
    private float floatDistance = 0.5f;
    private float lifeTime = 0.6f;
    private float jumpGap = 0.5f;
    public int sortingOrder = 500;

    private const string PauseProductionText = "暂停生产";
    private const string RefreshPersistentControlText = "__刷新长期跳字__";

    private static readonly HashSet<string> PersistentTexts = new HashSet<string>
    {
        "投料失败",
        "剩余时间不足以生产",
        "罢工",
        "等待分配",
        PauseProductionText
    };

    private static readonly HashSet<string> FloatingRefreshTexts = new HashSet<string>
    {
        "投料",
        "产出",
        "不需要耗材的虚空投料",
        "开始学习",
        "学习大成功",
        "学习成功",
        "学习失败"
    };

    private readonly Dictionary<string, float> nextSpawnTimeByRoom = new Dictionary<string, float>();
    private readonly Dictionary<string, GameObject> persistentItems = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, string> persistentTextByRoom = new Dictionary<string, string>();
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
        UpdateFloatingJumpItems();
        ProcessRoomTiaoZi();
    }

    #region 主流程
    // 扫描所有房间队列：按“上浮跳字 / 常驻跳字 / 控制指令”三类进行处理。
    private void ProcessRoomTiaoZi()
    {
        BaseData baseData = BaseData.instance;
        if (baseData == null || baseData.roomList == null || cellManager == null) return;

        roomIdsInFrame.Clear();

        for (int i = 0; i < baseData.roomList.Count; i++)
        {
            Room room = baseData.roomList[i];
            if (room == null || string.IsNullOrEmpty(room.instanceId)) continue;
            roomIdsInFrame.Add(room.instanceId);

            Vector3 center;
            bool hasCenter = TryGetBuildingCenter(room.instanceId, out center);

            SyncPausePersistent(room, hasCenter, center);
            RefreshPersistentPosition(room.instanceId, hasCenter, center);

            if (room.isProductionPaused)
            {
                continue;
            }

            if (room.tiaozi == null || room.tiaozi.Count == 0) continue;

            if (!CanConsumeQueue(room.instanceId)) continue;

            string jumpText = room.tiaozi.Peek();
            if (string.IsNullOrEmpty(jumpText))
            {
                DequeueAndMarkCooldown(room);
                continue;
            }

            if (IsRefreshPersistentControl(jumpText))
            {
                RemovePersistent(room.instanceId);
                DequeueAndMarkCooldown(room);
                continue;
            }

            if (!hasCenter)
            {
                continue;
            }

            if (IsPersistentText(jumpText))
            {
                ShowPersistentSprite(jumpText, room.instanceId, center);
                DequeueAndMarkCooldown(room);
                continue;
            }

            SpawnFloatingSprite(jumpText, room.instanceId, center);
            if (ShouldRefreshPersistentByFloatingText(jumpText))
            {
                RemovePersistent(room.instanceId);
            }

            DequeueAndMarkCooldown(room);
        }

        CleanupStalePersistentItems();
    }
    #endregion

    #region 持续跳字逻辑
    // 对暂停生产房间持续显示“暂停生产”，恢复时自动清理该提示。
    private void SyncPausePersistent(Room room, bool hasCenter, Vector3 center)
    {
        if (room == null || string.IsNullOrEmpty(room.instanceId))
        {
            return;
        }

        if (room.isProductionPaused)
        {
            if (hasCenter)
            {
                ShowPersistentSprite(PauseProductionText, room.instanceId, center);
            }
            return;
        }

        string currentPersistentText;
        if (persistentTextByRoom.TryGetValue(room.instanceId, out currentPersistentText)
            && currentPersistentText == PauseProductionText)
        {
            RemovePersistent(room.instanceId);
        }
    }
    #endregion

    #region 队列消费判定
    // 判断当前房间是否到达下一次可消费队列的时间点。
    private bool CanConsumeQueue(string roomInstanceId)
    {
        float roomNextTime;
        return !nextSpawnTimeByRoom.TryGetValue(roomInstanceId, out roomNextTime) || Time.time >= roomNextTime;
    }

    // 将房间跳字队列头部出队。
    private void DequeueRoomTiaoZi(Room room)
    {
        if (room.tiaozi == null || room.tiaozi.Count == 0) return;
        room.tiaozi.Dequeue();
    }

    // 出队并更新房间跳字冷却。
    private void DequeueAndMarkCooldown(Room room)
    {
        if (room == null || string.IsNullOrEmpty(room.instanceId))
        {
            return;
        }

        DequeueRoomTiaoZi(room);
        MarkRoomSpawnCooldown(room.instanceId);
    }
    #endregion

    #region 位置与生成
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
    private void SpawnFloatingSprite(string spriteName, string instanceId, Vector3 position)
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

    #region 文本分类
    // 判断是否为持续提示类跳字（不跳动，只固定显示）。
    private bool IsPersistentText(string text)
    {
        return !string.IsNullOrEmpty(text) && PersistentTexts.Contains(text);
    }

    // 判断是否为“控制类刷新指令”（不显示，只用于清理长期跳字）。
    private bool IsRefreshPersistentControl(string text)
    {
        return text == RefreshPersistentControlText;
    }

    // 判断是否为会触发长期跳字刷新的上浮文本。
    private bool ShouldRefreshPersistentByFloatingText(string text)
    {
        return !string.IsNullOrEmpty(text) && FloatingRefreshTexts.Contains(text);
    }
    #endregion

    #region 持续跳字显示与清理
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
        persistentTextByRoom[instanceId] = spriteName;
    }

    // 每帧把常驻提示同步到建筑重心。
    private void RefreshPersistentPosition(string instanceId, bool hasCenter, Vector3 center)
    {
        GameObject go;
        if (!persistentItems.TryGetValue(instanceId, out go) || go == null) return;

        if (hasCenter)
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

        if (persistentTextByRoom.ContainsKey(instanceId))
        {
            persistentTextByRoom.Remove(instanceId);
        }
    }
    #endregion

    #region 上浮跳字动画
    // 更新跳字上浮并在生命周期结束后销毁。
    private void UpdateFloatingJumpItems()
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
    #endregion

    #region 房间冷却
    // 设置房间下次可消费队列的时间点。
    private void MarkRoomSpawnCooldown(string roomInstanceId)
    {
        if (string.IsNullOrEmpty(roomInstanceId)) return;
        nextSpawnTimeByRoom[roomInstanceId] = Time.time + jumpGap;
    }
    #endregion
}
