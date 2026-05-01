using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StopBuildingUI : MonoBehaviour
{
    public Button StopButton;
    public Sprite clickToStop;
    public Sprite clickToStart;

    private Room currentRoom;
    private Image stopButtonImage;
    private TimeLine timeLine;

    #region 初始化与销毁
    // 初始化按钮事件与按钮图标缓存。
    private void Start()
    {
        timeLine = FindObjectOfType<TimeLine>();

        if (StopButton != null)
        {
            StopButton.onClick.AddListener(OnClickStopButton);
            stopButtonImage = StopButton.GetComponent<Image>();
        }

        RefreshButtonVisual();
    }

    // 组件销毁时解除按钮事件绑定。
    private void OnDestroy()
    {
        if (StopButton != null)
        {
            StopButton.onClick.RemoveListener(OnClickStopButton);
        }
    }
    #endregion

    #region 对外绑定接口
    // 由外部面板在切换房间时绑定当前房间。
    public void BindRoom(Room room)
    {
        currentRoom = room;
        RefreshButtonVisual();
    }
    #endregion

    #region 停止/恢复生产
    // 点击按钮：在“暂停生产”与“恢复生产”之间切换。
    private void OnClickStopButton()
    {
        if (currentRoom == null)
        {
            return;
        }

        bool paused = !currentRoom.isProductionPaused;

        if (paused)
        {
            if (timeLine != null)
            {
                timeLine.PauseRoomProduction(currentRoom);
            }
            else
            {
                currentRoom.isProductionPaused = true;
                currentRoom.isProducing = false;
                currentRoom.finishAtSecond = 0f;
            }
        }
        else
        {
            if (timeLine != null)
            {
                timeLine.ResumeRoomProduction(currentRoom);
            }
            else
            {
                currentRoom.isProductionPaused = false;
            }
        }

        RefreshButtonVisual();
    }

    // 刷新按钮图标：正常生产显示“点击停止”，暂停生产显示“点击开始”。
    private void RefreshButtonVisual()
    {
        if (stopButtonImage == null)
        {
            return;
        }

        bool isPaused = currentRoom != null && currentRoom.isProductionPaused;
        Sprite target = isPaused ? clickToStart : clickToStop;
        if (target != null)
        {
            stopButtonImage.sprite = target;
        }
    }
    #endregion

}
