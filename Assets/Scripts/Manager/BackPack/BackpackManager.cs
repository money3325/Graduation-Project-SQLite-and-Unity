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

        Debug.Log("✅ 初始背包物品填充完成：3种种子（各5个）+ 浇水壶 + 锄头");
    }

    /// <summary>
    /// 从数据库加载背包物品
    /// </summary>
    private void LoadBackpackItems()
    {
        currentBackpackItems.Clear();

        if (DBManager.Instance == null)
        {
            Debug.LogError("❌ 【背包】DBManager.Instance为null，无法加载背包数据");
            return;
        }

        // 关键：读取SaveBackupID=-1的有效数据
        var items = DBManager.Instance.dbConnection.Table<BackpackItems>()
            .Where(item => item.SaveBackupId == -1)
            .ToList();

        currentBackpackItems.AddRange(items);
        Debug.Log($"✅ 【背包】加载完成，共{currentBackpackItems.Count}个物品");
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
        Debug.Log($"🔍 【背包】开始添加物品：{itemType}，数量：{count}");
        if (DBManager.Instance == null)
        {
            Debug.LogError($"❌ 【背包】DBManager.Instance为null！");
            return;
        }
        DBManager.Instance.AddItem(itemType, count);
        LoadBackpackItems();
        RefreshBackpackUI();
        Debug.Log($"✅ 【背包】物品 {itemType} 添加完成，已刷新UI");
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
            Debug.LogWarning($"⚠️ 背包中无{itemType}，无法消耗");
            return;
        }

        // 2. 计算扣减后的数量
        int newCount = targetItem.ItemCount - deltaCount;
        if (newCount <= 0)
        {
            // 3. 数量≤0，直接从数据库删除该物品（背包中不再显示）
            DBManager.Instance.DeleteBackpackItem(itemType);
            Debug.Log($"✅ 物品{itemType}已耗尽，从背包中移除");
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
            Debug.Log("✅ 取消物品选中，退出所有功能模式");
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
            Debug.Log($"✅ 进入播种模式，选中种子：{itemType}");
        }
        // 锄头→耕地模式
        else if (itemType == "Hoe")
        {
            currentMode = BackpackMode.Cultivate;
            Debug.Log("✅ 进入耕地模式，可点击未耕地进行耕地");
        }
        // 浇水壶→浇水模式
        else if (itemType == "WateringCan")
        {
            currentMode = BackpackMode.Water;
            Debug.Log("✅ 进入浇水模式，可点击作物进行浇水");
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
        Debug.Log("✅ 强制退出所有功能模式");
    }
}