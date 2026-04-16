using System.Collections.Generic;
using UnityEngine;

public class TimeLine : MonoBehaviour
{
    public float dayDuration = 150.5f;
    public int totalDays = 10;
    public BuilidingPool buildingPool;

    private float secondAccumulator;
    public int currentDay = 1;
    public int secondInDay;
    private bool isFinished;

    private float gameSeconds;
    public EatFood eatFood;
    public WallNutManager wallNutManager;


    // 每帧累计时间，并按“每秒”驱动一次生产逻辑检测。
    private void Update()
    {
        if (isFinished) return;

        secondAccumulator += Time.deltaTime;
        while (secondAccumulator >= 1f)
        {
            secondAccumulator -= 1f;
            TickOneSecond();
            if (isFinished) break;
        }
    }

    // 推进 1 秒：更新时间、结算产出、执行投料检测。
    private void TickOneSecond()
    {
        gameSeconds += 1f;
        secondInDay += 1;

        if (secondInDay > Mathf.FloorToInt(dayDuration))
        {
            currentDay += 1;
            eatFood.ProcessDailyEat();
            secondInDay = 1;
            if (currentDay > totalDays)
            {
                isFinished = true;
                return;
            }
        }

        var baseData = BaseData.instance;
        if (baseData == null || buildingPool == null || baseData.roomList == null) return;

        ResolveProduction(baseData);
        TryStartProduction(baseData);
    }

    // 结算所有已完成等待时间的生产周期。
    private void ResolveProduction(BaseData baseData)
    {
        for (int i = 0; i < baseData.roomList.Count; i++)
        {
            Room room = baseData.roomList[i];
            if (room == null || string.IsNullOrEmpty(room.instanceId)) continue;

            if (!room.isProducing) continue;
            if (gameSeconds < room.finishAtSecond) continue;

            Building building = GetBuilding(room);
            if (building != null)
            {
                float efficiencyMultiplier = CalculateProductionEfficiencyMultiplier(baseData, room, building);
                int magicOut = Mathf.RoundToInt(building.magicProduce * efficiencyMultiplier);
                int nutrientOut = Mathf.RoundToInt(building.nutrientProduce * efficiencyMultiplier);
                int fruitOut = Mathf.RoundToInt(building.fruitProduce * efficiencyMultiplier);

                baseData.natureEnergy += magicOut;
                baseData.rootEnergy += nutrientOut;
                baseData.fruitEnergy += fruitOut;
                EnqueueTiaoZi(room, "产出");
                TryResolveWallNutDrop(baseData, room);
            }

            room.isProducing = false;
        }
    }

    // 每秒对空闲建筑尝试投料，满足规则才进入生产等待。
    private void TryStartProduction(BaseData baseData)
    {
        float remainingToday = dayDuration - secondInDay;

        for (int i = 0; i < baseData.roomList.Count; i++)
        {
            Room room = baseData.roomList[i];
            if (room == null || string.IsNullOrEmpty(room.instanceId)) continue;

            if (room.isProducing) continue;

            Building building = GetBuilding(room);
            if (building == null || building.timeToProduce <= 0f) continue;

            if (!HasEnoughWorkers(baseData, room, building))
            {
                EnqueuePersistentTiaoZi(room, "等待分配");
                continue;
            }

            if (HasHungryWorker(baseData, room))
            {
                EnqueuePersistentTiaoZi(room, "罢工");
                continue;
            }

            if (remainingToday <3f) continue; 
            if (remainingToday < building.timeToProduce)
            {
                EnqueueTiaoZi(room, "剩余时间不足以生产");
                continue;
            }

            if (!CanConsume(baseData, building))
            {
                EnqueueTiaoZi(room, "投料失败");
                continue;
            }

            Consume(baseData, building);
            room.isProducing = true;
            room.finishAtSecond = gameSeconds + building.timeToProduce;
            if (HasInputConsume(building))
            {
                EnqueueTiaoZi(room, "投料");
            }
            else EnqueueTiaoZi(room, "不需要耗材的虚空投料");
        }
    }

    // 根据 room 的 buildingId 读取建筑配置。
    private Building GetBuilding(Room room)
    {
        if (room == null || buildingPool == null) return null;

        int id;
        if (!int.TryParse(room.buildingId, out id)) return null;
        return buildingPool.GetBuildingById(id);
    }

