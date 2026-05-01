using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public enum TaskConditionType
{
    None = 0,
    BuildingCountAtLeast,
    AnyBuildingInGroupAtLeast,
    ShushuCountAtLeast,
    CurrentResourceAtLeast,
    AccumulatedResourceAtLeast,
    ShushuAnyStatAtLeast,
    ShushuAnyStatEquals,
    WallNutTotalAtLeast,
    WallNutUniqueTypeAtLeast
}

public enum TaskResourceType
{
    NatureEnergy = 1,
    RootEnergy = 2,
    FruitEnergy = 3,
    ExpandSpace = 4
}

[System.Serializable]
public class TaskCondition
{
    public TaskConditionType conditionType;
    public string key;
    public List<string> keyGroup = new List<string>();
    public int targetValue;
}

[System.Serializable]
public class TaskReward
{
    public TaskResourceType rewardType;
    public int amount;
}

[System.Serializable]
public class TaskDefinition
{
    public int id;
    public string taskName;
    public string description;
    public List<TaskCondition> conditions = new List<TaskCondition>();
    public List<TaskReward> rewards = new List<TaskReward>();
}

[System.Serializable]
public class TaskJsonWrapper
{
    public List<TaskDefinition> taskDefinitions = new List<TaskDefinition>();
}

public class TaskManager : MonoBehaviour
{

    [Header("任务定义")]
    public List<TaskDefinition> taskDefinitions = new List<TaskDefinition>();

    [Header("累计资源")]
    public int totalNatureEnergyEarned;
    public int totalRootEnergyEarned;
    public int totalFruitEnergyEarned;

    [Header("大树升级")]
    public CellManager cellManager;

    private bool hasResourceSnapshot;
    private int lastNatureEnergy;
    private int lastRootEnergy;
    private int lastFruitEnergy;

    private int currentTaskIndex = -1;
    private bool currentTaskCanClaim;

    public Action onTaskStateChanged;
    public Action<int> onExpandSpaceRewardGranted;

    #region 生命周期

    // 初始化任务数据：优先从TaskJson加载，失败则回退到代码内置任务
    private void Awake()
    {
        if (cellManager == null)
        {
            cellManager = FindObjectOfType<CellManager>();
        }

        LoadTaskDefinitions();
        InitializeTaskFlow();
    }

    private void Update()
    {
        UpdateAccumulatedResources();
    }

    #endregion

    #region 任务配置加载

    // 从Resources/Json/TaskJson.json读取任务数据
    private void LoadTaskDefinitions()
    {
        taskDefinitions.Clear();

        string json = LoadTaskJsonContent();
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        TaskJsonWrapper wrapper = JsonUtility.FromJson<TaskJsonWrapper>(json);
        if (wrapper == null || wrapper.taskDefinitions == null)
        {
            return;
        }

        taskDefinitions = wrapper.taskDefinitions;
    }

    // 读取TaskJson文本内容
    private string LoadTaskJsonContent()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Json/TaskJson");
        if (textAsset != null)
        {
            return textAsset.text;
        }

