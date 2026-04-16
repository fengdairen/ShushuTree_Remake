using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShuStorePanel : MonoBehaviour
{
    public GameObject[] Photo = new GameObject[10];
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public Button openButton;
    public GameObject storePanel;
    public Text ShushuText;
    public Text StoreNum;

    private const int PageSize = 10;
    private int currentPage;
    private Text[] photoNameTexts;
    private Image[] photoImages;
    private Button[] photoButtons;



    void Start()
    {
        // 初始化按钮事件
        BindButtonEvents();
        // 缓存每个照片格子的文本组件
        InitPhotoTextCache();
        // 初始化页面状态
        currentPage = 0;
        RefreshPage();

        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }

    #region 按键逻辑
    private void OnDestroy()
    {
        // 解除按钮事件，避免重复绑定
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenStorePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseStorePanel);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PrevPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
        }

        // 解除Photo按钮事件
        if (photoButtons != null)
        {
            for (int i = 0; i < photoButtons.Length; i++)
            {
                int slotIndex = i;
                if (photoButtons[slotIndex] != null)
                {
                    photoButtons[slotIndex].onClick.RemoveAllListeners();
                }
            }
        }
    }

    // 绑定open/close和翻页按钮事件
    private void BindButtonEvents()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenStorePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStorePanel);
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

  

    // 打开仓库面板并刷新数据
    private void OpenStorePanel()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        currentPage = 0;
        RefreshPage();
    }

    // 关闭仓库面板
    private void CloseStorePanel()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }

    // 上一页
    private void PrevPage()
    {
        if (currentPage <= 0)
        {
            return;
        }

        currentPage--;
        RefreshPage();
    }

    // 下一页
    private void NextPage()
    {
        int totalCount = GetShuShuCount();
        int totalPages = GetTotalPages(totalCount);

        if (currentPage >= totalPages - 1)
        {
            return;
        }

        currentPage++;
        RefreshPage();
    }

#endregion

    // 按当前页刷新10个Photo显示，数量不足则清空后续格子文本
    private void RefreshPage()
    {
        List<Shushu> list = BaseData.instance != null ? BaseData.instance.shushuList : null;
        int totalCount = list != null ? list.Count : 0;
        int totalPages = GetTotalPages(totalCount);

        if (currentPage >= totalPages)
        {
            currentPage = totalPages - 1;
        }
        if (currentPage < 0)
        {
            currentPage = 0;
        }

        int startIndex = currentPage * PageSize;

        for (int i = 0; i < Photo.Length; i++)
        {
            Text nameText = photoNameTexts != null && i < photoNameTexts.Length ? photoNameTexts[i] : null;
            Image photoImage = photoImages != null && i < photoImages.Length ? photoImages[i] : null;
            if (nameText == null)
            {
                continue;
            }

            int dataIndex = startIndex + i;
            if (list != null && dataIndex < totalCount && list[dataIndex] != null)
            {
                nameText.text = list[dataIndex].Name;

                if (photoImage != null)
                {
                    photoImage.sprite = list[dataIndex].photo;
                    photoImage.enabled = list[dataIndex].photo != null;
                }
            }
            else
            {
                nameText.text = string.Empty;

                if (photoImage != null)
                {
                    photoImage.sprite = null;
                    photoImage.enabled = false;
                }
            }
        }

        if (ShushuText != null)
        {
            ShushuText.text = string.Empty;
        }

        StoreNum.text = $"床位情况：{totalCount}/{BaseData.instance.MaxShuShu}";
    }

    #region 数据预处理
    // 缓存每个Photo下文本、图片、按钮组件，并绑定点击事件
    private void InitPhotoTextCache()
    {
        photoNameTexts = new Text[Photo.Length];
        photoImages = new Image[Photo.Length];
        photoButtons = new Button[Photo.Length];

        for (int i = 0; i < Photo.Length; i++)
        {
            GameObject photoObj = Photo[i];
            if (photoObj == null)
            {
                continue;
            }

            photoNameTexts[i] = photoObj.GetComponentInChildren<Text>(true);

            Transform imageTransform = photoObj.transform.Find("image");
            if (imageTransform == null)
            {
                imageTransform = photoObj.transform.Find("Image");
            }

            if (imageTransform != null)
            {
                photoImages[i] = imageTransform.GetComponent<Image>();
            }
            else
            {
                photoImages[i] = photoObj.GetComponentInChildren<Image>(true);
            }

            photoButtons[i] = photoObj.GetComponent<Button>();
            if (photoButtons[i] != null)
            {
                int slotIndex = i;
                photoButtons[i].onClick.AddListener(() => OnClickPhoto(slotIndex));
            }
        }
    }

    // 点击Photo按钮时显示当前格子对应鼠鼠属性
    private void OnClickPhoto(int slotIndex)
    {
        if (ShushuText == null)
        {
            return;
        }

        List<Shushu> list = BaseData.instance != null ? BaseData.instance.shushuList : null;
        int totalCount = list != null ? list.Count : 0;
        int dataIndex = currentPage * PageSize + slotIndex;

        if (list == null || dataIndex < 0 || dataIndex >= totalCount || list[dataIndex] == null)
        {
            ShushuText.text = string.Empty;
            return;
        }

        Shushu shu = list[dataIndex];
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
    // 计算总页数，至少保留1页用于展示空槽位
    private int GetTotalPages(int totalCount)
    {
        return Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)PageSize));
    }

    // 获取鼠鼠数量
    private int GetShuShuCount()
    {
        if (BaseData.instance == null || BaseData.instance.shushuList == null)
        {
            return 0;
        }

        return BaseData.instance.shushuList.Count;
    }

    #endregion
}
