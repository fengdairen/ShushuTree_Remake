using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[System.Serializable]
public class Room
{
    public string buildingId;
    public string instanceId;
    public bool isProducing;
    public float finishAtSecond;
    public Queue<string> tiaozi = new Queue<string>();
    public bool isTiaozing;
    public List<string> shushuIds = new List<string>();
    // 其他房间相关数据，暂不引入
}


[System.Serializable]
public class Shushu
{
    public string Id;
    public string Name;
    public int endurance;
    public int intelligence;
    public int magicPower;
    public int buffid1;
    public int buffid2;
    public int buffid3;
    public Sprite photo;

    public int foodIntake;
    public bool isHungry;
    public bool haveJob;
}



public class BaseData : MonoBehaviour
{
    public static BaseData instance;
    private const int WallNutTypeCount = 12;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (roomList == null)
            {
                roomList = new List<Room>();
            }

            if (shushuList == null)
            {
                shushuList = new List<Shushu>();
            }

            EnsureWallNutArray();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public int natureEnergy=1500;
    public int fruitEnergy=1500;
    public int rootEnergy=1500;
    public List<Room> roomList;
    public List<Shushu> shushuList;
    public int[] wallNutNum;
    public int MaxShuShu = 3;



    private void Update()
    {
        natureEnergy = Mathf.Clamp(natureEnergy, 0, 2000);
        fruitEnergy = Mathf.Clamp(fruitEnergy, 0, 2000);
        rootEnergy = Mathf.Clamp(rootEnergy, 0, 2000);
        UpdateMaxShuShu();

        //作弊键
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            natureEnergy += 100;

        }

        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            fruitEnergy += 100;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            rootEnergy += 100;
        }
    }

    // 根据房间列表实时更新鼠鼠仓库上限，并裁剪超出上限的鼠鼠
    private void UpdateMaxShuShu()
    {
        int max = 3;

        if (roomList != null)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                Room room = roomList[i];
                if (room == null)
                {
                    continue;
                }

                if (room.buildingId == "501" )
                {
                    max += 5;
                }
                else if (room.buildingId == "502" )
                {
                    max += 9;
                }
            }
        }

        MaxShuShu = max;

        if (shushuList == null)
        {
            shushuList = new List<Shushu>();
            return;
        }

        if (shushuList.Count > MaxShuShu)
        {
            shushuList.RemoveRange(MaxShuShu, shushuList.Count - MaxShuShu);
        }
    }

    #region 核桃特殊处理
    // 确保核桃计数数组长度为12，对应1~12号核桃。
    private void EnsureWallNutArray()
    {
        if (wallNutNum != null && wallNutNum.Length == WallNutTypeCount)
        {
            return;
        }

        int[] newArray = new int[WallNutTypeCount];
        if (wallNutNum != null)
        {
            int copyLen = Mathf.Min(wallNutNum.Length, WallNutTypeCount);
            for (int i = 0; i < copyLen; i++)
            {
                newArray[i] = wallNutNum[i];
            }
        }

        wallNutNum = newArray;
    }

    // 根据核桃id(1~12)累加对应计数。
    public void AddWallNutCount(int wallNutId)
    {
        EnsureWallNutArray();

        int index = wallNutId - 1;
        if (index < 0 || index >= wallNutNum.Length)
        {
            return;
        }

        wallNutNum[index] += 1;
    }

    #endregion
}
