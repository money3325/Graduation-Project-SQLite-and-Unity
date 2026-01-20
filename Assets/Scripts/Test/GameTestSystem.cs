using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTestSystem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DBManager.Instance.UpdatePlayer("春", 5);
        //DBManager.Instance.InsertCrop(1, "小麦");
        PlayerCore readData = DBManager.Instance.GetPlayerData();
        Debug.Log($"{readData.CurrentSeason},{readData.CurrentDay}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
