using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    public GameObject thePanel;
    public Button openButton;
    public Button closeButton;
    public Button receiveButton;
    public Text taskName;
    public Text taskText;
    public TaskManager taskManager;
    private UIManager uiManager;

    public Image reddot;

    #region 生命周期

    // 初始化按钮监听与面板默认状态。
    private void Start()
    {
        EnsureUIManager();
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }

        RegisterTaskEvents();

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (receiveButton != null)
        {
            receiveButton.onClick.AddListener(OnClickReceive);
        }

        ClosePanel();
    }

    private void OnDestroy()
    {
        UnregisterTaskEvents();
    }

    #endregion

    #region 面板开关

    // 打开任务面板，并在打开时刷新一次任务完成状态。
    public void OpenPanel()
    {
        EnsureUIManager();
        if (uiManager != null)
        {
            uiManager.OpenPanel(thePanel);
        }

        if (taskManager != null)
        {
            taskManager.RefreshCurrentTaskCompletion();
        }

        RefreshTaskView();
    }

    // 关闭任务面板。
    public void ClosePanel()
    {
        EnsureUIManager();
        if (uiManager != null)
        {
            uiManager.ClosePanel(thePanel);
        }
    }

    #endregion

    #region 任务显示与收取

    // 刷新任务名称、描述与领取按钮显示状态。
    private void RefreshTaskView()
    {
        if (taskManager == null)
        {
            SetTaskText("任务系统未初始化", "");
            SetReceiveButtonVisible(false);
            SetRedDotVisible(false);
            return;
        }

        TaskDefinition currentTask = taskManager.GetCurrentTask();
        if (currentTask == null)
        {
            SetTaskText("任务已完成", "暂无更多任务");
            SetReceiveButtonVisible(false);
            SetRedDotVisible(false);
            return;
        }

        SetTaskText(currentTask.taskName, currentTask.description);
        bool canClaim = taskManager.CanClaimCurrentTask();
        SetReceiveButtonVisible(canClaim);
        SetRedDotVisible(canClaim);
    }

    // 点击领取按钮：领取当前任务奖励并切换到下一个任务显示。
    private void OnClickReceive()
    {
        if (taskManager == null)
        {
            return;
        }

        bool claimSuccess = taskManager.TryClaimCurrentTask();
        if (!claimSuccess)
        {
            return;
        }

        // rewardType=4（扩建）暂由TaskManager内部回调预留，不在本UI中处理。
        RefreshTaskView();
    }

    // 设置任务名称与描述文本。
    private void SetTaskText(string nameText, string descText)
    {
        if (taskName != null)
        {
            taskName.text = nameText;
        }

        if (taskText != null)
        {
            taskText.text = descText;
        }
    }

    // 控制领取按钮显示隐藏。
    private void SetReceiveButtonVisible(bool isVisible)
    {
        if (receiveButton == null)
        {
            return;
        }

        receiveButton.gameObject.SetActive(isVisible);
    }

    // 控制任务小红点显示隐藏。
    private void SetRedDotVisible(bool isVisible)
    {
        if (reddot == null)
        {
            return;
        }

        reddot.gameObject.SetActive(isVisible);
    }

    // 根据任务完成状态刷新小红点显示。
    private void RefreshRedDot()
    {
        if (taskManager == null)
        {
            SetRedDotVisible(false);
            return;
        }

        SetRedDotVisible(taskManager.CanClaimCurrentTask());
    }

    #endregion

    #region 任务事件监听

    // 订阅任务状态变化事件。
    private void RegisterTaskEvents()
    {
        if (taskManager == null)
        {
            return;
        }

        taskManager.onTaskStateChanged -= HandleTaskStateChanged;
        taskManager.onTaskStateChanged += HandleTaskStateChanged;
    }

    // 取消订阅任务状态变化事件。
    private void UnregisterTaskEvents()
    {
        if (taskManager == null)
        {
            return;
        }

        taskManager.onTaskStateChanged -= HandleTaskStateChanged;
    }

    // 任务状态变化时刷新显示。
    private void HandleTaskStateChanged()
    {
        if (thePanel != null && thePanel.activeSelf)
        {
            RefreshTaskView();
        }
        else
        {
            RefreshRedDot();
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

}
