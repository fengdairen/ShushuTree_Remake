using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollNewShu : MonoBehaviour
{
    /*   
     招募属性权重（随机数范围1,100）
  1: 12
  2: 17（29）
  3: 30（59）
  4: 17（76）
  5: 11（87）
  6: 6（93）
  7: 3（96）
  8: 2（98）
  9: 1（99）
  10: 1（100）
    */

    private static readonly string[] NamePool =
    {
        "鼠我鸭", "鼠滑羊", "鼠零八", "鼠繁琦", "鼠前", "鼠听君", "鼠月阳", "鼠喜阳", "鼠鸭亭", "鼠宇杰",
        "鼠韦成", "鼠敖", "鼠明辉", "鼠杰瑞", "鼠里绫华", "鼠音未来", "鼠天依", "鼠麻衣", "鼠莉希雅", "鼠蒙",
        "鼠柯克", "鼠条", "鼠片", "鼠塔", "鼠格", "鼠饼", "鼠重八", "鼠谷和人", "鼠大林", "鼠利奈绪",
        "鼠城明日奈", "鼠莱因", "鼠迪乌斯", "鼠琪希", "鼠丽丝", "鼠露菲", "鼠条悟", "鼠艺菡", "鼠曼巴", "鼠拉给木",
        "鼠鼠侠", "鼠川祥子", "鼠早爱音", "鼠松灯", "鼠理员", "鼠行者", "鼠标", "鼠崎爽世", "鼠名立希", "鼠宫妃那",
        "鼠瑟夫", "鼠斯拿", "鼠芹仁菜", "鼠方仗助", "鼠陈露", "鼠嘉豪", "鼠耽任", "鼠贝林", "鼠笑川", "鼠角洲",
        "鼠一萱", "鼠旻钰","鼠喵喵","鼠杰瑞","鼠米奇"
    };

    public Sprite lastGeneratedPhoto;

    // 生成一个随机鼠鼠（六步流程：基础值->HR预留->词条->图片->边界->食量）
    public Shushu GenerateRandomShuShu()
    {
        Shushu newShu = new Shushu();

        // 1) 三个基础值按权重随机
        newShu.endurance = RollBaseStatByWeight();
        newShu.intelligence = RollBaseStatByWeight();
        newShu.magicPower = RollBaseStatByWeight();

        // 2) HR加成（预留）
        ApplyHrBonus(newShu);

        // 3) 词条抽取
        RollBuffs(newShu);

        // 3.5) 应用会改变基础属性的buff
        BuffWord.ApplyChangeAbilityBuffs(newShu);

        // 4) 根据buffid1加载照片
        lastGeneratedPhoto = LoadPhotoByBuffId(newShu.buffid1);

        // 4.5) 抽取不重复名字
        newShu.Name = RollUniqueName();

        // 5) 三维边界处理 1-10
        ClampBaseStats(newShu);

        // 6) 食量计算：基础10 + buff食量改动
        ApplyFoodIntake(newShu, 10);

        return newShu;
    }

    // 按注释中的权重将随机数1-100映射到基础值1-10
    private int RollBaseStatByWeight()
    {
        int roll = Random.Range(1, 101);

        if (roll <= 12) return 1;
        if (roll <= 29) return 2;
        if (roll <= 59) return 3;
        if (roll <= 76) return 4;
        if (roll <= 87) return 5;
        if (roll <= 93) return 6;
        if (roll <= 96) return 7;
        if (roll <= 98) return 8;
        if (roll <= 99) return 9;
        return 10;
    }

    // HR加成预留函数（后续在这里加逻辑）
    private void ApplyHrBonus(Shushu shushu)
    {
        if (shushu == null || BaseData.instance == null || BaseData.instance.roomList == null)
        {
            return;
        }

        int hrRoomWithWorkers = 0;
        int hrIntelligenceSum = 0;

        for (int i = 0; i < BaseData.instance.roomList.Count; i++)
        {
            Room room = BaseData.instance.roomList[i];
            if (room == null || room.buildingId != "101" || room.shushuIds == null)
            {
                continue;
            }

            int roomWorkerCount = 0;
            for (int j = 0; j < room.shushuIds.Count; j++)
            {
                Shushu hrShu = FindShushuById(room.shushuIds[j]);
                if (hrShu == null)
                {
                    continue;
                }

                hrIntelligenceSum += hrShu.intelligence;
                roomWorkerCount++;
            }

            if (roomWorkerCount > 0)
            {
                hrRoomWithWorkers++;
            }
        }

        if (hrRoomWithWorkers <= 0)
        {
            return;
        }

        float firstRate = Mathf.Clamp01((hrIntelligenceSum - (3 * hrRoomWithWorkers) + 2) * 0.1f);
        float secondRate = Mathf.Clamp01((hrIntelligenceSum - (3 * hrRoomWithWorkers)) * 0.05f);
        float thirdRate = Mathf.Clamp01((hrIntelligenceSum - (3 * hrRoomWithWorkers)) * 0.02f);

        shushu.endurance += RollHrBonusPoint(firstRate, secondRate, thirdRate);
        shushu.intelligence += RollHrBonusPoint(firstRate, secondRate, thirdRate);
        shushu.magicPower += RollHrBonusPoint(firstRate, secondRate, thirdRate);
    }

    // 按“首次成功后才判定下一次”的规则，返回单项属性的HR加成点数（0~3）。
    private int RollHrBonusPoint(float firstRate, float secondRate, float thirdRate)
    {
        int bonus = 0;

        if (Random.value <= firstRate)
        {
            bonus += 1;

            if (Random.value <= secondRate)
            {
                bonus += 1;

                if (Random.value <= thirdRate)
                {
                    bonus += 1;
                }
            }
        }

        return bonus;
    }

    // 通过Id在仓库中查找鼠鼠对象。
    private Shushu FindShushuById(string id)
    {
        if (string.IsNullOrEmpty(id) || BaseData.instance == null || BaseData.instance.shushuList == null)
        {
            return null;
        }

        for (int i = 0; i < BaseData.instance.shushuList.Count; i++)
        {
            Shushu shu = BaseData.instance.shushuList[i];
            if (shu != null && shu.Id == id)
            {
                return shu;
            }
        }

        return null;
    }

    // 按规则抽取1-3条buff，且不重复
    private void RollBuffs(Shushu shushu)
    {
        shushu.buffid1 = Random.Range(1, 23);
        shushu.buffid2 = 0;
        shushu.buffid3 = 0;

        if (Random.value <= 0.5f)
        {
            shushu.buffid2 = RollUniqueBuffId(shushu.buffid1, 0);

            if (Random.value <= 0.25f)
            {
                shushu.buffid3 = RollUniqueBuffId(shushu.buffid1, shushu.buffid2);
            }
        }
    }

    // 生成一个与已有buff不重复的buff id
    private int RollUniqueBuffId(int used1, int used2)
    {
        int id = Random.Range(1, 23);
        while (id == used1 || id == used2)
        {
            id = Random.Range(1, 23);
        }

        return id;
    }

    // 根据buffid加载对应鼠鼠图片：Resources/ShushuPicture/{buffid}
    private Sprite LoadPhotoByBuffId(int buffId)
    {
        return Resources.Load<Sprite>("ShushuPicture/" + buffId);
    }

    // 从名字池抽取未被仓库占用的名字
    private string RollUniqueName()
    {
        HashSet<string> usedNames = new HashSet<string>();

        if (BaseData.instance != null && BaseData.instance.shushuList != null)
        {
            for (int i = 0; i < BaseData.instance.shushuList.Count; i++)
            {
                Shushu shushu = BaseData.instance.shushuList[i];
                if (shushu != null && !string.IsNullOrEmpty(shushu.Name))
                {
                    usedNames.Add(shushu.Name);
                }
            }
        }

        List<string> candidates = new List<string>();
        for (int i = 0; i < NamePool.Length; i++)
        {
            if (!usedNames.Contains(NamePool[i]))
            {
                candidates.Add(NamePool[i]);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        int suffix = 1;
        string fallbackName = "鼠无名" + suffix;
        while (usedNames.Contains(fallbackName))
        {
            suffix++;
            fallbackName = "鼠无名" + suffix;
        }

        return fallbackName;
    }

    // 对基础值做边界处理（1-10）
    private void ClampBaseStats(Shushu shushu)
    {
        shushu.endurance = Mathf.Clamp(shushu.endurance, 1, 10);
        shushu.intelligence = Mathf.Clamp(shushu.intelligence, 1, 10);
        shushu.magicPower = Mathf.Clamp(shushu.magicPower, 1, 10);
    }

    // 在生成阶段计算食量，避免与BuffWord耦合业务逻辑
    private void ApplyFoodIntake(Shushu shushu, int baseFoodIntake)
    {
        if (shushu == null)
        {
            return;
        }

        int totalFoodIntake = baseFoodIntake;
        totalFoodIntake += GetFoodIntakeChangeByBuff(shushu.buffid1);
        totalFoodIntake += GetFoodIntakeChangeByBuff(shushu.buffid2);
        totalFoodIntake += GetFoodIntakeChangeByBuff(shushu.buffid3);

        shushu.foodIntake = totalFoodIntake;
    }

    // 读取单个buff对食量的改动值
    private int GetFoodIntakeChangeByBuff(int buffId)
    {
        if (buffId <= 0)
        {
            return 0;
        }

        BuffWord.Buff buff = BuffWord.GetBuff(buffId);
        if (buff == null || !buff.isFoodIntakeChange)
        {
            return 0;
        }

        return buff.foodIntakeChange;
    }
}
