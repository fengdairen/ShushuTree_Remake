using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[System.Serializable]
public class Room
{
    public string buildingId;
    public string instanceId;
    public bool isProducing;
    public bool isProductionPaused;
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
    public bool isWorking;
}


#region 黑板数据结构
// 通用黑板：用于集中存取数据，支持按Key读写。
[System.Serializable]
public class Blackboard
{
    private readonly Dictionary<string, object> data = new Dictionary<string, object>();

    // 设置指定Key的值。
    public void SetValue<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        data[key] = value;
    }

    // 读取指定Key的值，若不存在则返回默认值。
    public T GetValue<T>(string key, T defaultValue)
    {
        if (string.IsNullOrEmpty(key))
        {
            return defaultValue;
        }

        object value;
        if (data.TryGetValue(key, out value))
        {
            if (value is T)
            {
                return (T)value;
            }
        }

        return defaultValue;
    }

    // 判断Key是否已被写入。
    public bool HasKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        return data.ContainsKey(key);
    }
}
#endregion



public class BaseData : MonoBehaviour
{
    public static BaseData instance;
    private const int WallNutTypeCount = 12;

    #region 黑板键定义
    public static class BlackboardKeys
    {
        public const string NatureEnergy = "NatureEnergy";
        public const string FruitEnergy = "FruitEnergy";
        public const string RootEnergy = "RootEnergy";
        public const string RoomList = "RoomList";
        public const string ShushuList = "ShushuList";
        public const string WallNutNum = "WallNutNum";
        public const string MaxShuShu = "MaxShuShu";
    }
    #endregion

    #region 序列化字段
    [SerializeField]
    private int _natureEnergy = 0;
    [SerializeField]
    private int _fruitEnergy = 0;
    [SerializeField]
    private int _rootEnergy = 0;
    [SerializeField]
    private List<Room> _roomList;
    [SerializeField]
    private List<Shushu> _shushuList;
    [SerializeField]
    private int[] _wallNutNum;
    [SerializeField]
    private int _maxShuShu = 3;
    #endregion

    #region 黑板实例
    private readonly Blackboard blackboard = new Blackboard();

    // 获取黑板实例。
    public Blackboard Blackboard
    {
        get { return blackboard; }
    }
    #endregion

    #region 黑板通用接口
    // 从黑板读取数据（未写入时返回默认值）。
    public T GetBlackboardValue<T>(string key, T defaultValue)
    {
        return blackboard.GetValue(key, defaultValue);
    }

    // 向黑板写入数据，并同步到序列化字段。
    public void SetBlackboardValue<T>(string key, T value)
    {
        blackboard.SetValue(key, value);
        SyncSerializedValue(key, value);
    }

    // 根据Key同步序列化字段，确保Inspector数据一致。
    private void SyncSerializedValue<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (key == BlackboardKeys.NatureEnergy && value is int)
        {
            _natureEnergy = (int)(object)value;
            return;
        }

        if (key == BlackboardKeys.FruitEnergy && value is int)
        {
            _fruitEnergy = (int)(object)value;
            return;
        }

        if (key == BlackboardKeys.RootEnergy && value is int)
        {
            _rootEnergy = (int)(object)value;
            return;
        }

        if (key == BlackboardKeys.MaxShuShu && value is int)
        {
            _maxShuShu = (int)(object)value;
            return;
        }

        if (key == BlackboardKeys.RoomList)
        {
            _roomList = value as List<Room>;
            return;
        }

        if (key == BlackboardKeys.ShushuList)
        {
            _shushuList = value as List<Shushu>;
            return;
        }

