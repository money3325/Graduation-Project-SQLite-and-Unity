using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 背包UI管理核心脚本（带选中模式+初始物品+消耗逻辑）
/// </summary>
public class BackpackManager : MonoBehaviour
{
    // 单例
    private static BackpackManager instance;
    public static BackpackManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BackpackManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BackpackManager");
                    instance = obj.AddComponent<BackpackManager>();
                }
            }
            return instance;
        }
    }

    [Header("背包配置")]
    public List<ItemSlot> itemSlots; // 绑定5个背包格子
    private List<BackpackItems> currentBackpackItems = new List<BackpackItems>();

    [Header("当前选中状态")]
    public ItemSlot currentSelectedSlot; // 当前选中的格子
    public string currentSelectedItemType; // 当前选中的物品类型
    public enum BackpackMode { None, Plant, Cultivate, Water } // 背包功能模式
    public BackpackMode currentMode { get; private set; } // 当前功能模式

    void Awake()
    {
        // 单例去重
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 1. 初始化初始背包物品（仅执行一次）
        InitDefaultBackpackItems();
        // 2. 加载背包数据并刷新UI
        LoadBackpackItems();
        RefreshBackpackUI();
        // 3. 初始化模式为无
        currentMode = BackpackMode.None;
    }

    /// <summary>
    /// 初始化默认背包物品（3种种子+浇水壶+锄头，仅执行一次）
    /// </summary>
    private void InitDefaultBackpackItems()
    {
        var allBackpackItems = DBManager.Instance.QueryBackpackItems();
        if (allBackpackItems.Count > 0) return; // 已有物品，无需重复初始化

        // 填充3种种子（各5个，可修改数量）
        DBManager.Instance.AddItem("Wheat_Seed", 5);
        DBManager.Instance.AddItem("Tomato_Seed", 5);
        DBManager.Instance.AddItem("Carrot_Seed", 5);
        // 填充工具（数量1，无消耗）
        DBManager.Instance.AddItem("WateringCan", 1);
        DBManager.Instance.AddItem("Hoe", 1);
    }

    /// <summary>
    /// 从数据库加载背包物品
    /// </summary>
    private void LoadBackpackItems()
    {
        currentBackpackItems.Clear();

        if (DBManager.Instance == null)
        {
            return;
        }

        // 关键：读取SaveBackupID=-1的有效数据
        var items = DBManager.Instance.dbConnection.Table<BackpackItems>()
            .Where(item => item.SaveBackupId == -1)
            .ToList();

        currentBackpackItems.AddRange(items);
    }

    /// <summary>
    /// 刷新背包UI（适配8个格子，自动填充物品）
    /// </summary>
    public void RefreshBackpackUI()
    {
        // 1. 先清空所有8个格子
        foreach (var slot in itemSlots)
        {
            slot.ClearSlot();
        }

        // 2. 遍历物品数据，填充到8个格子中（优先填充，空格子留空）
        for (int i = 0; i < currentBackpackItems.Count && i < itemSlots.Count; i++)
        {
            BackpackItems item = currentBackpackItems[i];
            itemSlots[i].SetSlot(item.ItemType, item.ItemCount);
        }

        // 3. 刷新后重置选中状态
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.ForceCancelSelected();
            currentSelectedSlot = null;
            currentSelectedItemType = null;
            currentMode = BackpackMode.None;
        }
    }

    /// <summary>
    /// 对外接口：添加物品到背包（同步数据库+刷新UI）
    /// </summary>
    public void AddItem(string itemType, int count)
    {
        if (DBManager.Instance == null)
        {
            return;
        }

        // 1. 先同步数据库添加物品
        DBManager.Instance.AddItem(itemType, count);

        // 2. 核心：判断是否是任务关联物品，触发任务进度更新
        TriggerTaskProgressByItem(itemType, count);

        // 3. 加载数据+刷新UI
        LoadBackpackItems();
        RefreshBackpackUI();
    }

    /// <summary>
    /// 根据添加的物品，触发对应任务的进度更新
    /// </summary>
    private void TriggerTaskProgressByItem(string itemType, int count)
    {
        // 确保TaskManager实例存在
        if (TaskManager.Instance == null)
        {
            return;
        }

        // 映射：物品类型 → 对应的任务名（可扩展更多任务）
        var itemToTaskMap = new Dictionary<string, string>
        {
            {"Wheat", "收获小麦"}, // 小麦对应“收获小麦”任务
            // 可添加其他任务映射，比如：{"Tomato", "收获番茄"}
        };

        // 如果当前物品是任务关联物品，更新对应任务进度
        if (itemToTaskMap.TryGetValue(itemType, out string taskName))
        {
            TaskManager.Instance.UpdateProgress(taskName, count);
        }
    }

   
    /// <summary>
    /// 对外接口：消耗物品（扣减数量，同步数据库+刷新UI，数量为0则删除记录）
    /// </summary>
    public void ConsumeItem(string itemType, int deltaCount)
    {
        // 1. 获取当前物品数量
        var targetItem = currentBackpackItems.FirstOrDefault(item => item.ItemType == itemType);
        if (targetItem == null)
        {
            return;
        }

        // 2. 计算扣减后的数量
        int newCount = targetItem.ItemCount - deltaCount;
        if (newCount <= 0)
        {
            // 3. 数量≤0，直接从数据库删除该物品（背包中不再显示）
            DBManager.Instance.DeleteBackpackItem(itemType);
        }
        else
        {
            // 4. 数量>0，更新数据库物品数量
            DBManager.Instance.UpdateItemCount(itemType, -deltaCount);
        }

        // 5. 重新加载背包数据并刷新UI
        LoadBackpackItems();
        RefreshBackpackUI();
    }

    /// <summary>
    /// 格子被点击时的回调（更新选中状态+切换模式）
    /// </summary>
    public void OnItemSlotToggled(ItemSlot toggledSlot)
    {
        // 1. 如果是当前选中的格子，取消选中（退出模式）
        if (currentSelectedSlot == toggledSlot && toggledSlot.isSelected == false)
        {
            currentSelectedSlot = null;
            currentSelectedItemType = null;
            currentMode = BackpackMode.None;
            return;
        }

        // 2. 取消其他格子的选中状态（单选模式）
        foreach (var slot in itemSlots)
        {
            if (slot != toggledSlot)
            {
                slot.ForceCancelSelected();
            }
        }

        // 3. 更新当前选中状态
        currentSelectedSlot = toggledSlot;
        currentSelectedItemType = toggledSlot.currentItemType;

        // 4. 根据选中物品切换功能模式
        SwitchBackpackMode(currentSelectedItemType);
    }

    /// <summary>
    /// 根据物品类型切换功能模式
    /// </summary>
    private void SwitchBackpackMode(string itemType)
    {
        if (string.IsNullOrEmpty(itemType))
        {
            currentMode = BackpackMode.None;
            return;
        }

        // 种子→播种模式
        if (itemType.EndsWith("_Seed"))
        {
            currentMode = BackpackMode.Plant;
            // 通知CropManager选中对应种子
            CropManager.Instance.SelectSeed(itemType.Replace("_Seed", ""));
        }
        // 锄头→耕地模式
        else if (itemType == "Hoe")
        {
            currentMode = BackpackMode.Cultivate;
        }
        // 浇水壶→浇水模式
        else if (itemType == "WateringCan")
        {
            currentMode = BackpackMode.Water;
        }
        // 其他→无模式
        else
        {
            currentMode = BackpackMode.None;
        }
    }

    /// <summary>
    /// 强制退出所有模式（取消所有选中）
    /// </summary>
    public void ExitAllModes()
    {
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.ForceCancelSelected();
        }
        currentSelectedSlot = null;
        currentSelectedItemType = null;
        currentMode = BackpackMode.None;
        CropManager.Instance.isSinglePlantMode = false; // 退出播种模式
    }
}