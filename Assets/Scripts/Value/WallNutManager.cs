using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallNutManager : MonoBehaviour
{
    private const int BuffIdForDropBonus = 20;

    private static readonly float[] WalnutWeights =
    {
        20f, 20f, 5f, 5f, 5f, 15f, 7.5f, 7.5f, 4f, 6f, 4f, 1f
    };


    /*
     
1. 元宝核桃：因长相酷似金元宝而受鼠喜爱 20%
2. 尖尖核桃：核桃脑袋怎么尖尖的？ 20%
3. 橡子核桃：这就是橡子吧啊喂 5%
4. 香蕉核桃：那一晚，香蕉和核桃都喝醉了。。。 5%
5. 甜甜圈核桃：谁给他扎穿了？5%
6. 好大的核桃：这个核桃真的好大 15%
7. 白色核桃：其实鼠鼠们觉得这个长得像鸡蛋 7.5%
8. 金色核桃：谁不喜欢金色的小玩意呢 7.5%
9. 金色元宝核桃：我宣布不是金色的元宝核桃被淘汰了  4%
10. 二连核桃：两个核桃长到了一起，看起来像数字8  6%
11. 三连核桃：三个核桃按照000样式长到了一起，这是怎么做到的？4%
12. 三连炫彩核桃：这真的是该长出来的东西吗？1%
     */

    // 建筑完成一次产出后调用：若是407/408则执行核桃掉落检验。
    public bool TryRollOnProduction(BaseData data, Room room)
    {
        if (data == null || room == null)
        {
            return false;
        }

        int buildingId;
        if (!int.TryParse(room.buildingId, out buildingId))
        {
            return false;
        }

        if (buildingId != 407 && buildingId != 408)
        {
            return false;
        }

        float dropChance = CalculateDropChance(data, room);
        if (Random.value * 100f > dropChance)
        {
            return false;
        }

        int wallNutId = RollWeightedWallNutId();
        data.AddWallNutCount(wallNutId);
        return true;
    }

    // 计算本次核桃掉率：基础5% + buff20加成 + 智力加成。
    private float CalculateDropChance(BaseData data, Room room)
    {
        float chance = 5f;

        if (data == null || data.shushuList == null || room == null || room.shushuIds == null)
        {
            return chance;
        }

        for (int i = 0; i < room.shushuIds.Count; i++)
        {
            Shushu shu = FindShushuById(data, room.shushuIds[i]);
            if (shu == null)
            {
                continue;
            }

            if (HasBuff20(shu))
            {
                chance += 5f;
            }

            chance += shu.intelligence - 3;
        }

        return Mathf.Clamp(chance, 0f, 100f);
    }

    // 按注释给定权重，随机出1~12号核桃。
    private int RollWeightedWallNutId()
    {
        float totalWeight = 0f;
        for (int i = 0; i < WalnutWeights.Length; i++)
        {
            totalWeight += WalnutWeights[i];
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < WalnutWeights.Length; i++)
        {
            cumulative += WalnutWeights[i];
            if (roll <= cumulative)
            {
                return i + 1;
            }
        }

        return WalnutWeights.Length;
    }


    // 判断鼠鼠是否拥有20号buff。
    private bool HasBuff20(Shushu shu)
    {
        if (shu == null)
        {
            return false;
        }

        return shu.buffid1 == BuffIdForDropBonus
               || shu.buffid2 == BuffIdForDropBonus
               || shu.buffid3 == BuffIdForDropBonus;
    }

    // 按Id从BaseData中查找鼠鼠。
    private Shushu FindShushuById(BaseData data, string shushuId)
    {
        if (data == null || data.shushuList == null || string.IsNullOrEmpty(shushuId))
        {
            return null;
        }

        for (int i = 0; i < data.shushuList.Count; i++)
        {
            Shushu shu = data.shushuList[i];
            if (shu != null && shu.Id == shushuId)
            {
                return shu;
            }
        }

        return null;
    }
}
