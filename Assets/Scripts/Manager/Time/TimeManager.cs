using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    //时间参数配置
    [Header("时间缩放")]
    public float timeToHour=20;//20秒对应游戏1h
    [Header("时间划分")]
    public int dayStart=6;//白天6点开始
    public int duskStart=16;//下午16点开始
    public int nightStart=19;//晚上19点开始
    [Header("季节天数")]
    public int dayToSeason=28;
    //当前运行的变量
    public float currentHour;
    public int currentDay;
    public string currentSeason;
    private string currentPeriod;//当前时段，白天，下午，晚上
    //ui
    public Text timeDisplayText;
    //遮罩颜色变化
    public Image dayNightMask;
    private Color dayColor=new Color(0,0,0,0);
    private Color duskColor=new Color(0.8f,0.7f,0.1f,0.3f);
    private Color nightColor=new Color(0,0,0,0.7f);

    //新一天到来的事件（供CropManager订阅）
    public delegate void NewDayHandler();
    public event NewDayHandler OnNewDay; // 事件名：OnNewDay
    void Start()
    {
        PlayerCore playerData=DBManager.Instance.GetPlayerData();
        if(playerData!=null)
        {
            currentSeason=playerData.CurrentSeason;
            currentDay=playerData.CurrentDay;
            //currentDay=playerData.CurrentDay;
            currentHour=6;
            DBManager.Instance.UpdatePlayer(currentSeason,currentDay);
            UpdateMaskColor();
            UpdateTimeUI();
        }
        else
        {
            currentSeason="春";
            currentDay=28;
            currentHour=6;
            DBManager.Instance.UpdatePlayer(currentSeason,currentDay);
        }
        StartCoroutine(TimeLoop());
    }
    IEnumerator TimeLoop()
    {
        while(true)
        {   
             currentHour+=1/timeToHour;
            if(currentHour>=24)
            {   
               JumpToNextDay();
            }
           UpdateCurrentPeriod();
            UpdateTimeUI();
            UpdateMaskColor();
            yield return new WaitForSeconds(1f);
        }
    }
   public void UpdateMaskColor()
    {
        if(currentHour>=6&&currentHour<=16)
            {
                dayNightMask.color=dayColor;
            }
            else if(currentHour>=16&&currentHour<18)
            {
                float t=(currentHour-16)/1;//3小时渐变
                dayNightMask.color=Color.Lerp(dayColor,duskColor,t);
            }else if(currentHour>=18&&currentHour<19)
            {
                float t=(currentHour-18)/1;//3小时渐变
                dayNightMask.color=Color.Lerp(duskColor,nightColor,t);
            }else
            {
                dayNightMask.color=nightColor;
            }
    }
    public void JumpToNextDay()
    {
        currentDay++;
        currentHour=6;
        if(currentDay>dayToSeason)
        {
            currentDay=1;
            //这里可扩展季节交替逻辑
        }
        DBManager.Instance.UpdatePlayer(currentSeason,currentDay);
        OnNewDay?.Invoke();
    } 
    private void UpdateCurrentPeriod()
    {
         //判断当前时段
            if(currentHour>dayStart&&currentHour<duskStart)
            {
                currentPeriod="白天";
            }else if(currentHour>duskStart&&currentHour<nightStart)
            {
                currentPeriod="傍晚";
            }else
            {
                currentPeriod="晚上";
            }
    }
     public void UpdateTimeUI()
    {
        if(timeDisplayText!=null)
            {
                string hourStr=Mathf.FloorToInt(currentHour).ToString("D2");
                string minyteStr=Mathf.FloorToInt(currentHour%1*60).ToString("D2");
                timeDisplayText.text=$"{currentSeason}第{currentDay}天{hourStr}:{minyteStr}({currentPeriod})";
            }
    }
}
