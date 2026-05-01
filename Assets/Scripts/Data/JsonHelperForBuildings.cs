using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingListWrapper
{
    public List<Building> buildings;
}

public class JsonHelperForBuildings : MonoBehaviour
{
    [Header("Source JSON (wrapped form: {\"buildings\":[...]})")]
    public TextAsset buildingJson;

    [Header("Target Pool")]
    public BuilidingPool pool;

    void Start()
    {
        LoadJsonToPool();
    }

    // 仅实现：从 inspector 指定的 TextAsset 读取（包装形式），写入 pool.buildings
    public void LoadJsonToPool()
    {
        if (pool == null)
        {
            Debug.LogError("BuilidingPool is not assigned.");
            return;
        }

        if (buildingJson == null)
        {
            Debug.LogWarning("No buildingJson assigned in inspector.");
            return;
        }

        try
        {
            // Support both wrapped form {"buildings":[...]} and plain array [...]
            string text = buildingJson.text ?? string.Empty;
            string trimmed = text.TrimStart();
            string toParse = trimmed.StartsWith("[") ? ("{\"buildings\":" + text + "}") : text;

            var wrapper = JsonUtility.FromJson<BuildingListWrapper>(toParse);
            if (wrapper != null && wrapper.buildings != null)
            {
                pool.SetBuildings(wrapper.buildings);
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
}
