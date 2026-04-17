using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuDevelop : MonoBehaviour
{
    /*
    标准逻辑：
    601房间逻辑：
    5%耐力+2，25%耐力+1，70%无
    602房间逻辑：
    1%智力+2，4%智力+1，4%魔力+2，16%魔力+1，75%无
    603房间逻辑：
    5%魔力+2，25%魔力+1，70%无

    加强逻辑
    601：10%耐力+2，50%耐力+1，40%无
    602：2%智力+2，8%智力+1，8%魔力+2，32%魔力+1，50%无
    603：10%魔力+2，50%魔力+1，40%无

    弱化逻辑
    601：2.5%耐力+2，12.5%耐力+1，85%无
    602：0.5%智力+2，2%智力+1，4%魔力+2，16%魔力+1，77.5%无
    603：2.5%魔力+2，12.5%魔力+1，85%无

    特化逻辑：
    601：不做特化
    602：1%智力+2，4%智力+1，95%无
    603：100%无

    如果有buff7
    601逻辑减弱
    如果有buff8
    602换逻辑减弱
    如果有buff9
    602,603使用特化逻辑
    如果有buff10
    603逻辑减弱

    如果有buff17
    601逻辑加强
    如果有buff18
    602逻辑加强
    如果有buff19
    603逻辑加强

    如果有
     */

    private const string LearnGreatSuccessText = "学习大成功";
    private const string LearnSuccessText = "学习成功";
    private const string LearnFailText = "学习失败";

    private enum DevelopLogicType
    {
        Standard,
        Enhanced,
        Weakened,
        Specialized
    }

    #region 成长房间判定
    // 判断是否为成长类房间（601/602/603）。
    public bool IsGrowthRoom(string buildingId)
    {
        return buildingId == "601" || buildingId == "602" || buildingId == "603";
    }
    #endregion

    #region 成长检验逻辑
    // 对指定成长房间中的鼠鼠执行一次学习检验，并返回对应跳字文本。
    public string ApplyDevelopResult(string buildingId, Shushu shu)
    {
        if (shu == null)
        {
            return LearnFailText;
        }

        DevelopLogicType logicType = GetLogicType(buildingId, shu);

        if (buildingId == "601")
        {
            return ApplyRoom601(shu, logicType);
        }

        if (buildingId == "602")
        {
            return ApplyRoom602(shu, logicType);
        }

        if (buildingId == "603")
        {
            return ApplyRoom603(shu, logicType);
        }

        return LearnFailText;
    }

    // 根据房间与buff组合决定使用哪一套培养逻辑。
    private DevelopLogicType GetLogicType(string buildingId, Shushu shu)
    {
        if (shu == null || string.IsNullOrEmpty(buildingId))
        {
            return DevelopLogicType.Standard;
        }

        // buff9：602/603直接走特化逻辑，优先级最高。
        if ((buildingId == "602" || buildingId == "603") && HasAnyBuff(shu, 9))
        {
            return DevelopLogicType.Specialized;
        }

        bool hasEnhanced = false;
        bool hasWeakened = false;

        if (buildingId == "601")
        {
            hasEnhanced = HasAnyBuff(shu, 17);
            hasWeakened = HasAnyBuff(shu, 7);
        }
        else if (buildingId == "602")
        {
            hasEnhanced = HasAnyBuff(shu, 18);
            hasWeakened = HasAnyBuff(shu, 8);
        }
        else if (buildingId == "603")
        {
            hasEnhanced = HasAnyBuff(shu, 19);
            hasWeakened = HasAnyBuff(shu, 10);
        }

        // 同时加强+减弱时回到标准逻辑。
        if (hasEnhanced && hasWeakened)
        {
            return DevelopLogicType.Standard;
        }

        if (hasEnhanced)
        {
            return DevelopLogicType.Enhanced;
        }

        if (hasWeakened)
        {
            return DevelopLogicType.Weakened;
        }

        return DevelopLogicType.Standard;
    }

    // 判断鼠鼠是否携带指定buff。
    private bool HasAnyBuff(Shushu shu, int buffId)
    {
        if (shu == null || buffId <= 0)
        {
            return false;
        }

        return shu.buffid1 == buffId || shu.buffid2 == buffId || shu.buffid3 == buffId;
    }

    // 601：按标准/加强/弱化逻辑结算（无特化逻辑）。
    private string ApplyRoom601(Shushu shu, DevelopLogicType logicType)
    {
        if (logicType == DevelopLogicType.Enhanced)
        {
            return RollSingleStat(shu, "endurance", 10f, 50f);
        }

        if (logicType == DevelopLogicType.Weakened)
        {
            return RollSingleStat(shu, "endurance", 2.5f, 12.5f);
        }

        return RollSingleStat(shu, "endurance", 5f, 25f);
    }

    // 602：按标准/加强/弱化/特化逻辑结算。
    private string ApplyRoom602(Shushu shu, DevelopLogicType logicType)
    {
        if (logicType == DevelopLogicType.Specialized)
        {
            return RollDualStat(shu, 1f, 4f, 0f, 0f);
        }

        if (logicType == DevelopLogicType.Enhanced)
        {
            return RollDualStat(shu, 2f, 8f, 8f, 32f);
        }

        if (logicType == DevelopLogicType.Weakened)
        {
            return RollDualStat(shu, 0.5f, 2f, 4f, 16f);
        }

        return RollDualStat(shu, 1f, 4f, 4f, 16f);
    }

    // 603：按标准/加强/弱化/特化逻辑结算。
    private string ApplyRoom603(Shushu shu, DevelopLogicType logicType)
    {
        if (logicType == DevelopLogicType.Specialized)
        {
            return LearnFailText;
        }

        if (logicType == DevelopLogicType.Enhanced)
        {
            return RollSingleStat(shu, "magicPower", 10f, 50f);
        }

        if (logicType == DevelopLogicType.Weakened)
        {
            return RollSingleStat(shu, "magicPower", 2.5f, 12.5f);
        }

        return RollSingleStat(shu, "magicPower", 5f, 25f);
    }

    // 执行单属性成长检验：greatRate为+2概率，successRate为+1概率。
    private string RollSingleStat(Shushu shu, string statKey, float greatRate, float successRate)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < greatRate)
        {
            AddStat(shu, statKey, 2);
            return LearnGreatSuccessText;
        }

        if (roll < greatRate + successRate)
        {
            AddStat(shu, statKey, 1);
            return LearnSuccessText;
        }

        return LearnFailText;
    }

    // 执行双属性成长检验：按智力(+2/+1)与法力(+2/+1)顺序累计概率判定。
    private string RollDualStat(Shushu shu, float intGreatRate, float intSuccessRate, float magicGreatRate, float magicSuccessRate)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < intGreatRate)
        {
            AddStat(shu, "intelligence", 2);
            return LearnGreatSuccessText;
        }

        float threshold = intGreatRate + intSuccessRate;
        if (roll < threshold)
        {
            AddStat(shu, "intelligence", 1);
            return LearnSuccessText;
        }

        threshold += magicGreatRate;
        if (roll < threshold)
        {
            AddStat(shu, "magicPower", 2);
            return LearnGreatSuccessText;
        }

        threshold += magicSuccessRate;
        if (roll < threshold)
        {
            AddStat(shu, "magicPower", 1);
            return LearnSuccessText;
        }

        return LearnFailText;
    }

    // 给指定属性增加数值并做1~10边界处理。
    private void AddStat(Shushu shu, string statKey, int addValue)
    {
        if (shu == null || string.IsNullOrEmpty(statKey) || addValue == 0)
        {
            return;
        }

        if (statKey == "endurance")
        {
            shu.endurance = Mathf.Clamp(shu.endurance + addValue, 1, 10);
            return;
        }

        if (statKey == "intelligence")
        {
            shu.intelligence = Mathf.Clamp(shu.intelligence + addValue, 1, 10);
            return;
        }

        if (statKey == "magicPower")
        {
            shu.magicPower = Mathf.Clamp(shu.magicPower + addValue, 1, 10);
        }
    }
    #endregion
}
