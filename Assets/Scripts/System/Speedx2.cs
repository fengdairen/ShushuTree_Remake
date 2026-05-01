using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedx2 : MonoBehaviour
{
    public Button speedx2Button;
    public Sprite ButtonDown;
    public Sprite ButtonUp;

    private const float NormalSpeed = 1f;
    private const float DoubleSpeed = 2f;
    private bool isSpeedUp;
    private Image buttonImage;

    #region 初始化与销毁
    // 初始化按钮状态与点击事件：默认关闭倍速，按钮弹起。
    private void Start()
    {
        if (speedx2Button != null)
        {
            speedx2Button.onClick.AddListener(ToggleSpeed);
            buttonImage = speedx2Button.GetComponent<Image>();
        }

        SetSpeedUp(false);
    }

    // 销毁时恢复正常速度并解绑事件，避免影响其他场景。
    private void OnDestroy()
    {
        if (speedx2Button != null)
        {
            speedx2Button.onClick.RemoveListener(ToggleSpeed);
        }

        Time.timeScale = NormalSpeed;
    }
    #endregion

    #region 倍速开关
    // 点击按钮切换 1x / 2x 倍速。
    private void ToggleSpeed()
    {
        SetSpeedUp(!isSpeedUp);
    }

    // 应用倍速状态：统一使用Time.timeScale，让逻辑与表现同步加速。
    private void SetSpeedUp(bool enable)
    {
        isSpeedUp = enable;
        Time.timeScale = isSpeedUp ? DoubleSpeed : NormalSpeed;
        RefreshButtonVisual();
    }

    // 根据当前状态刷新按钮按下/弹起贴图。
    private void RefreshButtonVisual()
    {
        if (buttonImage == null)
        {
            return;
        }

        Sprite target = isSpeedUp ? ButtonDown : ButtonUp;
        if (target != null)
        {
            buttonImage.sprite = target;
        }
    }
    #endregion
}
