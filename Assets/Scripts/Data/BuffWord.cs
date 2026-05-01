using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class BuffWord : MonoBehaviour
{
    /*
     * 
     1昆虫恐惧症：不能从事农业
    2黑暗料理者：不能当厨师
    3松果过敏：不能从事松果种植工作
    4体弱多病：耐力-2
    5膝盖中了一箭：耐力-3
    6重度近视：智力-2
    7懒癌：降低耐力增长的概率（使用弱化的健身房逻辑）
    8学习困难症：降低智力增长的概率（使用弱化的图书馆逻辑）
    9麻瓜：魔力为0且魔力无法成长（使用特化的图书馆、妙妙屋逻辑）
    10低悟性：降低魔力增长的概率（使用弱化的妙妙屋逻辑）
    11大胃袋：每天吃果实+10
    12究极大胃袋：每天吃果实+20

    13园艺高手：耐力+3
    14身强体壮：耐力+2
    15天资聪颖：智力+2
    16马猴烧酒：魔力+2
    17健美爱好者：在健身房锻炼成功率更高（使用强化的健身房逻辑）
    18酷爱阅读者：在图书馆学习成功率更高（使用强化的图书馆逻辑）
    19喜爱魔法：在妙妙屋学习成功率更高（使用强化的妙妙屋逻辑）
    20幸运鼠：种植核桃时更容易出收藏品（出收藏品概率+5%）
    21精英HR：智力+5
    22小鸟胃：每天吃果实-3
     
     
     
     */
    [System.Serializable]
    public class Buff
    {
        public int id;
        public string description;
        public string name;
        public bool isCangeAbility; // 是否改变基础属性
        public bool isCantDoJob; // 是否无法从事某些职业
        public int enduranceDelta;
        public int intelligenceDelta;
        public int magicPowerDelta;
        public bool isFoodIntakeChange; // 是否改变食量
        public int foodIntakeChange;
    }

    [System.Serializable]
    private class BuffJsonWrapper
    {
        public List<Buff> BuffList;
    }

    private static readonly Dictionary<int, Buff> BuffMap = new Dictionary<int, Buff>();
    private static bool isBuffLoaded;

    // 根据buff id获取配置
    public static Buff GetBuff(int id)
    {
        EnsureBuffDataLoaded();

        if (BuffMap.ContainsKey(id))
        {
            return BuffMap[id];
        }

        return null;
    }

    // 把改变基础属性的buff效果应用到鼠鼠基础数值
    public static void ApplyChangeAbilityBuffs(Shushu shushu)
    {
        EnsureBuffDataLoaded();

        if (shushu == null)
        {
            return;
        }

        ApplySingleBuffToAbility(shushu, shushu.buffid1);
        ApplySingleBuffToAbility(shushu, shushu.buffid2);
        ApplySingleBuffToAbility(shushu, shushu.buffid3);
    }


    // 生成用于UI展示的buff文本（名称+描述）
    public static string BuildBuffDisplayText(Shushu shushu)
    {
        EnsureBuffDataLoaded();

        if (shushu == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        AppendBuffLine(builder, shushu.buffid1);
        AppendBuffLine(builder, shushu.buffid2);
        AppendBuffLine(builder, shushu.buffid3);

        return builder.ToString();
    }

    // 把单个buff的基础属性改动应用到鼠鼠
    private static void ApplySingleBuffToAbility(Shushu shushu, int buffId)
    {
        if (buffId <= 0)
        {
            return;
        }

        Buff buff = GetBuff(buffId);
        if (buff == null || !buff.isCangeAbility)
        {
            return;
        }

        shushu.endurance += buff.enduranceDelta;
        shushu.intelligence += buff.intelligenceDelta;
        shushu.magicPower += buff.magicPowerDelta;


    }

    // 将单个buff文本追加到显示内容
    private static void AppendBuffLine(StringBuilder builder, int buffId)
    {
        if (buffId <= 0)
        {
            return;
        }

        Buff buff = GetBuff(buffId);
        if (buff == null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("\n");
        }

        builder.Append(buff.name);
        builder.Append("：");
        builder.Append(buff.description);
    }

    // 延迟加载Buff配置，优先读取Resources，其次读取Assets/Json/BuffJson.json
    private static void EnsureBuffDataLoaded()
    {
        if (isBuffLoaded)
        {
            return;
        }

        isBuffLoaded = true;
        BuffMap.Clear();

        string json = LoadBuffJsonContent();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("BuffWord: 未读取到BuffJson配置。");
            return;
        }

        BuffJsonWrapper wrapper = JsonUtility.FromJson<BuffJsonWrapper>(json);
        if (wrapper == null || wrapper.BuffList == null)
        {
            Debug.LogError("BuffWord: BuffJson解析失败或BuffList为空。");
            return;
        }

        for (int i = 0; i < wrapper.BuffList.Count; i++)
        {
            Buff buff = wrapper.BuffList[i];
            if (buff == null)
            {
                continue;
            }

            if (BuffMap.ContainsKey(buff.id))
            {
                Debug.LogWarning("BuffWord: 发现重复buff id，已跳过 id=" + buff.id);
                continue;
            }

            BuffMap.Add(buff.id, buff);
        }
    }

    // 读取BuffJson文本内容
    private static string LoadBuffJsonContent()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Json/BuffJson");
        if (textAsset != null)
        {
            return textAsset.text;
        }

        string filePath = Path.Combine(Application.dataPath, "Json/BuffJson.json");
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        return string.Empty;
    }
}
