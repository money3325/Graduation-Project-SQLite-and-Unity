using UnityEngine;

public class TaskRewardManager : MonoBehaviour
{
    public static TaskRewardManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 发放任务完成奖励（匹配图中「小麦种子入背包」需求）
    /// </summary>
    public void GiveReward(string taskName)
    {
        switch (taskName)
        {
            case "收获小麦":
                // 调用背包系统添加物品（假设BackpackManager已实现AddItem）
                BackpackManager.Instance.AddItem("小麦种子", 5);
                break;
            // 可扩展其他任务的奖励配置
            default:
                break;
        }
    }
}
