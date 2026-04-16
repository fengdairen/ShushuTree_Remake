using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatFood : MonoBehaviour
{
    private const string CafeteriaBuildingId = "701";
    private const string KitchenBuildingId = "801";

    public BaseData baseData;

    // 每天结束时调用：按鼠鼠序号逐个吃饭，不够吃则标记饥饿
    public void ProcessDailyEat()
    {
        BaseData data = GetBaseData();
        if (data == null || data.shushuList == null)
        {
            return;
        }

        int originalFoodEnergy = Mathf.Max(0, data.fruitEnergy);
        int remainFoodEnergy = originalFoodEnergy + GetFoodReductionBonus(data);

        for (int i = 0; i < data.shushuList.Count; i++)
        {
            Shushu shushu = data.shushuList[i];
            if (shushu == null)
            {
                continue;
            }

            int needFood = Mathf.Max(0, shushu.foodIntake);
            if (remainFoodEnergy >= needFood)
            {
                remainFoodEnergy -= needFood;
                shushu.isHungry = false;
            }
            else
            {
                shushu.isHungry = true;
            }
        }

        data.fruitEnergy = Mathf.Max(0, Mathf.Min(originalFoodEnergy, remainFoodEnergy));
    }

    // 计算建筑提供的食物减耗（厨房5% + 每个食堂12点）
    private int GetFoodReductionBonus(BaseData data)
    {
        int totalNeedFood = 0;
        for (int i = 0; i < data.shushuList.Count; i++)
        {
            Shushu shushu = data.shushuList[i];
            if (shushu == null)
            {
                continue;
            }

            totalNeedFood += Mathf.Max(0, shushu.foodIntake);
        }

        int kitchenCount = 0;
        int cafeteriaCount = 0;

        if (data.roomList != null)
        {
            for (int i = 0; i < data.roomList.Count; i++)
            {
                Room room = data.roomList[i];
                if (room == null)
                {
                    continue;
                }

                if (room.buildingId == KitchenBuildingId)
                {
                    kitchenCount++;
                }
                else if (room.buildingId == CafeteriaBuildingId)
                {
                    cafeteriaCount++;
                }
            }
        }

        int kitchenBonus = kitchenCount > 0 ? Mathf.FloorToInt(totalNeedFood * 0.05f) : 0;
        int cafeteriaBonus = cafeteriaCount * 12;

        return Mathf.Max(0, kitchenBonus + cafeteriaBonus);
    }

    // 获取BaseData引用（可手动绑定，也可自动取单例）
    private BaseData GetBaseData()
    {
        if (baseData != null)
        {
            return baseData;
        }

        baseData = BaseData.instance;
        return baseData;
    }
}