        if (key == BlackboardKeys.WallNutNum)
        {
            _wallNutNum = value as int[];
        }
    }
    #endregion

    #region Unity生命周期
    // 初始化单例并同步黑板数据。
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeBlackboard();
            EnsureWallNutArray();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 每帧维护资源上限与作弊键。
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

    // 编辑器修改序列化字段时，同步黑板数据。
    private void OnValidate()
    {
        InitializeBlackboard();
        EnsureWallNutArray();
    }
    #endregion

    #region 黑板数据访问
    // 自然能量。
    public int natureEnergy
    {
        get { return blackboard.GetValue(BlackboardKeys.NatureEnergy, _natureEnergy); }
        set
        {
            _natureEnergy = value;
            blackboard.SetValue(BlackboardKeys.NatureEnergy, value);
        }
    }

    // 果实能量。
    public int fruitEnergy
    {
        get { return blackboard.GetValue(BlackboardKeys.FruitEnergy, _fruitEnergy); }
        set
        {
            _fruitEnergy = value;
            blackboard.SetValue(BlackboardKeys.FruitEnergy, value);
        }
    }

    // 根系能量。
    public int rootEnergy
    {
        get { return blackboard.GetValue(BlackboardKeys.RootEnergy, _rootEnergy); }
        set
        {
            _rootEnergy = value;
            blackboard.SetValue(BlackboardKeys.RootEnergy, value);
        }
    }

    // 房间列表。
    public List<Room> roomList
    {
        get { return blackboard.GetValue(BlackboardKeys.RoomList, _roomList); }
        set
        {
            _roomList = value;
            blackboard.SetValue(BlackboardKeys.RoomList, value);
        }
    }

    // 鼠鼠列表。
    public List<Shushu> shushuList
    {
        get { return blackboard.GetValue(BlackboardKeys.ShushuList, _shushuList); }
        set
        {
            _shushuList = value;
            blackboard.SetValue(BlackboardKeys.ShushuList, value);
        }
    }

    // 核桃数量数组。
    public int[] wallNutNum
    {
        get { return blackboard.GetValue(BlackboardKeys.WallNutNum, _wallNutNum); }
        set
        {
            _wallNutNum = value;
            blackboard.SetValue(BlackboardKeys.WallNutNum, value);
        }
    }

    // 鼠鼠上限。
    public int MaxShuShu
    {
        get { return blackboard.GetValue(BlackboardKeys.MaxShuShu, _maxShuShu); }
        set
        {
            _maxShuShu = value;
            blackboard.SetValue(BlackboardKeys.MaxShuShu, value);
        }
    }
    #endregion

    #region 黑板初始化
    // 初始化黑板数据（首次运行或实例化时调用）。
    private void InitializeBlackboard()
    {
        if (_roomList == null)
        {
            _roomList = new List<Room>();
        }

        if (_shushuList == null)
        {
            _shushuList = new List<Shushu>();
        }

        blackboard.SetValue(BlackboardKeys.NatureEnergy, _natureEnergy);
        blackboard.SetValue(BlackboardKeys.FruitEnergy, _fruitEnergy);
        blackboard.SetValue(BlackboardKeys.RootEnergy, _rootEnergy);
        blackboard.SetValue(BlackboardKeys.RoomList, _roomList);
        blackboard.SetValue(BlackboardKeys.ShushuList, _shushuList);
        blackboard.SetValue(BlackboardKeys.WallNutNum, _wallNutNum);
        blackboard.SetValue(BlackboardKeys.MaxShuShu, _maxShuShu);
    }
    #endregion
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
        int[] currentWallNutNum = wallNutNum;
        if (currentWallNutNum != null && currentWallNutNum.Length == WallNutTypeCount)
        {
            return;
        }

        int[] newArray = new int[WallNutTypeCount];
        if (currentWallNutNum != null)
        {
            int copyLen = Mathf.Min(currentWallNutNum.Length, WallNutTypeCount);
            for (int i = 0; i < copyLen; i++)
            {
                newArray[i] = currentWallNutNum[i];
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

    #region 鼠鼠工作地点查询
    // 通过鼠鼠ID遍历房间列表，返回该鼠鼠当前工作的房间；未找到则返回null。
    public Room GetWorkRoomByShushuId(string shushuId)
    {
        if (string.IsNullOrEmpty(shushuId) || roomList == null)
        {
            return null;
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            Room room = roomList[i];
            if (room == null || room.shushuIds == null)
            {
                continue;
            }

            for (int j = 0; j < room.shushuIds.Count; j++)
            {
                if (room.shushuIds[j] == shushuId)
                {
                    return room;
                }
            }
        }

        return null;
    }
    #endregion

    #region 鼠鼠信息展示
    // 获取鼠鼠简历文本（用于招募面板等简要展示）。
    public string GetShushuResumeText(Shushu shu)
    {
        if (shu == null)
        {
            return string.Empty;
        }

        string buffLine = BuffWord.BuildBuffDisplayText(shu);
        if (string.IsNullOrEmpty(buffLine))
        {
            buffLine = "无";
        }

        return "食量：" + shu.foodIntake +
               "\n词条：\n" + buffLine;
    }

    // 获取鼠鼠完整信息文本（可选包含工作地点）。
    public string GetShushuInfoText(Shushu shu, bool includeWorkLocation, BuilidingPool buildingPool)
    {
        if (shu == null)
        {
            return string.Empty;
        }

        string buffText = BuffWord.BuildBuffDisplayText(shu);
        if (string.IsNullOrEmpty(buffText))
        {
            buffText = "无";
        }

        string result = "姓名：" + shu.Name +
                        "\n体力：" + shu.endurance +
                        "\n智力：" + shu.intelligence +
                        "\n法力：" + shu.magicPower +
                        "\n食量：" + shu.foodIntake +
                        "\n词条：\n" + buffText;

        if (includeWorkLocation)
        {
            result += "\n工作地点：" + GetShushuWorkLocationText(shu, buildingPool);
        }

        return result;
    }

    // 获取鼠鼠当前工作地点文本；未分配时返回“待分配”。
    public string GetShushuWorkLocationText(Shushu shu, BuilidingPool buildingPool)
    {
        if (shu == null || string.IsNullOrEmpty(shu.Id))
        {
            return "待分配";
        }

        Room room = GetWorkRoomByShushuId(shu.Id);
        if (room == null)
        {
            return "待分配";
        }

        if (buildingPool == null)
        {
            return "未知建筑";
        }

        int id;
        if (!int.TryParse(room.buildingId, out id))
        {
            return "未知建筑";
        }

        Building building = buildingPool.GetBuildingById(id);
        if (building == null)
        {
            return "未知建筑";
        }

        string displayName = !string.IsNullOrEmpty(building.Name) ? building.Name : building.text;
        return string.IsNullOrEmpty(displayName) ? "未知建筑" : displayName;
    }
    #endregion
}
