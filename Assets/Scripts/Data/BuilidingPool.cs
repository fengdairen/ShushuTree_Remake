using System.Collections.Generic;
using UnityEngine;

public class BuilidingPool : MonoBehaviour
{
    // 存放已加载的建筑列表
    public List<Building> buildings = new List<Building>();

    #region 建筑查询

    // 用于外部一次性设置所有建筑
    public void SetBuildings(List<Building> list)
    {
        buildings = list ?? new List<Building>();
    }

    // 简单的查询方法
    public Building GetBuildingById(int id)
    {
        return buildings.Find(b => b != null && b.id == id);
    }
    #endregion

    #region 建筑展示信息
    // 获取建筑展示文本（优先使用text字段）。
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

    // 按建筑Id获取展示文本。
    public string GetBuildingDisplayTextById(int id)
    {
        return GetBuildingDisplayText(GetBuildingById(id));
    }
    #endregion
}
