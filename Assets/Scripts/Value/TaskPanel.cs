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

    #region 生命周期

    // 初始化按钮监听与面板默认状态。
    private void Start()
    {
        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }

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

    // 监听作弊键：按下P将当前任务直接设为完成。
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P))
        {
            return;
        }

        if (taskManager == null)
        {
            taskManager = FindObjectOfType<TaskManager>();
        }

        if (taskManager == null)
        {
            return;
        }

        taskManager.CheatCompleteCurrentTask();

        if (thePanel != null && thePanel.activeSelf)
        {
            RefreshTaskView();
        }
    }

    #endregion

    #region 面板开关

    // 打开任务面板，并在打开时刷新一次任务完成状态。
    public void OpenPanel()
    {
        if (thePanel != null)
        {
            thePanel.SetActive(true);
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
        if (thePanel != null)
        {
            thePanel.SetActive(false);
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
            return;
        }

        TaskDefinition currentTask = taskManager.GetCurrentTask();
        if (currentTask == null)
        {
            SetTaskText("任务已完成", "暂无更多任务");
            SetReceiveButtonVisible(false);
            return;
        }

        SetTaskText(currentTask.taskName, currentTask.description);
        SetReceiveButtonVisible(taskManager.CanClaimCurrentTask());
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

    #endregion

}
