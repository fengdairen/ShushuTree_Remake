using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class TabPageManager : MonoBehaviour
{
    public Button buildButton;
    public Button cancelButton;
    public Button destoryButton;
    public Button[] tabButton = new Button[4];
    public Sprite[] sprites = new Sprite[4];
    public GameObject tabPage;
    public int tabIndex = 0;
    public BuildingPanelWiget buildingPanelWiget;
    public Sprite DestoryingSprite;
    public Sprite DestorySprite;
    public bool isDestorying = false;
    public BPbuild bpDestory;
    private UIManager uiManager;


    #region 生命周期
    // Start is called before the first frame update
    void Start()
    {
        EnsureUIManager();
        buildButton.onClick.AddListener(OnBuildButtonPressed);
        destoryButton.onClick.AddListener(OnDestoryButtonPressed);
        cancelButton.onClick.AddListener(OnCancelButtonPressed);
        for (int i = 0; i < tabButton.Length; i++)
        {
            int index = i;
            tabButton[i].onClick.AddListener(() => OnExternalButtonPressed(index));
        }
    }
    #endregion


    #region 按钮交互

    void OnBuildButtonPressed()
    {
        buildingPanelWiget.pagenum = 0;
        buildingPanelWiget.UpdatePage();
        if (isDestorying) OnDestoryButtonPressed();

        EnsureUIManager();
        if (uiManager != null)
        {
            uiManager.OpenPanel(tabPage);
        }
    }

    void OnCancelButtonPressed()
    {
        EnsureUIManager();
        if (uiManager != null)
        {
            uiManager.ClosePanel(tabPage);
        }

        buildingPanelWiget.pagenum = 0;
        buildingPanelWiget.UpdatePage();
    }

    void OnDestoryButtonPressed()
    {
        if (isDestorying)
        {
                destoryButton.GetComponent<Image>().sprite = DestorySprite;
                isDestorying = false;
                if (bpDestory != null)
                {
                    bpDestory.isDestorying = false;
                }
        }
        else
        {
                destoryButton.GetComponent<Image>().sprite = DestoryingSprite;
                isDestorying = true;
                if (bpDestory != null)
                {
                    bpDestory.isDestorying = true;
                }

        }
    }

    #endregion

    #region UI管理器
    // 获取UIManager实例。
    private void EnsureUIManager()
    {
        if (uiManager == null)
        {
            uiManager = UIManager.Instance != null ? UIManager.Instance : FindObjectOfType<UIManager>();
        }
    }
    #endregion

    void OnExternalButtonPressed(int index)
    {
        Image image = tabPage.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprites[index];
            tabIndex = index;
            buildingPanelWiget.tabPageChoice = tabIndex == 0 ? "自然能量" : tabIndex == 1 ? "农业生产" : tabIndex == 2 ? "鼠鼠养成" : "其他";
            buildingPanelWiget.pagenum = 0;
            buildingPanelWiget.UpdatePage();
        }
    }


}