    // 在建筑产出后尝试触发核桃掉落，并写入跳字反馈。
    private void TryResolveWallNutDrop(BaseData baseData, Room room)
    {
        if (wallNutManager.TryRollOnProduction(baseData,room))
        {
            EnqueueTiaoZi(room, "核桃掉落" );
        }
    }



    // 判断资源是否满足本次投料消耗。
    private bool CanConsume(BaseData baseData, Building building)
    {
        return baseData.natureEnergy >= building.magicConsume
               && baseData.rootEnergy >= building.nutrientConsume
               && baseData.fruitEnergy >= building.fruitConsume;
    }

    // 执行一次投料扣除。
    private void Consume(BaseData baseData, Building building)
    {
        baseData.natureEnergy -= building.magicConsume;
        baseData.rootEnergy -= building.nutrientConsume;
        baseData.fruitEnergy -= building.fruitConsume;
    }

    // 判断该建筑是否存在任意投料消耗。
    private bool HasInputConsume(Building building)
    {
        return building != null && (building.magicConsume != 0 || building.nutrientConsume != 0 || building.fruitConsume != 0);
    }

    // 向房间跳字队列尾部入队一条跳字。
    private void EnqueueTiaoZi(Room room, string text)
    {
        if (room == null || string.IsNullOrEmpty(text)) return;

        if (room.tiaozi == null)
        {
            room.tiaozi = new Queue<string>();
        }

        room.tiaozi.Enqueue(text);
    }

    // 入队长期提示，避免每秒重复堆积同一条文本。
    private void EnqueuePersistentTiaoZi(Room room, string text)
    {
        if (room == null || string.IsNullOrEmpty(text)) return;

        if (room.tiaozi == null)
        {
            room.tiaozi = new Queue<string>();
        }

        if (room.tiaozi.Count > 0 && room.tiaozi.Contains(text))
        {
            return;
        }

        room.tiaozi.Enqueue(text);
    }

    // 检查当前房间分配人数是否达到建筑岗位需求。
    private bool HasEnoughWorkers(BaseData baseData, Room room, Building building)
    {
        if (building == null || building.workersToRun <= 0)
        {
            return true;
        }

        if (room == null || room.shushuIds == null)
        {
            return false;
        }

        int validCount = 0;
        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(baseData, room.shushuIds[i]);
            if (shu != null)
            {
                validCount++;
            }
        }

        return validCount >= building.workersToRun;
    }

    // 检查房间内是否存在饥饿员工。
    private bool HasHungryWorker(BaseData baseData, Room room)
    {
        if (room == null || room.shushuIds == null)
        {
            return false;
        }

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(baseData, room.shushuIds[i]);
            if (shu != null && shu.isHungry)
            {
                return true;
            }
        }

        return false;
    }

    // 按 Id 从仓库查找鼠鼠对象。
    private Shushu FindShushuById(BaseData baseData, string id)
    {
        if (baseData == null || baseData.shushuList == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < baseData.shushuList.Count; i++)
        {
            Shushu shu = baseData.shushuList[i];
            if (shu != null && shu.Id == id)
            {
                return shu;
            }
        }

        return null;
    }

    // 计算一次产出的效率乘区：按建筑配置属性读取每只鼠鼠能力，并将所有鼠鼠倍率累乘。
    private float CalculateProductionEfficiencyMultiplier(BaseData baseData, Room room, Building building)
    {
        if (room == null || building == null || room.shushuIds == null)
        {
            return 1f;
        }

        float totalMultiplier = 1f;
        string capability = building.capabilityToEnhanceEfficiency;

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(baseData, room.shushuIds[i]);
            if (shu == null)
            {
                continue;
            }

            int value = GetCapabilityValue(shu, capability);
            totalMultiplier *= GetSingleShuEfficiencyMultiplier(value);
        }

        return totalMultiplier;
    }

    // 根据建筑配置的 capabilityToEnhanceEfficiency，读取鼠鼠对应能力值。
    private int GetCapabilityValue(Shushu shu, string capability)
    {
        if (shu == null || string.IsNullOrEmpty(capability))
        {
            return 0;
        }

        string key = capability.Trim().ToLowerInvariant();
        if (key == "endurance" )
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

    // 将单只鼠鼠能力值转换为产出倍率：1/2 为减益，3+ 为微量增益。
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
}
