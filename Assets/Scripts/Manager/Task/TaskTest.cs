using UnityEngine;
using UnityEngine.UI;

public class TaskTest : MonoBehaviour
{
    public Button testDay3Btn;
    public Button testProgressBtn;
    public Button testSaveBtn;

    void Start()
    {
        testDay3Btn.onClick.AddListener(() => TaskManager.Instance.OnGameDayChanged(3));
        testProgressBtn.onClick.AddListener(() => TaskManager.Instance.UpdateProgress("收获小麦", 1));
        testSaveBtn.onClick.AddListener(() => DBManager.Instance.SaveGame("Spring", 3, "测试存档"));
    }
}
