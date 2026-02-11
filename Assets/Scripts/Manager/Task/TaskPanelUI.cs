using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskPanelUI : MonoBehaviour
{
    public static TaskPanelUI Instance { get; private set; }

    [Header("UI 绑定")]
    public GameObject taskPanel; 
    public ScrollRect scrollRect;
    public Transform taskContent; 
    public GameObject taskItemPrefab; 

    private bool isPanelOpen = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 直接显示面板
        if (taskPanel != null)
        {
            taskPanel.SetActive(true);
            isPanelOpen = true;
        }
    }

    void Start()
    {
        Invoke("UpdateTaskUI", 1f);
        UpdateTaskUI();
    }


    public void UpdateTaskUI()
    {
        

        // 🔒 空引用防护
        if (TaskManager.Instance == null)
        {
            Debug.LogError(" TaskManager.Instance 为空，无法获取任务数据");
            return;
        }
        if (taskContent == null)
        {
            Debug.LogError(" taskContent未赋值，无法刷新任务列表");
            return;
        }

        // 仅修复：用while循环清空旧内容（核心）
        while (taskContent.childCount > 0)
        {
            DestroyImmediate(taskContent.GetChild(0).gameObject);
        }

        // 获取最新任务数据
        var tasks = TaskManager.Instance.GetCurrentTasks();

        // 无任务时显示提示
        if (tasks == null || tasks.Count == 0)
        {;
            var emptyText = new GameObject("EmptyTaskText");
            emptyText.transform.SetParent(taskContent);
            emptyText.transform.localScale = Vector3.one;

            // 修复TMP组件创建失败的问题（先导入TMP资源！）
            var tmpText = emptyText.AddComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = "暂无任务";
                tmpText.color = Color.gray;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.fontSize = 16;
                // 手动赋值TMP字体（避免Resources.Load路径错误）
                tmpText.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                tmpText.fontMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow");
            }
            else
            {
                // 降级用默认Text组件
                var text = emptyText.AddComponent<UnityEngine.UI.Text>();
                text.text = "暂无任务";
                text.color = Color.gray;
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 16;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // 调整空提示布局
            var rect = emptyText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(0, 30);
            return;
        }

        // 有任务时渲染任务项
        foreach (var task in tasks)
        {
            var item = Instantiate(taskItemPrefab, taskContent);
            item.transform.localScale = Vector3.one;

            // 获取TMP组件
            var nameText = item.transform.Find("TaskNameText")?.GetComponent<TMPro.TMP_Text>();
            var descText = item.transform.Find("TaskDescText")?.GetComponent<TMPro.TMP_Text>();
            var progressText = item.transform.Find("ProgressText")?.GetComponent<TMPro.TMP_Text>();
            var progressSlider = item.transform.Find("ProgressSlider")?.GetComponent<Slider>();

            // 填充数据（匹配你的字段名 TaskDesc，不是 TaskDescription）
            if (nameText != null)
            {
                nameText.text = task.TaskName;
                nameText.color = Color.white;
                nameText.fontSize = 22;
                nameText.ForceMeshUpdate(); // 强制刷新字体
            }
            if (descText != null)
            {
                descText.text = task.TaskDesc; // 匹配你的字段名
                descText.color = Color.gray;
                descText.fontSize = 18;
                descText.ForceMeshUpdate();
            }
            if (progressText != null)
            {
                progressText.text = $"{task.CurrentProgress}/{task.TargetCount}";
                progressText.color = Color.yellow;
                progressText.fontSize = 20;
                progressText.ForceMeshUpdate();
            }
            if (progressSlider != null)
            {
                progressSlider.maxValue = task.TargetCount;
                progressSlider.value = task.CurrentProgress;
            }

            // 完成状态标记
            if (task.TaskStatus == TaskManager.TASK_COMPLETED)
            {
                if (nameText != null) nameText.color = Color.green;
                if (progressText != null) progressText.text = " 已完成";
            }
        }

        // 刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(taskContent.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}