using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Building
{
    // 建筑物序号
    public int id;

    // 建筑物名称
    public string Name;

    // 建筑占用格子宽、高
    public int RoomX;
    public int RoomY;

    // 建造消耗
    public int magicToBuild;

    // 消耗
    public int magicConsume;
    public int nutrientConsume;
    public int fruitConsume;

    // 生产
    public int magicProduce;
    public int nutrientProduce;
    public int fruitProduce;

    // 产出周期（秒）
    public float timeToProduce;

    // 员工需求
    public int workersToRun;

    // 效率增益属性
    public string capabilityToEnhanceEfficiency;

    // 文案
    public string text;

    // 分类/类型
    public string type;

    #region 构造函数
    // 默认构造函数
    public Building()
    {
        id = -1;
        Name = string.Empty;
        RoomX = 1;
        RoomY = 1;
        magicToBuild = 0;
        magicConsume = 0;
        nutrientConsume = 0;
        fruitConsume = 0;
        magicProduce = 0;
        nutrientProduce = 0;
        fruitProduce = 0;
        timeToProduce = 0;
        workersToRun = 0;
        capabilityToEnhanceEfficiency = string.Empty;
        text = string.Empty;
        type = string.Empty;
    }
    #endregion
}

[Serializable]
public class BuildingListWrapper
{
    public List<Building> buildings;
}

public class BuilidingPool : MonoBehaviour
{
    #region JSON配置
    [Header("Source JSON (wrapped form: {\"buildings\":[...]})")]
    public TextAsset buildingJson;
    #endregion

    // 存放已加载的建筑列表
    public List<Building> buildings = new List<Building>();

    #region 生命周期
    // 启动时尝试载入建筑数据
    private void Start()
    {
        LoadJsonToPool();
    }
    #endregion

    #region 建筑查询
    // 用于外部一次性设置所有建筑
    public void SetBuildings(List<Building> list)
    {
        buildings = list ?? new List<Building>();
    }

    // 按建筑Id获取建筑
    public Building GetBuildingById(int id)
    {
        return buildings.Find(b => b != null && b.id == id);
    }
    #endregion

    #region 建筑展示信息
    // 获取建筑展示文本（优先使用text字段）
    public string GetBuildingDisplayText(Building building)
    {
        if (building == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(building.text))
        {
            return building.text;
        }

        return building.Name;
    }

    // 按建筑Id获取展示文本
    public string GetBuildingDisplayTextById(int id)
    {
        return GetBuildingDisplayText(GetBuildingById(id));
    }
    #endregion

    #region JSON加载
    // 从 inspector 指定的 TextAsset 读取建筑数据并写入池
    public void LoadJsonToPool()
    {
        if (buildingJson == null)
        {
            Debug.LogWarning("No buildingJson assigned in inspector.");
            return;
        }

        try
        {
            string text = buildingJson.text ?? string.Empty;
            string trimmed = text.TrimStart();
            string toParse = trimmed.StartsWith("[") ? ("{\"buildings\":" + text + "}") : text;

            var wrapper = JsonUtility.FromJson<BuildingListWrapper>(toParse);
            if (wrapper != null && wrapper.buildings != null)
            {
                SetBuildings(wrapper.buildings);
                Debug.Log($"Loaded {wrapper.buildings.Count} buildings into pool.");
            }
            else
            {
                Debug.LogWarning("Parsed JSON but found no buildings.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse building JSON: {ex.Message}");
        }
    }
    #endregion
}
