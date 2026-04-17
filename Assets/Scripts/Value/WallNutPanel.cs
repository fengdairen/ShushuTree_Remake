using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WallNutPanel : MonoBehaviour
{
    public GameObject panel;
    public Button openButton;
    public Button closeButton;
    public GameObject[] icon = new GameObject[12];
    public Text nutText;
    public Image nutPhoto;

    private readonly string[] nutNames =
    {
        "元宝核桃", "尖尖核桃", "橡子核桃", "香蕉核桃", "甜甜圈核桃", "好大的核桃",
        "白色核桃", "金色核桃", "金色元宝核桃", "二连核桃", "三连核桃", "三连炫彩核桃"
    };

    private readonly string[] nutDescriptions =
    {
        "因长相酷似金元宝而受鼠喜爱",
        "核桃脑袋怎么尖尖的？",
        "这就是橡子吧啊喂",
        "那一晚，香蕉和核桃都喝醉了。。。",
        "谁给他扎穿了？",
        "这个核桃真的好大",
        "其实鼠鼠们觉得这个长得像鸡蛋",
        "谁不喜欢金色的小玩意呢",
        "我宣布不是金色的元宝核桃被淘汰了",
        "两个核桃长到了一起，看起来像数字8",
        "三个核桃长到了一起，这是怎么做到的？",
        "这真的是该长出来的东西吗？"
    };

    // 初始化按钮事件和面板默认状态。
    private void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        BindIconButtons();
        ClosePanel();
    }

    // 打开面板并刷新12个核桃图标状态。
    public void OpenPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
        nutPhoto.sprite = null;
        nutText.text = "点击核桃查看详情";

        RefreshIcons();
    }

    // 关闭面板。
    public void ClosePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // 给12个icon的Button绑定点击事件。
    private void BindIconButtons()
    {
        for (int i = 0; i < icon.Length; i++)
        {
            GameObject iconObj = icon[i];
            if (iconObj == null)
            {
                continue;
            }

            Button button = iconObj.GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            int id = i + 1;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OnClickNut(id); });
        }
    }

    // 根据拥有情况刷新每个icon的显示（已获得显示真实图和名字，未获得显示0号图）。
    private void RefreshIcons()
    {
        BaseData data = BaseData.instance;
        if (data == null)
        {
            return;
        }

        Sprite unknown = LoadNutSprite(0);

        for (int i = 0; i < icon.Length; i++)
        {
            GameObject iconObj = icon[i];
            if (iconObj == null)
            {
                continue;
            }

            bool owned = HasNut(data, i + 1);
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = owned ? LoadNutSprite(i + 1) : unknown;
            }

            Text iconText = iconObj.GetComponentInChildren<Text>();
            if (iconText != null)
            {
                iconText.text = owned ? nutNames[i] : "???";
            }
        }
    }

    // 点击某个核桃图标：在右侧显示名称、描述和对应贴图。
    private void OnClickNut(int id)
    {
        BaseData data = BaseData.instance;
        bool owned = HasNut(data, id);

        if (nutPhoto != null)
        {
            nutPhoto.sprite = owned ? LoadNutSprite(id) : LoadNutSprite(0);
        }

        if (nutText != null)
        {
            if (owned)
            {
                int index = id - 1;
                nutText.text = nutNames[index] + "\n" + nutDescriptions[index]+"\n现有数量：" + data.wallNutNum[index];
            }
            else
            {
                nutText.text = "未知核桃\n尚未获得";
            }
        }
    }

    // 根据id从Resources/WallNut/(id)读取Sprite。
    private Sprite LoadNutSprite(int id)
    {
        return Resources.Load<Sprite>("WallNut/" + id);
    }

    // 判断是否拥有指定核桃（id范围1~12）。
    private bool HasNut(BaseData data, int id)
    {
        if (data == null || data.wallNutNum == null)
        {
            return false;
        }

        int index = id - 1;
        if (index < 0 || index >= data.wallNutNum.Length)
        {
            return false;
        }

        return data.wallNutNum[index] > 0;
    }


}
