using UnityEngine;
using UnityEngine.UI;

public class TimePanel : MonoBehaviour
{
    public Text dayText;
    public Text timeText;
    public TimeLine timeLine;


    private int prevDay = int.MinValue;
    private int prevSecond = int.MinValue;

    // 初始化并尝试刷新一次时间显示。
    private void Start()
    {
        if (timeLine == null)
        {
            timeLine = FindObjectOfType<TimeLine>();
        }

        Refresh();
    }

   
    private void Update()
    {
        #region 每帧同步显示天数与时间文本
        if (timeLine == null)
        {
            timeLine = FindObjectOfType<TimeLine>();
            if (timeLine == null) return;
        }

        if (timeLine.currentDay == prevDay && timeLine.secondInDay == prevSecond) return;

        Refresh();
        #endregion


    }

    // 刷新 UI 文本：天数显示 x/总天数，时间显示 xxx/150。
    private void Refresh()
    {
        if (timeLine == null) return;

        prevDay = timeLine.currentDay;
        prevSecond = timeLine.secondInDay;

        if (dayText != null)
        {
            dayText.text = prevDay.ToString();
        }

        if (timeText != null)
        {
            int dayTotal = Mathf.FloorToInt(timeLine.dayDuration);
            timeText.text = Mathf.Min(prevSecond, dayTotal - 3) + " / " + (dayTotal - 3);
        }
    }





}
