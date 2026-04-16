using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HRPanel : MonoBehaviour
{
    public Button openButton;
    public Button closeButton;
    public GameObject panel;
    public Button rollButton;
    public RollNewShu rollNewShu;
    public Image shushuPhoto;
    public Text resumeText;
    public Button acceptButton;
    public Button refuseButton;

    public Text nameText;
    public Text enduranceText;
    public Text intelligenceText;
    public Text magicText;

    private const int RollCost = 50;
    private Shushu pendingShu;

    // 初始化按钮事件与界面状态
    private void Start()
    {
        BindButtonEvents();
        SetPanelActive(false);
        ClearCurrentCandidate();
    }

    // 组件销毁时解除事件绑定
    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(RollOneShu);
        }

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveListener(AcceptCurrentShu);
        }

        if (refuseButton != null)
        {
            refuseButton.onClick.RemoveListener(RefuseCurrentShu);
        }
    }

    // 绑定所有按钮事件
    private void BindButtonEvents()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (rollButton != null)
        {
            rollButton.onClick.AddListener(RollOneShu);
        }

        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(AcceptCurrentShu);
        }

        if (refuseButton != null)
        {
            refuseButton.onClick.AddListener(RefuseCurrentShu);
        }
    }

    // 打开招募面板
    private void OpenPanel()
    {
        SetPanelActive(true);
    }

    // 关闭招募面板
    private void ClosePanel()
    {
        SetPanelActive(false);
    }

    // 控制面板开关
    private void SetPanelActive(bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    // 点击roll：扣除果实能量并生成一个新的候选鼠鼠
    private void RollOneShu()
    {
        if (BaseData.instance == null)
        {
            return;
        }

        if (BaseData.instance.fruitEnergy < RollCost)
        {
            ShowMessage("没饭吃了，招不到鼠");
            SetDecisionButtonsEnabled(false);
            return;
        }

      
        BaseData.instance.fruitEnergy -= RollCost;

        pendingShu = rollNewShu.GenerateRandomShuShu();
        if (pendingShu == null)
        {
            ShowMessage("生成鼠鼠失败");
            SetDecisionButtonsEnabled(false);
            return;
        }

        if (string.IsNullOrEmpty(pendingShu.Id))
        {
            pendingShu.Id = System.Guid.NewGuid().ToString();
        }

        if (string.IsNullOrEmpty(pendingShu.Name))
        {
            pendingShu.Name = "鼠无名";
        }

        pendingShu.photo = rollNewShu.lastGeneratedPhoto;

        if (shushuPhoto != null)
        {
            shushuPhoto.sprite = pendingShu.photo;
            shushuPhoto.enabled = pendingShu.photo != null;
        }

        if (resumeText != null)
        {
            nameText.text = "姓名: "+pendingShu.Name;
            enduranceText.text = "耐力：" + pendingShu.endurance;
            intelligenceText.text = "智力：" + pendingShu.intelligence;
            magicText.text = "法力：" + pendingShu.magicPower;
            resumeText.text = BuildResumeText(pendingShu);
        }

        SetDecisionButtonsEnabled(true);
    }

    // 接受当前候选鼠鼠并放入仓库
    private void AcceptCurrentShu()
    {
        if (pendingShu == null || BaseData.instance == null)
        {
            return;
        }

        if (BaseData.instance.shushuList == null)
        {
            BaseData.instance.shushuList = new List<Shushu>();
        }

        if (BaseData.instance.shushuList.Count >= BaseData.instance.MaxShuShu)
        {
            ShowMessage("床位已满，招不了鼠");
            return;
        }

        BaseData.instance.shushuList.Add(pendingShu);
        ClearCurrentCandidate();
    }

    // 拒绝当前候选鼠鼠并清空界面
    private void RefuseCurrentShu()
    {
        ClearCurrentCandidate();
    }

    // 清空当前候选鼠鼠与UI展示
    private void ClearCurrentCandidate()
    {
        pendingShu = null;

        if (shushuPhoto != null)
        {
            shushuPhoto.sprite = null;
            shushuPhoto.enabled = false;
        }

        if (resumeText != null)
        {
            resumeText.text = string.Empty;
            nameText.text = string.Empty;
            enduranceText.text = string.Empty;
            intelligenceText.text = string.Empty;
            magicText.text = string.Empty;
        }

        SetDecisionButtonsEnabled(false);
    }

    // 启用或禁用接受/拒绝按钮
    private void SetDecisionButtonsEnabled(bool enabled)
    {
        if (acceptButton != null)
        {
            acceptButton.interactable = enabled;
        }

        if (refuseButton != null)
        {
            refuseButton.interactable = enabled;
        }
    }

    // 生成简历文本
    private string BuildResumeText(Shushu shu)
    {
        string buffLine = BuffWord.BuildBuffDisplayText(shu);
        if (string.IsNullOrEmpty(buffLine))
        {
            buffLine = "无";
        }

        return "食量：" + shu.foodIntake +
               "\n词条：\n" + buffLine;
    }

    // 显示一条提示文本
    private void ShowMessage(string message)
    {
        if (resumeText != null)
        {
            resumeText.text = message;
        }
    }
}
