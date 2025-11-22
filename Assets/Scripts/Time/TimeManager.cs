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
    private float currentHour=6;
    private int currentDay;
    private string currentSeason="春";
    private string currentPeriod;//当前时段，白天，下午，晚上
    //ui
    public Text timeDisplayText;
    void Start()
    {
        PlayerCore playerData=DBManager.Instance.GetPlayerData();
        if(playerData!=null)
        {
            currentDay=27;
            //currentDay=playerData.CurrentDay;
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
                currentDay++;
                currentHour=6;
                if(currentDay>dayToSeason)
                {
                    currentDay=1;
                    //这里可扩展季节交替逻辑
                }
                DBManager.Instance.UpdatePlayer(currentSeason,currentDay);
            }
            //判断当前时段
            if(currentHour>dayStart&&currentHour<duskStart)
            {
                currentPeriod="白天";
                Debug.Log("白天");
            }else if(currentHour>duskStart&&currentHour<nightStart)
            {
                currentPeriod="傍晚";
                Debug.Log("傍晚");
            }else
            {
                currentPeriod="晚上";
                Debug.Log("晚上");
            }
            if(timeDisplayText!=null)
            {
                string hourStr=Mathf.FloorToInt(currentHour).ToString("D2");
                string minyteStr=Mathf.FloorToInt(currentHour%1*60).ToString("D2");
                timeDisplayText.text=$"{currentSeason}第{currentDay}天{hourStr}:{minyteStr}({currentPeriod})";
            }
            yield return new WaitForSeconds(1f);
        }
    }

  

}
