using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPbuild : MonoBehaviour
{
    public BuildingPanelWiget buildingPanelWiget;
    public CellManager cellManager;
    public Camera mainCamera;
    public bool isDestorying = false;

    private readonly List<Cell> previewCells = new List<Cell>();
    private readonly List<PreviewVisualState> previewStates = new List<PreviewVisualState>();
    private int nextBuildingInstanceId = 1;

    private class PreviewVisualState
    {
        public Cell cell;
        public Sprite sprite;
        public Color spriteColor;
        public Color rendererColor;
        public Vector3 localScale;
        public bool hasSpriteRenderer;
        public bool hasRenderer;
    }

    #region 生命周期
    // 每帧处理建造/拆除逻辑。
    void Update()
    {
        if (isDestorying)
        {
            HandleDestroy();
            return;
        }

        int selectedId = buildingPanelWiget.selectedBuildingId;
        if (selectedId < 0 || buildingPanelWiget.buildingPool == null)
        {
            ClearPreview();
            return;
        }

        if (MouseToCell.IsPointerOverUI())
        {
            ClearPreview();
            return;
        }


        Vector2Int anchor;
        if (!MouseToCell.TryGetMouseCell(cellManager, mainCamera, out anchor))
        {
            ClearPreview();
            return;
        }

        Building selectedBuilding = buildingPanelWiget.buildingPool.GetBuildingById(selectedId);
        bool canPlace = CanPlace(selectedBuilding, anchor.x, anchor.y);

        Sprite sourceSprite = Resources.Load<Sprite>("BuildingSprite/" + selectedBuilding.id);
        ShowPreview(selectedBuilding, anchor.x, anchor.y, canPlace, sourceSprite);
        if (canPlace&& Input.GetMouseButtonDown(0))
        {
            Build(selectedBuilding, anchor.x, anchor.y, sourceSprite);
        }

    }
    #endregion

    #region 各种可行性检测的方法 
    // 检查以锚点为起始、占地范围内的所有格子是否允许放置建筑。
    private bool CanPlace(Building building, int anchorX, int anchorY)
    {
        if (!HasEnoughEnergy(building)) return false;

        for (int x = 0; x < building.RoomX; x++)
        {
            for (int y = 0; y < building.RoomY; y++)
            {
                int tx = anchorX + x;
                int ty = anchorY + y;
                if (!cellManager.IsValidtoBuild(tx, ty)) return false;

                Cell c = cellManager.GetCell(tx, ty);
                if (c == null || c.state != 1) return false;
            }
        }

        return true;
    }

    // 检查当前基础能量是否足够支付该建筑的消耗。
    private bool HasEnoughEnergy(Building building)
    {
        BaseData data = BaseData.instance;
        if (data == null || building == null) return false;

        return data.natureEnergy >= building.magicToBuild;
    }

    // 判断格子是否存在有效的 SpriteRenderer 且已设置精灵。
    private bool HasCellSprite(Cell c)
    {
        if (c == null || c.visual == null) return false;
        SpriteRenderer sr = c.visual.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null;
    }
    #endregion

    #region 拆除模式
    // 拆除模式的入口逻辑。
    private void HandleDestroy()
    {
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
    #endregion



    // 预览的核心方法
    private void ShowPreview(Building building, int anchorX, int anchorY, bool canPlace, Sprite sourceSprite)
    {
        ClearPreview();

        Color anchorOverlay = new Color(0f, 0f, 1f, 0.1f);
        Color areaOverlay = canPlace ? new Color(0f, 1f, 0f, 0.1f) : new Color(1f, 0f, 0f, 0.1f);

        for (int x = 0; x < building.RoomX; x++)
        {
            for (int y = 0; y < building.RoomY; y++)
            {
                int tx = anchorX + x;
                int ty = anchorY + y;
                if (!cellManager.IsValid(tx, ty)) continue;

                Cell c = cellManager.GetCell(tx, ty);
                if (c == null) continue;

                previewCells.Add(c);
                CachePreviewState(c);
                TrySetBuildingCellSprite(c, sourceSprite, building.RoomX, building.RoomY, x, y);

                Color overlay = (tx == anchorX && ty == anchorY) ? anchorOverlay : areaOverlay;
                ApplyOverlayColor(c, overlay);
            }
        }
    }

    // 建造的核心方法
    private void Build(Building building, int anchorX, int anchorY, Sprite sourceSprite)
    {
        if (!HasEnoughEnergy(building)) return;

        ClearPreview();

        int instanceId = nextBuildingInstanceId++;
        string tag = building.id.ToString();

        for (int x = 0; x < building.RoomX; x++)
        {
            for (int y = 0; y < building.RoomY; y++)
            {
                int tx = anchorX + x;
                int ty = anchorY + y;
                cellManager.SetCellState(tx, ty, 2);

                Cell c = cellManager.GetCell(tx, ty);
                if (c == null) continue;

                c.buildingTag = tag;
                c.buildingInstanceId = instanceId;
                c.relativeAnchorX = x;
                c.relativeAnchorY = y;
                if (!TrySetBuildingCellSprite(c, sourceSprite, building.RoomX, building.RoomY, x, y))
                {
                    SetCellColor(c, Color.red);
                }
            }
        }

        // 建造成功后扣除能量，并写入房间列表。
        BaseData data = BaseData.instance;
        if (data != null)
        {
            data.natureEnergy -= building.magicToBuild;

            if (data.roomList == null)
            {
                data.roomList = new List<Room>();
            }
            data.roomList.Add(new Room
            {
                buildingId = tag,
                instanceId = instanceId.ToString()
            });
        }

    }


    #region 图像的挂载预览恢复等逻辑

    // 清除当前预览并恢复被临时修改过的格子显示状态。
    private void ClearPreview()
    {
        for (int i = 0; i < previewStates.Count; i++)
        {
            RestorePreviewState(previewStates[i]);
        }
        previewStates.Clear();
        previewCells.Clear();
    }

    // 缓存单个格子的可视化状态，用于后续恢复预览前的显示。
    private void CachePreviewState(Cell c)
    {
        if (c == null || c.visual == null) return;

        SpriteRenderer sr = c.visual.GetComponent<SpriteRenderer>();
        Renderer rend = c.visual.GetComponent<Renderer>();

        PreviewVisualState state = new PreviewVisualState();
        state.cell = c;
        state.localScale = c.visual.transform.localScale;
        state.hasSpriteRenderer = sr != null;
        state.hasRenderer = rend != null;
        state.sprite = sr != null ? sr.sprite : null;
        state.spriteColor = sr != null ? sr.color : Color.white;
        state.rendererColor = rend != null ? rend.material.color : Color.white;

        previewStates.Add(state);
    }

    // 将单个格子的显示还原为缓存时的状态。
    private void RestorePreviewState(PreviewVisualState state)
    {
        if (state == null || state.cell == null || state.cell.visual == null) return;

        if (state.hasSpriteRenderer)
        {
            SpriteRenderer sr = state.cell.visual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = state.sprite;
                sr.color = state.spriteColor;
            }
        }

        if (state.hasRenderer)
        {
            Renderer rend = state.cell.visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = state.rendererColor;
            }
        }

        state.cell.visual.transform.localScale = state.localScale;
    }

    // 在不改变原透明度的前提下，为格子叠加预览颜色效果。
    private void ApplyOverlayColor(Cell c, Color overlay)
    {
        if (c == null || c.visual == null) return;

        SpriteRenderer sr = c.visual.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color baseColor = sr.color;
            Color mixed = Color.Lerp(baseColor, new Color(overlay.r, overlay.g, overlay.b, baseColor.a), overlay.a);
            mixed.a = baseColor.a;
            sr.color = mixed;
        }

        Renderer rend = c.visual.GetComponent<Renderer>();
        if (rend != null)
        {
            Color baseColor = rend.material.color;
            Color mixed = Color.Lerp(baseColor, new Color(overlay.r, overlay.g, overlay.b, baseColor.a), overlay.a);
            mixed.a = baseColor.a;
            rend.material.color = mixed;
        }
    }


    // 按建筑占地切分源贴图并设置到对应格子，同时调整显示缩放。
    private bool TrySetBuildingCellSprite(Cell c, Sprite sourceSprite, int roomX, int roomY, int relativeX, int relativeY)
    {
        if (c == null || c.visual == null || sourceSprite == null || roomX <= 0 || roomY <= 0) return false;

        SpriteRenderer sr = c.visual.GetComponent<SpriteRenderer>();
        if (sr == null) return false;

        Rect srcRect = sourceSprite.rect;
        Texture2D tex = sourceSprite.texture;
        if (tex == null) return false;

        const int partWidth = 128;
        const int partHeight = 135;

        if (srcRect.width < partWidth * roomX || srcRect.height < partHeight * roomY) return false;

        float px = srcRect.x + relativeX * partWidth;
        float py = srcRect.y + relativeY * partHeight;
        Rect partRect = new Rect(px, py, partWidth, partHeight);

        Sprite partSprite = Sprite.Create(tex, partRect, new Vector2(0.5f, 0.5f), sourceSprite.pixelsPerUnit);
        sr.sprite = partSprite;

        float partWorldWidth = partWidth / Mathf.Max(0.0001f, sourceSprite.pixelsPerUnit);
        float partWorldHeight = partHeight / Mathf.Max(0.0001f, sourceSprite.pixelsPerUnit);
        Vector3 scale = c.visual.transform.localScale;
        scale.x = cellManager.cellSize.x / Mathf.Max(0.0001f, partWorldWidth);
        scale.y = cellManager.cellSize.y / Mathf.Max(0.0001f, partWorldHeight);
        c.visual.transform.localScale = scale;

        sr.color = Color.white;
        return true;
    }



    // 直接修改格子渲染器颜色，用于错误高亮等场景。
    private void SetCellColor(Cell c, Color color)
    {
        if (c == null || c.visual == null) return;


        Renderer rend = c.visual.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = color;
        }
    }

    #endregion
}
