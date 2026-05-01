using System;
using UnityEngine;

[Serializable]
public class Building
{
    // 建筑物序号
    public int id;

    // 建筑物名称
    public string Name;

    // 建筑占用格子宽、高
    public int RoomX;
    public int RoomY;

    // 建造消耗
    public int magicToBuild;

    // 消耗
    public int magicConsume;
    public int nutrientConsume;
    public int fruitConsume;

    // 生产
    public int magicProduce;
    public int nutrientProduce;
    public int fruitProduce;

    // 产出周期（秒）
    public float timeToProduce;

    // 员工需求
    public int workersToRun;

    // 效率增益属性
    public string capabilityToEnhanceEfficiency;

    // 文案
    public string text;

    // 分类/类型
    public string type;

    public Building()
    {
        id = -1;
        Name = string.Empty;
        RoomX = 1;
        RoomY = 1;
        magicToBuild = 0;
        magicConsume = 0;
        nutrientConsume = 0;
        fruitConsume = 0;
        magicProduce = 0;
        nutrientProduce = 0;
        fruitProduce = 0;
        timeToProduce = 0;
        workersToRun = 0;
        capabilityToEnhanceEfficiency = string.Empty;
        text = string.Empty;
        type = string.Empty;
    }

}