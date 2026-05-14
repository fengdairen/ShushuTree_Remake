using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedx2 : MonoBehaviour
{
    public Button speedx2Button;
    public Sprite speed1Sprite;
    public Sprite speed2Sprite;
    public Sprite speed3Sprite;
    private const float NormalSpeed = 1f;
    private const float DoubleSpeed = 2f;
    private const float TripleSpeed = 3f;
    private int speedLevel = 1;
    private Image buttonImage;

    #region 初始化与销毁
    // 初始化按钮状态与点击事件：默认1倍速。
    private void Start()
    {
        if (speedx2Button != null)
        {
            speedx2Button.onClick.AddListener(CycleSpeed);
            buttonImage = speedx2Button.GetComponent<Image>();
        }

        SetSpeedLevel(1);
    }

    // 销毁时恢复正常速度并解绑事件，避免影响其他场景。
    private void OnDestroy()
    {
        if (speedx2Button != null)
        {
            speedx2Button.onClick.RemoveListener(CycleSpeed);
        }

        Time.timeScale = NormalSpeed;
    }
    #endregion

    #region 倍速开关
    // 点击按钮轮换 1x / 2x / 3x 倍速。
    private void CycleSpeed()
    {
        int nextLevel = speedLevel + 1;
        if (nextLevel > 3)
        {
            nextLevel = 1;
        }

        SetSpeedLevel(nextLevel);
    }

    // 应用倍速等级：统一使用Time.timeScale，让逻辑与表现同步加速。
    private void SetSpeedLevel(int level)
    {
        speedLevel = Mathf.Clamp(level, 1, 3);
        switch (speedLevel)
        {
            case 2:
                Time.timeScale = DoubleSpeed;
                break;
            case 3:
                Time.timeScale = TripleSpeed;
                break;
            default:
                Time.timeScale = NormalSpeed;
                break;
        }

        RefreshButtonVisual();
    }

    // 根据当前速度等级刷新按钮贴图。
    private void RefreshButtonVisual()
    {
        if (buttonImage == null)
        {
            return;
        }

        Sprite target = GetSpeedSprite();
        if (target != null)
        {
            buttonImage.sprite = target;
        }
    }
    #endregion

    #region 贴图选择

    // 按速度等级获取对应Sprite。
    private Sprite GetSpeedSprite()
    {
        switch (speedLevel)
        {
            case 2:
                return speed2Sprite;
            case 3:
                return speed3Sprite;
            default:
                return speed1Sprite;
        }
    }

    #endregion
}
