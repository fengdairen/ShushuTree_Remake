using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AllocatePanel : MonoBehaviour
{
    public GameObject[] UpPhoto = new GameObject[5];
    public GameObject[] DownPhoto = new GameObject[5];

    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    public GameObject Panel;
    public Text ShushuText;
    public Text BuildingText;

    public Sprite noNeedShu;
    public Sprite waitShushu;

    private const int PageSize = 5;
    private int currentPage;

    private Text[] UpphotoNameTexts;
    private Image[] UpphotoImages;
    private Button[] UpphotoButtons;
    private Text[] DownphotoNameTexts;
    private Image[] DownphotoImages;
    private Button[] DownphotoButtons;

    private string currentBuildingId = string.Empty;
    private string currentInstanceId = string.Empty;
    private Room currentRoom;
    private Building currentBuilding;
    private int currentWorkersToRun;

    private bool lastSelectFromUp;
    private string lastSelectedShuId = string.Empty;

    public CellManager cellManager;
    public Camera mainCamera;
    public BuilidingPool buildingPool;
    public BuildingPanelWiget buildingPanelWiget;
    public BPDestory bpDestory;

    // 初始化面板状态和关闭按钮事件
    private void Start()
    {
        InitPhotoSlots();
        BindButtonEvents();

        if (Panel != null)
        {
            Panel.SetActive(false);
        }
    }

    // 每帧检测点击建筑格子，打开分配面板并展示建筑信息
    private void Update()
    {
        if (cellManager == null) return;
        if (IsInBuildOrDestroyMode()) return;
        if (IsPointerOverUI()) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2Int cellPos;
        if (!TryGetMouseCell(out cellPos)) return;

        Cell cell = cellManager.GetCell(cellPos.x, cellPos.y);
        if (cell == null || cell.state != 2) return;

        OpenPanelAndShowBuilding(cell);
    }


    // 组件销毁时解除按钮事件。
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PrevPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
        }

        RemovePhotoButtonEvents(UpphotoButtons);
        RemovePhotoButtonEvents(DownphotoButtons);
    }


    // 关闭分配面板
    private void ClosePanel()
    {
        if (Panel != null)
        {
            Panel.SetActive(false);
        }

        lastSelectedShuId = string.Empty;
    }

    // 打开面板并把当前建筑信息写入 BuildingText
    private void OpenPanelAndShowBuilding(Cell cell)
    {
        if (Panel != null)
        {
            Panel.SetActive(true);
        }

        currentBuildingId = cell.buildingTag;
        currentInstanceId = cell.buildingInstanceId.ToString();
        int buildingId = int.Parse(cell.buildingTag);
        currentBuilding = buildingPool.GetBuildingById(buildingId);
        currentWorkersToRun = currentBuilding.workersToRun;
        currentRoom = GetOrCreateCurrentRoom();
        currentPage = 0;
        lastSelectedShuId = string.Empty;

        BuildingText.text = currentBuilding.text;
        ShushuText.text = "点击鼠鼠头像可查看信息，再次点击可分配/取消分配";
        RefreshPage();
    }


    #region 页面与点击逻辑
    // 绑定上下翻页、关闭按钮事件。
    private void BindButtonEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(PrevPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextPage);
        }
    }

    // 初始化上下两组照片槽位缓存，并绑定点击事件。
    private void InitPhotoSlots()
    {
        UpphotoNameTexts = new Text[UpPhoto.Length];
        UpphotoImages = new Image[UpPhoto.Length];
        UpphotoButtons = new Button[UpPhoto.Length];

        for (int i = 0; i < UpPhoto.Length; i++)
        {
            GameObject slot = UpPhoto[i];
            if (slot == null) continue;

            UpphotoNameTexts[i] = slot.GetComponentInChildren<Text>(true);
            UpphotoImages[i] = FindImageInSlot(slot);
            UpphotoButtons[i] = slot.GetComponent<Button>();
            if (UpphotoButtons[i] != null)
            {
                int index = i;
                UpphotoButtons[i].onClick.AddListener(() => OnClickUpPhoto(index));
            }
        }

        DownphotoNameTexts = new Text[DownPhoto.Length];
        DownphotoImages = new Image[DownPhoto.Length];
        DownphotoButtons = new Button[DownPhoto.Length];

        for (int i = 0; i < DownPhoto.Length; i++)
        {
            GameObject slot = DownPhoto[i];
            if (slot == null) continue;

            DownphotoNameTexts[i] = slot.GetComponentInChildren<Text>(true);
            DownphotoImages[i] = FindImageInSlot(slot);
            DownphotoButtons[i] = slot.GetComponent<Button>();
            if (DownphotoButtons[i] != null)
            {
                int index = i;
                DownphotoButtons[i].onClick.AddListener(() => OnClickDownPhoto(index));
            }
        }
    }

    // 点击上一页。
    private void PrevPage()
    {
        if (currentPage <= 0)
        {
            return;
        }

        currentPage--;
        lastSelectedShuId = string.Empty;
        RefreshPage();
    }

    // 点击下一页。
    private void NextPage()
    {
        int total = GetUnassignedList().Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(total / (float)PageSize));
        if (currentPage >= totalPages - 1)
        {
            return;
        }

        currentPage++;
        lastSelectedShuId = string.Empty;
        RefreshPage();
    }

    // 刷新当前页面数据。
    private void RefreshPage()
    {
        List<Shushu> upList = GetUnassignedList();
        List<Shushu> downList = GetAssignedListForCurrentRoom();

        int start = currentPage * PageSize;
        for (int i = 0; i < UpPhoto.Length; i++)
        {
            int dataIndex = start + i;
            Shushu shu = dataIndex < upList.Count ? upList[dataIndex] : null;
            UpdateUpSlot(i, shu);
        }

        for (int i = 0; i < DownPhoto.Length; i++)
        {
            Shushu shu = i < downList.Count ? downList[i] : null;
            UpdateDownSlot(i, shu);
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(upList.Count / (float)PageSize));
        if (prevButton != null)
        {
            prevButton.interactable = currentPage > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentPage < totalPages - 1;
        }
    }

    // 点击待分配鼠鼠：第一次展示信息，第二次执行分配。
    private void OnClickUpPhoto(int slotIndex)
    {
        List<Shushu> upList = GetUnassignedList();
        int dataIndex = currentPage * PageSize + slotIndex;
        if (dataIndex < 0 || dataIndex >= upList.Count)
        {
            return;
        }

        Shushu shu = upList[dataIndex];
        if (shu == null)
        {
            return;
        }

        ShowShushuInfo(shu);

        if (IsSecondClick(true, shu.Id))
        {
            AssignShushuToCurrentRoom(shu);
            lastSelectedShuId = string.Empty;
            RefreshPage();
            return;
        }

        lastSelectFromUp = true;
        lastSelectedShuId = shu.Id;
    }

    // 点击已分配鼠鼠：第一次展示信息，第二次执行放回仓库。
    private void OnClickDownPhoto(int slotIndex)
    {
        List<Shushu> downList = GetAssignedListForCurrentRoom();
        if (slotIndex < 0 || slotIndex >= downList.Count)
        {
            return;
        }

        Shushu shu = downList[slotIndex];
        if (shu == null)
        {
            return;
        }

        ShowShushuInfo(shu);

        if (IsSecondClick(false, shu.Id))
        {
            UnassignShushuFromCurrentRoom(shu);
            lastSelectedShuId = string.Empty;
            RefreshPage();
            return;
        }

        lastSelectFromUp = false;
        lastSelectedShuId = shu.Id;
    }

    // 判断是否为同一鼠鼠的第二次点击。
    private bool IsSecondClick(bool isFromUp, string shuId)
    {
        return !string.IsNullOrEmpty(shuId)
               && !string.IsNullOrEmpty(lastSelectedShuId)
               && lastSelectFromUp == isFromUp
               && lastSelectedShuId == shuId;
    }
    #endregion


    #region 分配数据逻辑
    // 获取当前建筑对应的房间数据，不存在则创建。
    private Room GetOrCreateCurrentRoom()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return null;
        }

        if (data.roomList == null)
        {
            data.roomList = new List<Room>();
        }

        for (int i = 0; i < data.roomList.Count; i++)
        {
            Room room = data.roomList[i];
            if (room != null && room.buildingId == currentBuildingId && room.instanceId == currentInstanceId)
            {
                if (room.shushuIds == null)
                {
                    room.shushuIds = new List<string>();
                }
                return room;
            }
        }

        Room newRoom = new Room
        {
            buildingId = currentBuildingId,
            instanceId = currentInstanceId,
            shushuIds = new List<string>()
        };
        data.roomList.Add(newRoom);
        return newRoom;
    }

    // 获取仓库中未分配工作的鼠鼠列表。
    private List<Shushu> GetUnassignedList()
    {
        List<Shushu> result = new List<Shushu>();
        BaseData data = BaseData.instance;
        if (data == null || data.shushuList == null)
        {
            return result;
        }

        for (int i = 0; i < data.shushuList.Count; i++)
        {
            Shushu shu = data.shushuList[i];
            if (shu == null || shu.haveJob)
            {
                continue;
            }
            EnsureShushuId(shu);
            result.Add(shu);
        }

        return result;
    }

    // 获取当前建筑已分配鼠鼠列表。
    private List<Shushu> GetAssignedListForCurrentRoom()
    {
        List<Shushu> result = new List<Shushu>();
        BaseData data = BaseData.instance;
        if (data == null || data.shushuList == null || currentRoom == null || currentRoom.shushuIds == null)
        {
            return result;
        }

        for (int i = 0; i < currentRoom.shushuIds.Count; i++)
        {
            string id = currentRoom.shushuIds[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            Shushu shu = FindShushuById(id);
            if (shu != null)
            {
                result.Add(shu);
            }
        }

        return result;
    }

    // 把鼠鼠分配到当前建筑。
    private void AssignShushuToCurrentRoom(Shushu shu)
    {
        if (shu == null || currentRoom == null)
        {
            return;
        }

        EnsureShushuId(shu);
        if (string.IsNullOrEmpty(shu.Id))
        {
            return;
        }

        if (currentRoom.shushuIds == null)
        {
            currentRoom.shushuIds = new List<string>();
        }

        if (currentRoom.shushuIds.Count >= currentWorkersToRun)
        {
            if (ShushuText != null)
            {
                ShushuText.text = "该建筑岗位已满";
            }
            return;
        }

        if (!currentRoom.shushuIds.Contains(shu.Id))
        {
            currentRoom.shushuIds.Add(shu.Id);
        }

        shu.haveJob = true;
    }

    // 将鼠鼠从当前建筑放回仓库。
    private void UnassignShushuFromCurrentRoom(Shushu shu)
    {
        if (shu == null || currentRoom == null)
        {
            return;
        }

        if (currentRoom.shushuIds != null && !string.IsNullOrEmpty(shu.Id))
        {
            currentRoom.shushuIds.Remove(shu.Id);
        }

        shu.haveJob = false;
    }

    // 确保鼠鼠有可追踪的唯一ID。
    private void EnsureShushuId(Shushu shu)
    {
        if (shu != null && string.IsNullOrEmpty(shu.Id))
        {
            shu.Id = System.Guid.NewGuid().ToString();
        }
    }

    // 通过ID在仓库中查找鼠鼠。
    private Shushu FindShushuById(string id)
    {
        BaseData data = BaseData.instance;
        if (data == null || data.shushuList == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < data.shushuList.Count; i++)
        {
            Shushu shu = data.shushuList[i];
            if (shu != null && shu.Id == id)
            {
                return shu;
            }
        }

        return null;
    }
    #endregion


    #region UI显示
    // 更新上方待分配槽位显示：空位不显示图片。
    private void UpdateUpSlot(int index, Shushu shu)
    {
        if (UpphotoNameTexts != null && index >= 0 && index < UpphotoNameTexts.Length && UpphotoNameTexts[index] != null)
        {
            UpphotoNameTexts[index].text = shu != null ? shu.Name : string.Empty;
        }

        if (UpphotoImages != null && index >= 0 && index < UpphotoImages.Length && UpphotoImages[index] != null)
        {
            if (shu != null)
            {
                UpphotoImages[index].sprite = shu.photo;
                UpphotoImages[index].enabled = shu.photo != null;
            }
            else
            {
                UpphotoImages[index].sprite = null;
                UpphotoImages[index].enabled = false;
            }
        }
    }

    // 更新下方已分配槽位显示：可用岗位空位显示waitShushu，超出岗位显示noNeedShu。
    private void UpdateDownSlot(int index, Shushu shu)
    {
        if (DownphotoNameTexts != null && index >= 0 && index < DownphotoNameTexts.Length && DownphotoNameTexts[index] != null)
        {
            DownphotoNameTexts[index].text = shu != null ? shu.Name : string.Empty;
        }

        if (DownphotoImages == null || index < 0 || index >= DownphotoImages.Length || DownphotoImages[index] == null)
        {
            return;
        }

        if (shu != null)
        {
            DownphotoImages[index].sprite = shu.photo;
            DownphotoImages[index].enabled = shu.photo != null;
            return;
        }

        if (index < currentWorkersToRun)
        {
            DownphotoImages[index].sprite = waitShushu;
            DownphotoImages[index].enabled = waitShushu != null;
        }
        else
        {
            DownphotoImages[index].sprite = noNeedShu;
            DownphotoImages[index].enabled = noNeedShu != null;
        }
    }


    // 在ShushuText显示鼠鼠信息。
    private void ShowShushuInfo(Shushu shu)
    {
        if (ShushuText == null || shu == null)
        {
            return;
        }

        string buffText = BuffWord.BuildBuffDisplayText(shu);
        if (string.IsNullOrEmpty(buffText))
        {
            buffText = "无";
        }

        ShushuText.text = "姓名：" + shu.Name +
                         "\n体力：" + shu.endurance +
                         "\n智力：" + shu.intelligence +
                         "\n法力：" + shu.magicPower +
                         "\n食量：" + shu.foodIntake +
                         "\n词条：\n" + buffText;
    }

    // 查找照片槽内用于显示头像的Image。
    private Image FindImageInSlot(GameObject slot)
    {
        if (slot == null)
        {
            return null;
        }

        Transform imageTransform = slot.transform.Find("image");
        if (imageTransform == null)
        {
            imageTransform = slot.transform.Find("Image");
        }

        if (imageTransform != null)
        {
            return imageTransform.GetComponent<Image>();
        }

        return slot.GetComponentInChildren<Image>(true);
    }

    // 解除一组照片按钮上的所有点击事件。
    private void RemovePhotoButtonEvents(Button[] buttons)
    {
        if (buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].onClick.RemoveAllListeners();
            }
        }
    }
    #endregion


    #region 解析cell
    // 尝试把当前鼠标位置解析成格子坐标
    private bool TryGetMouseCell(out Vector2Int cellPos)
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
        {
            cellPos = Vector2Int.zero;
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Vector2Int hitCell;
            if (TryParseCellFromName(hit.transform.name, out hitCell))
            {
                cellPos = hitCell;
                return cellManager.IsValid(cellPos.x, cellPos.y);
            }
        }

        Plane gridPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, cellManager.transform.position.z));
        float enter;
        if (!gridPlane.Raycast(ray, out enter))
        {
            cellPos = Vector2Int.zero;
            return false;
        }

        Vector3 world = ray.GetPoint(enter);
        int x = Mathf.FloorToInt((world.x - cellManager.origin.x) / cellManager.cellSize.x + 0.5f);
        int y = Mathf.FloorToInt((world.y - cellManager.origin.y) / cellManager.cellSize.y + 0.5f);
        cellPos = new Vector2Int(x, y);
        return cellManager.IsValid(x, y);
    }

    // 从 Cell_x_y 解析格子坐标
    private bool TryParseCellFromName(string objectName, out Vector2Int cellPos)
    {
        cellPos = Vector2Int.zero;
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("Cell_")) return false;

        string[] parts = objectName.Split('_');
        if (parts.Length != 3) return false;

        int x;
        int y;
        if (!int.TryParse(parts[1], out x) || !int.TryParse(parts[2], out y)) return false;

        cellPos = new Vector2Int(x, y);
        return true;
    }

    // 避免点击穿透 UI
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }


    // 当处于建造模式或拆除模式时，不响应分配面板弹出。
    private bool IsInBuildOrDestroyMode()
    {
        if (bpDestory != null && bpDestory.isDestorying)
        {
            return true;
        }

        if (buildingPanelWiget != null && buildingPanelWiget.selectedBuildingId >= 0)
        {
            return true;
        }

        return false;
    }
    #endregion



}
