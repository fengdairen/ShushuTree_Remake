using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSourceA;
    public AudioSource audioSourceB;
    public AudioClip bgm;
    public AudioClip clickButton;
    

    #region 初始化
    /// <summary>
    /// 初始化音频并注册按钮点击音效。
    /// </summary>
    void Start()
    {
        PlayBgmLoop();
        RegisterButtonClickSound();
    }
    #endregion

    #region 背景音乐
    /// <summary>
    /// 永久循环播放背景音乐。
    /// </summary>
    private void PlayBgmLoop()
    {
        if (audioSourceA == null || bgm == null)
        {
            return;
        }

        audioSourceA.clip = bgm;
        audioSourceA.loop = true;
        audioSourceA.volume = 0.7f;

        if (!audioSourceA.isPlaying)
        {
            audioSourceA.Play();
        }
    }
    #endregion

    #region 按钮点击音效
    /// <summary>
    /// 为场景中的所有按钮绑定点击音效。
    /// </summary>
    private void RegisterButtonClickSound()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayClickButtonSound);
        }
    }

    /// <summary>
    /// 播放按钮点击音效。
    /// </summary>
    private void PlayClickButtonSound()
    {
        if (audioSourceB == null || clickButton == null)
        {
            return;
        }

        audioSourceB.PlayOneShot(clickButton);
    }
    #endregion
}
