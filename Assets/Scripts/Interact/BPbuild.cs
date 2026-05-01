using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPbuild : MonoBehaviour
{
    public BuildingPanelWiget buildingPanelWiget;
    public CellManager cellManager;
    public Camera mainCamera;

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

    // 每帧处理选中建筑、鼠标格子检测、预览显示与点击建造逻辑。
    void Update()
    {
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
