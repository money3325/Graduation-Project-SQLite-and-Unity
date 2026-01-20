using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BedInteraction : MonoBehaviour
{
    public GameObject sleepPanel;
    public TimeManager timeManager;
    public DBManager dbManager;
    public string playerTag="Player";
    public LayerMask bedLayer;
    private bool isPlayerInRange=false;
    void Start()
    {
        if(sleepPanel==null)
        {
            return;
        }
        sleepPanel.SetActive(false);
        Button confirmBtn=sleepPanel.transform.Find("Confirm")?.GetComponent<Button>();
        Button cancelBtn=sleepPanel.transform.Find("Cancel").GetComponent<Button>();
        confirmBtn?.onClick.AddListener(ConfirmSleep);
        cancelBtn?.onClick.AddListener(()=>sleepPanel.SetActive(false));
    }
     void Update()
    {
        // 只检测鼠标左键点击（按下的瞬间）
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 把鼠标屏幕坐标转成2D世界坐标（适配正交相机）
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // 2. 发射2D射线，检测点击的2D碰撞体
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 100f,bedLayer);

            // 3. 判断：点击到当前床 + 玩家在范围内 + 面板存在
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                if (isPlayerInRange && sleepPanel != null)
                {
                    sleepPanel.SetActive(true);
                }
            }
        }
    }

    void ConfirmSleep()
    {
        timeManager.JumpToNextDay();
       dbManager.SaveGame(timeManager.currentSeason,timeManager.currentDay,System.DateTime.Now.ToString());
        sleepPanel.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag(playerTag))
        {
            isPlayerInRange=true;
            Debug.Log("进入");
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag(playerTag))
        {
            isPlayerInRange=false;
            sleepPanel.SetActive(false);
            Debug.Log("离开");
        }
    }
 

}
