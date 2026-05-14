using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InformationPanel : MonoBehaviour
{
    public Text infoText;
    public TimeLine timeLine;
    public BuilidingPool buildingPool;

    private BaseData baseData;
    private float refreshTimer;

    private const string CafeteriaBuildingId = "701";
    private const string KitchenBuildingId = "801";

    #region 生命周期
    // 初始化并刷新显示。
    private void Start()
    {
        EnsureReferences();
        RefreshInfo();
    }

    // 定时刷新信息面板显示。
    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < 0.5f)
        {
            return;
        }

        refreshTimer = 0f;
        RefreshInfo();
    }
    #endregion

    #region 主显示逻辑

    // 刷新面板文本内容。
    private void RefreshInfo()
    {
        EnsureReferences();
        if (infoText == null)
        {
            return;
        }

        int magicConsume;
        int nutrientConsume;
        int fruitConsume;
        int magicProduce;
        int nutrientProduce;
        int fruitProduce;
        CalculateDailyProduction(out magicConsume, out nutrientConsume, out fruitConsume, out magicProduce, out nutrientProduce, out fruitProduce);

        int totalFoodNeed;
        int actualFoodNeed;
        CalculateDailyFoodNeed(out totalFoodNeed, out actualFoodNeed);

        int shushuCount = GetShushuCount();
        int maxShushu = baseData != null ? baseData.MaxShuShu : 0;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("每日理论消耗量/产出量");
        builder.AppendLine(string.Format("自然能量：{0}/{1}", magicConsume, magicProduce));
        builder.AppendLine(string.Format("根系能量：{0}/{1}", nutrientConsume, nutrientProduce));
        builder.AppendLine(string.Format("果实能量：{0}/{1}", fruitConsume, fruitProduce));
        builder.AppendLine();
        builder.AppendLine("每日吃饭需求");
        builder.AppendLine(string.Format("理论 {0} / 实际 {1}", totalFoodNeed, actualFoodNeed));
        builder.AppendLine(string.Format("鼠鼠数量：{0}/{1}", shushuCount, maxShushu));

        infoText.text = builder.ToString();
    }

    #endregion

    #region 理论生产计算

    // 计算每日理论消耗与产出。
    private void CalculateDailyProduction(out int magicConsume, out int nutrientConsume, out int fruitConsume,
        out int magicProduce, out int nutrientProduce, out int fruitProduce)
    {
        magicConsume = 0;
        nutrientConsume = 0;
        fruitConsume = 0;
        magicProduce = 0;
        nutrientProduce = 0;
        fruitProduce = 0;

        if (baseData == null || timeLine == null || buildingPool == null)
        {
            return;
        }

        List<Room> roomList = baseData.GetBlackboardValue(BaseData.BlackboardKeys.RoomList, baseData.roomList);
        if (roomList == null)
        {
            return;
        }

        float dayDuration = timeLine.dayDuration;
        List<Shushu> shushuList = baseData.GetBlackboardValue(BaseData.BlackboardKeys.ShushuList, baseData.shushuList);

        for (int i = 0; i < roomList.Count; i++)
        {
            Room room = roomList[i];
            if (room == null || string.IsNullOrEmpty(room.instanceId))
            {
                continue;
            }

            if (room.isProductionPaused)
            {
                continue;
            }

            if (IsGrowthRoom(room))
            {
                continue;
            }

            Building building = GetBuilding(room);
            if (building == null || building.timeToProduce <= 0f)
            {
                continue;
            }

            if (!HasEnoughWorkers(shushuList, room, building))
            {
                continue;
            }

            if (HasHungryWorker(shushuList, room))
            {
                continue;
            }

            int cycles = Mathf.FloorToInt(dayDuration / building.timeToProduce);
            if (cycles <= 0)
            {
                continue;
            }

            float efficiency = CalculateProductionEfficiencyMultiplier(shushuList, room, building);
            int perMagicProduce = Mathf.RoundToInt(building.magicProduce * efficiency);
            int perNutrientProduce = Mathf.RoundToInt(building.nutrientProduce * efficiency);
            int perFruitProduce = Mathf.RoundToInt(building.fruitProduce * efficiency);

            magicConsume += cycles * building.magicConsume;
            nutrientConsume += cycles * building.nutrientConsume;
            fruitConsume += cycles * building.fruitConsume;

            magicProduce += cycles * perMagicProduce;
            nutrientProduce += cycles * perNutrientProduce;
            fruitProduce += cycles * perFruitProduce;
        }
    }

    // 判断是否为成长类房间。
    private bool IsGrowthRoom(Room room)
    {
        if (room == null || string.IsNullOrEmpty(room.buildingId))
        {
            return false;
        }

        return room.buildingId == "601" || room.buildingId == "602" || room.buildingId == "603";
    }

    // 判断房间分配人数是否满足建筑要求。
    private bool HasEnoughWorkers(List<Shushu> shushuList, Room room, Building building)
    {
        if (building == null || building.workersToRun <= 0)
        {
            return true;
        }

        if (room == null || room.shushuIds == null || shushuList == null)
        {
            return false;
        }

        int validCount = 0;
        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            if (FindShushuById(shushuList, room.shushuIds[i]) != null)
            {
                validCount++;
            }
        }

        return validCount >= building.workersToRun;
    }

    // 判断房间内是否存在饥饿鼠鼠。
    private bool HasHungryWorker(List<Shushu> shushuList, Room room)
    {
        if (room == null || room.shushuIds == null || shushuList == null)
        {
            return false;
        }

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(shushuList, room.shushuIds[i]);
            if (shu != null && shu.isHungry)
            {
                return true;
            }
        }

        return false;
    }

    // 计算产出效率倍率。
    private float CalculateProductionEfficiencyMultiplier(List<Shushu> shushuList, Room room, Building building)
    {
        if (room == null || building == null || room.shushuIds == null || shushuList == null)
        {
            return 1f;
        }

        float totalMultiplier = 1f;
        string capability = building.capabilityToEnhanceEfficiency;

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(shushuList, room.shushuIds[i]);
            if (shu == null)
            {
                continue;
            }

            int value = GetCapabilityValue(shu, capability);
            totalMultiplier *= GetSingleShuEfficiencyMultiplier(value);
        }

        return totalMultiplier;
    }

    // 读取鼠鼠对应能力值。
    private int GetCapabilityValue(Shushu shu, string capability)
    {
        if (shu == null || string.IsNullOrEmpty(capability))
        {
            return 0;
        }

        string key = NormalizeCapabilityKey(capability);
        if (key == "endurance")
        {
            return shu.endurance;
        }

        if (key == "intelligence")
        {
            return shu.intelligence;
        }

        if (key == "magicpower")
        {
            return shu.magicPower;
        }

        return 0;
    }

    // 统一规范化能力字段，避免配置格式影响匹配。
    private string NormalizeCapabilityKey(string capability)
    {
        if (string.IsNullOrEmpty(capability))
        {
            return string.Empty;
        }

        string key = capability.Trim().ToLowerInvariant();
        key = key.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        return key;
    }

    // 单只鼠鼠能力值转换为产出倍率。
    private float GetSingleShuEfficiencyMultiplier(int capabilityValue)
    {
        if (capabilityValue == 1)
        {
            return 0.9f;
        }

        if (capabilityValue == 2)
        {
            return 0.95f;
        }

        if (capabilityValue >= 3)
        {
            return 1f + (capabilityValue - 3) * 0.01f;
        }

        return 1f;
    }

    // 按 buildingId 获取建筑配置。
    private Building GetBuilding(Room room)
    {
        if (room == null || buildingPool == null)
        {
            return null;
        }

        int id;
        if (!int.TryParse(room.buildingId, out id))
        {
            return null;
        }

        return buildingPool.GetBuildingById(id);
    }

    // 按 Id 查找鼠鼠对象。
    private Shushu FindShushuById(List<Shushu> shushuList, string id)
    {
        if (shushuList == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < shushuList.Count; i++)
        {
            Shushu shu = shushuList[i];
            if (shu != null && shu.Id == id)
            {
                return shu;
            }
        }

        return null;
    }

    #endregion

    #region 吃饭需求统计

    // 计算当天吃饭需求与增益后的实际需求。
    private void CalculateDailyFoodNeed(out int totalNeed, out int actualNeed)
    {
        totalNeed = 0;
        actualNeed = 0;

        if (baseData == null)
        {
            return;
        }

        List<Shushu> shushuList = baseData.GetBlackboardValue(BaseData.BlackboardKeys.ShushuList, baseData.shushuList);
        if (shushuList == null)
        {
            return;
        }

        for (int i = 0; i < shushuList.Count; i++)
        {
            Shushu shu = shushuList[i];
            if (shu == null)
            {
                continue;
            }

            totalNeed += Mathf.Max(0, shu.foodIntake);
        }

        int bonus = CalculateFoodReductionBonus(shushuList, totalNeed);
        actualNeed = Mathf.Max(0, totalNeed - bonus);
    }

    // 计算厨房与食堂带来的吃饭减耗。
    private int CalculateFoodReductionBonus(List<Shushu> shushuList, int totalNeed)
    {
        if (shushuList == null || baseData == null)
        {
            return 0;
        }

        bool hasActiveKitchen = false;
        int cafeteriaCount = 0;

        List<Room> roomList = baseData.GetBlackboardValue(BaseData.BlackboardKeys.RoomList, baseData.roomList);
        if (roomList != null)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                Room room = roomList[i];
                if (room == null)
                {
                    continue;
                }

                if (room.buildingId == KitchenBuildingId)
                {
                    if (!hasActiveKitchen && HasWorkingShushuInRoom(shushuList, room))
                    {
                        hasActiveKitchen = true;
                    }
                }
                else if (room.buildingId == CafeteriaBuildingId)
                {
                    cafeteriaCount++;
                }
            }
        }

        int kitchenBonus = hasActiveKitchen ? Mathf.FloorToInt(totalNeed * 0.05f) : 0;
        int cafeteriaBonus = cafeteriaCount * 12;
        return Mathf.Max(0, kitchenBonus + cafeteriaBonus);
    }

    // 判断房间是否有分配鼠鼠。
    private bool HasWorkingShushuInRoom(List<Shushu> shushuList, Room room)
    {
        if (room == null || room.shushuIds == null || shushuList == null)
        {
            return false;
        }

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            if (FindShushuById(shushuList, room.shushuIds[i]) != null)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 数据获取

    // 获取必要引用。
    private void EnsureReferences()
    {
        if (baseData == null)
        {
            baseData = BaseData.instance;
        }

        if (timeLine == null)
        {
            timeLine = FindObjectOfType<TimeLine>();
        }

        if (buildingPool == null && timeLine != null)
        {
            buildingPool = timeLine.buildingPool;
        }

        if (buildingPool == null)
        {
            buildingPool = FindObjectOfType<BuilidingPool>();
        }


    }

    // 获取鼠鼠数量。
    private int GetShushuCount()
    {
        if (baseData == null)
        {
            return 0;
        }

        List<Shushu> shushuList = baseData.GetBlackboardValue(BaseData.BlackboardKeys.ShushuList, baseData.shushuList);
        return shushuList != null ? shushuList.Count : 0;
    }

    #endregion
}
