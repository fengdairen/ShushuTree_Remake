using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Cell
{
    // 树上坐标
    public int treeX;
    public int treeY;

    // 格子状态: 0-未解锁, 1-解锁无建筑, 2-解锁有建筑
    public int state;

    // 当 state == 2 时有意义
    public float relativeAnchorX;
    public float relativeAnchorY;
    public string buildingTag;
    public int buildingInstanceId;

    // 可选的可视化对象
    [HideInInspector]
    public GameObject visual;
}

public class CellManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 15; // X
    public int rows = 8;     // Y
    public Vector2 cellSize = new Vector2(128f, 135f);
    public Vector2 origin = Vector2.zero;

    [Header("Visuals (optional)")]
    public GameObject cellPrefab; // optional prefab to instantiate per cell

    [Header("Tree Level")]
    public int currentTreeLevel = 0;
    public GameObject treeBackground;
    public Sprite tree0;
    public Sprite tree1;
    public Sprite tree2;
    public Sprite tree3;
    public Sprite tree4;
    private Vector2 treeBackgroundBaseSize;
    private bool treeBackgroundBaseSizeInitialized;

    // 底层数据结构
    private Cell[,] grid;
    private int[,] treeLevel0 = {
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1,1,1,1,1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1,1,1,1,1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1,1,1,1,1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1,1,1,1,1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1,1,1,1,1, 0, 0, 0, 0, 0 }
    };
    private int[,] treeLevel1 = {
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 }
    };
    private int[,] treeLevel2 = {
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 }
    };
    private int[,] treeLevel3 = {
    { 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 }
    };

    private int[,] treeLevel4 = {
    { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 }
    };

    void Awake()
    {
        InitializeGrid();
    }

    // 初始化格子数据和可选的可视化对象
    public void InitializeGrid()
    {
        grid = new Cell[columns, rows];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var c = new Cell();
                c.treeX = x;
                c.treeY = y;
                c.state = 0; // default locked
                c.relativeAnchorX = 0f;
                c.relativeAnchorY = 0f;
                c.buildingTag = string.Empty;
                c.buildingInstanceId = -1;

                // 创造实例
                if (cellPrefab != null)
                {
                    Vector3 pos = new Vector3(origin.x + x * cellSize.x, origin.y + y * cellSize.y, 0f);
                    var go = Instantiate(cellPrefab, pos, Quaternion.identity, this.transform);
                    go.name = $"Cell_{x}_{y}";
                    c.visual = go;
                    UpdateVisualForCell(c);
                }

                grid[x, y] = c;
            }
        }
        ApplyTreeLevelUnlock(currentTreeLevel);
        UpdateTreeBackgroundVisual(currentTreeLevel);
              

    }

    #region 大树升级功能

    // 升级大树空间到指定等级（0~4）。
    public bool UpgradeTreeSpace(int targetLevel)
    {
        int clampedLevel = Mathf.Clamp(targetLevel, 0, 4);
        if (clampedLevel <= currentTreeLevel)
        {
            return false;
        }

        currentTreeLevel = clampedLevel;
        UpdateTreeBackgroundVisual(currentTreeLevel);
        ApplyTreeLevelUnlock(currentTreeLevel);
        return true;
    }

    #region 大树背景功能

    // 获取指定等级对应的大树背景贴图。
    private Sprite GetTreeLevelSprite(int level)
    {
        switch (level)
        {
            case 0:
                return tree0;
            case 1:
                return tree1;
            case 2:
                return tree2;
            case 3:
                return tree3;
            case 4:
                return tree4;
            default:
                return null;
        }
    }

    // 更新大树背景贴图，并保持背景显示尺寸一致。
    private void UpdateTreeBackgroundVisual(int level)
    {
        if (treeBackground == null)
        {
            return;
        }

        var spriteRenderer = treeBackground.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite targetSprite = GetTreeLevelSprite(level);
        if (targetSprite == null)
        {
            return;
        }

        bool spriteChanged = spriteRenderer.sprite != targetSprite;

        if (!treeBackgroundBaseSizeInitialized)
        {
            Sprite referenceSprite = spriteRenderer.sprite != null ? spriteRenderer.sprite : targetSprite;
            Vector3 localScale = treeBackground.transform.localScale;
            treeBackgroundBaseSize = new Vector2(
                referenceSprite.bounds.size.x * Mathf.Abs(localScale.x),
                referenceSprite.bounds.size.y * Mathf.Abs(localScale.y));
            treeBackgroundBaseSizeInitialized = true;
        }

        spriteRenderer.sprite = targetSprite;

        Vector2 spriteSize = targetSprite.bounds.size;
        if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 currentScale = treeBackground.transform.localScale;
        float signX = currentScale.x < 0f ? -1f : 1f;
        float signY = currentScale.y < 0f ? -1f : 1f;
        currentScale.x = signX * (treeBackgroundBaseSize.x / spriteSize.x);
        currentScale.y = signY * (treeBackgroundBaseSize.y / spriteSize.y);
        treeBackground.transform.localScale = currentScale;

        // 每次更换大树贴图后，整体向下移动 。
        if (spriteChanged)
        {
            treeBackground.transform.localPosition += new Vector3(0f, -2f, 0f);
        }
    }

    #endregion

    // 按指定等级矩阵解锁格子（只做解锁，不回锁）。
    private void ApplyTreeLevelUnlock(int level)
    {
        int[,] matrix = GetTreeLevelMatrix(level);
        if (matrix == null)
        {
            return;
        }

        int matrixRows = matrix.GetLength(0);
        int matrixCols = matrix.GetLength(1);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                int mx = x;
                int my = (rows - 1) - y;

                if (mx < 0 || my < 0 || mx >= matrixCols || my >= matrixRows)
                {
                    continue;
                }

                if (matrix[my, mx] != 1)
                {
                    continue;
                }

                if (grid[x, y] != null && grid[x, y].state == 0)
                {
                    SetCellState(x, y, 1);
                }
            }
        }
    }

    // 获取指定等级对应的格子解锁矩阵。
    private int[,] GetTreeLevelMatrix(int level)
    {
        switch (level)
        {
            case 0:
                return treeLevel0;
            case 1:
                return treeLevel1;
            case 2:
                return treeLevel2;
            case 3:
                return treeLevel3;
            case 4:
                return treeLevel4;
            default:
                return null;
        }
    }

    #endregion

    // 获取格子数据（安全）
    public Cell GetCell(int x, int y)
    {
        if (!IsValid(x, y)) return null;
        return grid[x, y];
    }

    public bool IsValid(int x, int y)
    {
        return grid != null && x >= 0 && x < columns && y >= 0 && y < rows;
    }

    public bool IsValidtoBuild(int x, int y)
    {
        return IsValid(x, y) && grid[x, y].state == 1;
    }
    // 设置格子状态
    public bool SetCellState(int x, int y, int state)
    {
        if (!IsValid(x, y)) return false;
        var c = grid[x, y];
        c.state = state;

        UpdateVisualForCell(c);
        return true;
    }

    // 根据 state 更新可视化（若存在 Renderer）
    public void UpdateVisualForCell(Cell c)
    {
        if (c == null || c.visual == null) return;
        var rend = c.visual.GetComponent<Renderer>();
        if (rend == null) return;

        switch (c.state)
        {
            case 0: // locked
                rend.material.color = Color.clear;
                break;
            case 1: // unlocked empty
                rend.material.color = new Color(1f, 1f, 1f, 0.5f);
                break;
            case 2: // has building
                rend.material.color = new Color(1f, 1f, 1f, 1f);
                break;
            default:
                rend.material.color = Color.magenta;
                break;
        }
    }

    // 放置建筑（设置相关字段）
    public bool PlaceBuilding(int x, int y, string tag, int instanceId, float relX = 0f, float relY = 0f)
    {
        //这里先不实现
        return true;
    }

    // 清空建筑
    public bool RemoveBuilding(int x, int y)
    {

        //这里先不实现
        return true;
    }
    // 重新绘制边缘格子
    public void RepaintEdgeCell()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                UpdateVisualForCell(grid[x, y]);
            }
        }
    }
}
