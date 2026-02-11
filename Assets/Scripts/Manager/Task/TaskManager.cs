using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using Yarn.Unity;
using System.Threading.Tasks;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }
    private DBManager dbManager;
    private bool isDBReady = false;

    public const string TASK_NOT_STARTED = "NotStarted";
    public const string TASK_IN_PROGRESS = "InProgress";
    public const string TASK_COMPLETED = "Completed";

    private class TaskDialogueMap
    {
        public string StartNode;
        public string CompleteNode;
    }

    private Dictionary<string, TaskDialogueMap> taskDialogueDict = new Dictionary<string, TaskDialogueMap>()
    {
        { "收获小麦", new TaskDialogueMap { 
            StartNode = "Task_Wheat_Start", 
            CompleteNode = "Task_Wheat_Complete" 
        }}
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(DelayedGetDBManager());
    }

    IEnumerator DelayedGetDBManager()
    {
        yield return null;
        dbManager = DBManager.Instance;
        
        if (dbManager != null && dbManager.dbConnection != null)
        {
            isDBReady = true;
        }
        else
        {
            isDBReady = false;
        }
    }

    // 天数变化通知，无任何报错，DB未就绪直接跳过，不抛红
    public void OnGameDayChanged(int currentDay)
    {
        AssignTasksByDay(currentDay);   
        CheckTaskFailure(currentDay);  
    }

    // 分配任务：不做任何“天数不对=失败”的判断，有任务就加，没有就跳过，无报错
    private void AssignTasksByDay(int currentDay)
    {
        var newTasks = new List<PlayerTasks>();

        // 只有你配置的天数（如1天）才加任务，其他天数安静跳过，不打错误日志
        if (currentDay == 1)
        {
            newTasks.Add(new PlayerTasks
            {
                TaskName = "收获小麦",
                TaskDesc = "种植并收获5个小麦",
                TargetCount = 1,
                CurrentProgress = 0,
                TaskStatus = TASK_IN_PROGRESS,
                DayAssigned = currentDay,
                DayLimit = 7,
                SaveBackupID = -1
            });
        }
        // 其他天数：不打任何错误/失败日志，安静跳过

        // 遍历添加任务，无报错
        foreach (var task in newTasks)
        {
            var existTask = dbManager.dbConnection.Table<PlayerTasks>()
                .FirstOrDefault(t => t.TaskName == task.TaskName 
                                && t.DayAssigned == task.DayAssigned 
                                && t.SaveBackupID == -1);
            
            if (existTask == null)
            {
                int insertCount = dbManager.dbConnection.Insert(task);
                // 对话触发，找不到安静跳过
                var dialogueRunner = FindObjectOfType<DialogueRunner>();
                if (dialogueRunner != null && taskDialogueDict.ContainsKey(task.TaskName))
                {
                    _ = RunDialogueAsync(dialogueRunner, taskDialogueDict[task.TaskName].StartNode);
                }
            }
        }
        // 更新UI，无报错
        TaskPanelUI.Instance?.UpdateTaskUI();
    }

    // 异步对话，安静执行
    private async Task RunDialogueAsync(DialogueRunner runner, string node)
    {
        await runner.StartDialogue(node);
        await runner.DialogueTask;
    }

    // 更新进度，无报错
    public void UpdateProgress(string taskName, int increment = 1)
    {
        if (!isDBReady) return;

        var task = dbManager.dbConnection.Table<PlayerTasks>()
            .FirstOrDefault(t => t.TaskName == taskName 
                             && t.TaskStatus == TASK_IN_PROGRESS 
                             && t.SaveBackupID == -1);

        if (task == null)
        {
            Debug.Log($"未找到进行中任务：{taskName}");
            return;
        }

        task.CurrentProgress = Mathf.Min(task.CurrentProgress + increment, task.TargetCount);
    
        if (task.CurrentProgress >= task.TargetCount)
        {
            task.TaskStatus = TASK_COMPLETED;

            var dialogueRunner = FindObjectOfType<DialogueRunner>();
            if (dialogueRunner != null && taskDialogueDict.ContainsKey(taskName))
            {
                _ = RunDialogueAsync(dialogueRunner, taskDialogueDict[taskName].CompleteNode);
            }
            TaskRewardManager.Instance?.GiveReward(taskName);
        }

        dbManager.dbConnection.Update(task);
        TaskPanelUI.Instance?.UpdateTaskUI();
    }

    // 任务失败检测，无报错
    private void CheckTaskFailure(int currentDay)
    {
        if (!isDBReady) return;

        var allTasks = dbManager.dbConnection.Table<PlayerTasks>().ToList();
        foreach (var task in allTasks)
        {
            if (task.TaskStatus != TASK_COMPLETED && currentDay > task.DayAssigned + task.DayLimit)
            {
                GameFailureManager.Instance?.TriggerGameOver(task.TaskName);
                break;
            }
        }
    }

    // 【核心】获取任务：DB未就绪直接返回空列表，无任何红色报错！
    public List<PlayerTasks> GetCurrentTasks()
    {
        // DB未就绪，直接返回空列表，不抛错，面板显示暂无任务
        if (!isDBReady)
        {
            return new List<PlayerTasks>();
        }

        var tasks = dbManager.GetCurrentPlayerTasks();
        return tasks;
    }
}