        string filePath = Path.Combine(Application.dataPath, "Resources/Json/TaskJson.json");
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        return string.Empty;
    }

    #endregion

    #region 任务流程与UI接口

    // 初始化线性任务流
    private void InitializeTaskFlow()
    {
        currentTaskIndex = taskDefinitions.Count > 0 ? 0 : -1;
        currentTaskCanClaim = false;
        UpdateCurrentTaskCompletion();
    }

    // 持续检测当前任务是否可收取
    private void UpdateCurrentTaskCompletion()
    {
        bool newCanClaim = IsCurrentTaskCompleted();
        if (newCanClaim == currentTaskCanClaim)
        {
            return;
        }

        currentTaskCanClaim = newCanClaim;
        NotifyTaskStateChanged();
    }

    // 手动刷新当前任务完成状态（供UI打开时调用）
    public void RefreshCurrentTaskCompletion()
    {
        UpdateCurrentTaskCompletion();
    }

    // 作弊：将当前任务直接标记为可收取。
    public void CheatCompleteCurrentTask()
    {
        if (!HasCurrentTask() || currentTaskCanClaim)
        {
            return;
        }

        currentTaskCanClaim = true;
        NotifyTaskStateChanged();
    }

    // 获取当前发布中的任务
    public TaskDefinition GetCurrentTask()
    {
        if (!HasCurrentTask())
        {
            return null;
        }

        return taskDefinitions[currentTaskIndex];
    }

    // 当前是否还有可发布任务
    public bool HasCurrentTask()
    {
        return currentTaskIndex >= 0 && currentTaskIndex < taskDefinitions.Count;
    }

    // 当前任务是否已完成可收取
    public bool CanClaimCurrentTask()
    {
        return currentTaskCanClaim;
    }

    // 获取当前任务序号（从0开始）
    public int GetCurrentTaskIndex()
    {
        return currentTaskIndex;
    }

    // 玩家点击收取当前任务奖励
    public bool TryClaimCurrentTask()
    {
        if (!HasCurrentTask() || !currentTaskCanClaim)
        {
            return false;
        }

        TaskDefinition task = taskDefinitions[currentTaskIndex];
        ApplyTaskRewards(task);

        currentTaskIndex += 1;
        currentTaskCanClaim = false;
        UpdateCurrentTaskCompletion();
        NotifyTaskStateChanged();
        return true;
    }

    // 当前任务是否满足全部条件
    private bool IsCurrentTaskCompleted()
    {
        TaskDefinition task = GetCurrentTask();
        return IsTaskCompleted(task);
    }

    // 判定单个任务是否完成（无条件任务视为天然完成）
    public bool IsTaskCompleted(TaskDefinition task)
    {
        if (task == null)
        {
            return false;
        }

        if (task.conditions == null || task.conditions.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < task.conditions.Count; i++)
        {
            if (!CheckCondition(task.conditions[i]))
            {
                return false;
            }
        }

        return true;
    }

    // 统一条件判定入口
    public bool CheckCondition(TaskCondition condition)
    {
        if (condition == null)
        {
            return false;
        }

        switch (condition.conditionType)
        {
            case TaskConditionType.BuildingCountAtLeast:
                return CheckBuildingCountCondition(condition);
            case TaskConditionType.AnyBuildingInGroupAtLeast:
                return CheckAnyBuildingGroupCondition(condition);
            case TaskConditionType.ShushuCountAtLeast:
                return CheckShushuCountCondition(condition);
            case TaskConditionType.CurrentResourceAtLeast:
                return CheckCurrentResourceCondition(condition);
            case TaskConditionType.AccumulatedResourceAtLeast:
                return CheckAccumulatedResourceCondition(condition);
            case TaskConditionType.ShushuAnyStatAtLeast:
                return CheckShushuAnyStatCondition(condition);
            case TaskConditionType.ShushuAnyStatEquals:
                return CheckShushuAnyStatCondition(condition);
            case TaskConditionType.WallNutTotalAtLeast:
                return CheckWallNutTotalCondition(condition);
            case TaskConditionType.WallNutUniqueTypeAtLeast:
                return CheckWallNutUniqueTypeCondition(condition);
            default:
                return false;
        }
    }

    // 发放任务奖励（先支持资源与扩建回调）
    private void ApplyTaskRewards(TaskDefinition task)
    {
        if (task == null || task.rewards == null)
        {
            return;
        }

        BaseData data = BaseData.instance;
        for (int i = 0; i < task.rewards.Count; i++)
        {
            TaskReward reward = task.rewards[i];
            if (reward == null)
            {
                continue;
            }

            switch (reward.rewardType)
            {
                case TaskResourceType.NatureEnergy:
                    if (data != null)
                    {
                        int current = data.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, data.natureEnergy);
                        data.SetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, current + reward.amount);
                    }
                    break;
                case TaskResourceType.RootEnergy:
                    if (data != null)
                    {
                        int current = data.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, data.rootEnergy);
                        data.SetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, current + reward.amount);
                    }
                    break;
                case TaskResourceType.FruitEnergy:
                    if (data != null)
                    {
                        int current = data.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, data.fruitEnergy);
                        data.SetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, current + reward.amount);
                    }
                    break;
                case TaskResourceType.ExpandSpace:
                    HandleExpandSpaceReward(reward.amount);
                    break;
            }
        }
    }

    // 处理大树升级奖励（rewardType=4）。
    private void HandleExpandSpaceReward(int targetLevel)
    {
        if (cellManager == null)
        {
            cellManager = FindObjectOfType<CellManager>();
        }

        if (cellManager != null)
        {
            cellManager.UpgradeTreeSpace(targetLevel);
        }

        if (onExpandSpaceRewardGranted != null)
        {
            onExpandSpaceRewardGranted(targetLevel);
        }
    }

    // 通知UI刷新
    private void NotifyTaskStateChanged()
    {
        if (onTaskStateChanged != null)
        {
            onTaskStateChanged();
        }
    }

    #endregion

    #region 累计资源统计

    // 通过“当前值差量”更新累计获得资源（只累计正向增长，不计算消耗）
    private void UpdateAccumulatedResources()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return;
        }

        if (!hasResourceSnapshot)
        {
            lastNatureEnergy = data.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, data.natureEnergy);
            lastRootEnergy = data.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, data.rootEnergy);
            lastFruitEnergy = data.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, data.fruitEnergy);
            hasResourceSnapshot = true;
            return;
        }

        int currentNature = data.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, data.natureEnergy);
        int currentRoot = data.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, data.rootEnergy);
        int currentFruit = data.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, data.fruitEnergy);
        int deltaNature = currentNature - lastNatureEnergy;
        int deltaRoot = currentRoot - lastRootEnergy;
        int deltaFruit = currentFruit - lastFruitEnergy;

        if (deltaNature > 0)
        {
            totalNatureEnergyEarned += deltaNature;
        }

        if (deltaRoot > 0)
        {
            totalRootEnergyEarned += deltaRoot;
        }

        if (deltaFruit > 0)
        {
            totalFruitEnergyEarned += deltaFruit;
        }

        lastNatureEnergy = currentNature;
        lastRootEnergy = currentRoot;
        lastFruitEnergy = currentFruit;
    }

    #endregion

    #region 建筑相关判定

    // 获取某建筑数量（key为BuildingId字符串，如"501"）
    public int GetBuildingCount(string buildingKey)
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return 0;
        }

        List<Room> roomList = data.GetBlackboardValue(BaseData.BlackboardKeys.RoomList, data.roomList);
        if (roomList == null)
        {
            return 0;
        }

        int targetBuildingId;
        if (!int.TryParse(buildingKey, out targetBuildingId))
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < roomList.Count; i++)
        {
            Room room = roomList[i];
            if (room == null)
            {
                continue;
            }

            int roomBuildingId;
            if (!int.TryParse(room.buildingId, out roomBuildingId))
            {
                continue;
            }

            if (roomBuildingId == targetBuildingId)
            {
                count += 1;
            }
        }

        return count;
    }

    // 获取任意建筑组总量（keyGroup为BuildingId字符串集合）
    public int GetAnyBuildingInGroupCount(List<string> buildingKeyGroup)
    {
        if (buildingKeyGroup == null || buildingKeyGroup.Count == 0)
        {
            return 0;
        }

        List<string> uniqueBuildingKeys = new List<string>();
        for (int i = 0; i < buildingKeyGroup.Count; i++)
        {
            string buildingKey = buildingKeyGroup[i];
            if (string.IsNullOrEmpty(buildingKey))
            {
                continue;
            }

            int id;
            if (!int.TryParse(buildingKey, out id))
            {
                continue;
            }

            if (!uniqueBuildingKeys.Contains(buildingKey))
            {
                uniqueBuildingKeys.Add(buildingKey);
            }
        }

        if (uniqueBuildingKeys.Count == 0)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < uniqueBuildingKeys.Count; i++)
        {
            count += GetBuildingCount(uniqueBuildingKeys[i]);
        }

        return count;
    }

    // 判定“拥有/建造指定建筑数量”是否达成
    public bool CheckBuildingCountCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.BuildingCountAtLeast)
        {
            return false;
        }

        return GetBuildingCount(condition.key) >= condition.targetValue;
    }

    // 判定“任意建筑组数量”是否达成
    public bool CheckAnyBuildingGroupCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.AnyBuildingInGroupAtLeast)
        {
            return false;
        }

        return GetAnyBuildingInGroupCount(condition.keyGroup) >= condition.targetValue;
    }

    #endregion

    #region 资源判定

    // 判定“累计获得资源”类型条件是否达成（用于15/16/17任务）
    public bool CheckAccumulatedResourceCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.AccumulatedResourceAtLeast)
        {
            return false;
        }

        TaskResourceType resourceType;
        if (!TryParseTaskResourceType(condition.key, out resourceType))
        {
            return false;
        }

        return GetAccumulatedResource(resourceType) >= condition.targetValue;
    }

    // 判定“当前资源至少为X”条件
    public bool CheckCurrentResourceCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.CurrentResourceAtLeast)
        {
            return false;
        }

        TaskResourceType resourceType;
        if (!TryParseTaskResourceType(condition.key, out resourceType))
        {
            return false;
        }

        return GetCurrentResource(resourceType) >= condition.targetValue;
    }

    // 将条件里的key（如NatureEnergy）转换为资源类型
    private bool TryParseTaskResourceType(string key, out TaskResourceType resourceType)
    {
        resourceType = TaskResourceType.NatureEnergy;

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        // 使用黑板键名做统一映射，避免字符串散落。
        switch (key)
        {
            case BaseData.BlackboardKeys.NatureEnergy:
                resourceType = TaskResourceType.NatureEnergy;
                return true;
            case BaseData.BlackboardKeys.RootEnergy:
                resourceType = TaskResourceType.RootEnergy;
                return true;
            case BaseData.BlackboardKeys.FruitEnergy:
                resourceType = TaskResourceType.FruitEnergy;
                return true;
            default:
                return false;
        }
    }


    // 获取当前资源值（自然能量/养分/果实）
    public int GetCurrentResource(TaskResourceType resourceType)
    {
        return GetResourceValue(resourceType, false);
    }

    // 获取累计资源值（15/16/17任务依赖）
    public int GetAccumulatedResource(TaskResourceType resourceType)
    {
        return GetResourceValue(resourceType, true);
    }

    // 获取资源值：支持当前值和累计值两类来源
    private int GetResourceValue(TaskResourceType resourceType, bool useAccumulated)
    {
        BaseData data = BaseData.instance;
        if (!useAccumulated && data == null)
        {
            return 0;
        }

        switch (resourceType)
        {
            case TaskResourceType.NatureEnergy:
                return useAccumulated ? totalNatureEnergyEarned : data.GetBlackboardValue(BaseData.BlackboardKeys.NatureEnergy, data.natureEnergy);
            case TaskResourceType.RootEnergy:
                return useAccumulated ? totalRootEnergyEarned : data.GetBlackboardValue(BaseData.BlackboardKeys.RootEnergy, data.rootEnergy);
            case TaskResourceType.FruitEnergy:
                return useAccumulated ? totalFruitEnergyEarned : data.GetBlackboardValue(BaseData.BlackboardKeys.FruitEnergy, data.fruitEnergy);
            default:
                return 0;
        }
    }

    #endregion

    #region 鼠鼠判定



    // 获取当前鼠鼠数量
    public int GetShushuCount()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return 0;
        }

        List<Shushu> shushuList = data.GetBlackboardValue(BaseData.BlackboardKeys.ShushuList, data.shushuList);
        return shushuList == null ? 0 : shushuList.Count;
    }

    // 判定“鼠鼠数量至少为X”条件
    public bool CheckShushuCountCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.ShushuCountAtLeast)
        {
            return false;
        }

        return GetShushuCount() >= condition.targetValue;
    }

    // 判断是否存在满足“任一属性>=x”的鼠鼠
    public bool HasShushuAnyStatAtLeast(int value)
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return false;
        }

        List<Shushu> shushuList = data.GetBlackboardValue(BaseData.BlackboardKeys.ShushuList, data.shushuList);
        if (shushuList == null)
        {
            return false;
        }

        for (int i = 0; i < shushuList.Count; i++)
        {
            Shushu shushu = shushuList[i];
            if (shushu == null)
            {
                continue;
            }

            if (shushu.endurance >= value || shushu.magicPower >= value || shushu.intelligence >= value)
            {
                return true;
            }
        }

        return false;
    }

    // 判定“任一属性至少为X/等于X”条件（等于X在当前设计中按至少X处理）
    public bool CheckShushuAnyStatCondition(TaskCondition condition)
    {
        if (condition == null)
        {
            return false;
        }

        if (condition.conditionType != TaskConditionType.ShushuAnyStatAtLeast && condition.conditionType != TaskConditionType.ShushuAnyStatEquals)
        {
            return false;
        }

        return HasShushuAnyStatAtLeast(condition.targetValue);
    }

    #endregion

    #region 收藏品核桃判定

    // 获取已获得收藏品核桃总数
    public int GetWallNutTotalCount()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return 0;
        }

        int[] wallNutNum = data.GetBlackboardValue(BaseData.BlackboardKeys.WallNutNum, data.wallNutNum);
        if (wallNutNum == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < wallNutNum.Length; i++)
        {
            total += wallNutNum[i];
        }

        return total;
    }

    // 获取已获得收藏品核桃种类数
    public int GetWallNutUniqueTypeCount()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return 0;
        }

        int[] wallNutNum = data.GetBlackboardValue(BaseData.BlackboardKeys.WallNutNum, data.wallNutNum);
        if (wallNutNum == null)
        {
            return 0;
        }

        int unique = 0;
        for (int i = 0; i < wallNutNum.Length; i++)
        {
            if (wallNutNum[i] > 0)
            {
                unique += 1;
            }
        }

        return unique;
    }

    // 判定“收藏品核桃总数至少为X”条件
    public bool CheckWallNutTotalCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.WallNutTotalAtLeast)
        {
            return false;
        }

        return GetWallNutTotalCount() >= condition.targetValue;
    }

    // 判定“收藏品核桃种类数至少为X”条件
    public bool CheckWallNutUniqueTypeCondition(TaskCondition condition)
    {
        if (condition == null || condition.conditionType != TaskConditionType.WallNutUniqueTypeAtLeast)
        {
            return false;
        }

        return GetWallNutUniqueTypeCount() >= condition.targetValue;
    }

    #endregion
}
