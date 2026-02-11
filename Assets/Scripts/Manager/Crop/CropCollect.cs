using System.Linq;
using UnityEngine;
/// <summary>
/// 作物成熟天数枚举
/// </summary>
public enum CropMatureDays
{
    ThreeDays = 3,
    SevenDays = 7,
    TwelveDays = 12
}

/// <summary>
/// 作物采集组件（完整版本，自动挂载到成熟作物）
/// </summary>
public class CropCollect : MonoBehaviour
{
    [Header("作物基础配置（自动填充，无需手动赋值）")]
    public CropMatureDays matureDays;
    public int cropId;
    public int farmlandId;
    public string cropType;

    public bool isMature = true; // 成熟状态（仅成熟可采集）
    private BoxCollider2D cropCollider; // 2D碰撞体（点击采集）

    void Awake()
    {
        // 初始化碰撞体
        InitCollider();

        // 恢复12天作物采集状态
        if (matureDays == CropMatureDays.TwelveDays)
        {
            RestoreTwelveDaysCropStatus();
        }
    }

    /// <summary>
    /// 初始化2D碰撞体（点击采集必备）
    /// </summary>
    private void InitCollider()
    {
        cropCollider = GetComponent<BoxCollider2D>();
        if (cropCollider == null)
        {
            cropCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        cropCollider.size = new Vector2(1f, 1f);
        cropCollider.offset = Vector2.zero;
        cropCollider.isTrigger = true;
    }

    /// <summary>
    /// 恢复12天作物采集状态
    /// </summary>
    private void RestoreTwelveDaysCropStatus()
    {
        var cropData = DBManager.Instance.dbConnection.Table<CropsStatus>()
            .FirstOrDefault(c => c.Id == cropId);

        if (cropData != null && cropData.GrowthStage == 3)
        {
            isMature = false;
        }
    }

    /// <summary>
    /// 鼠标点击触发采集
    /// </summary>
    private void OnMouseDown()
    {
        if (!gameObject.activeSelf || !isMature)
        {
            return;
        }

        bool isCollectSuccess = OnCollectClick();
    }

    /// <summary>
    /// 采集核心逻辑
    /// </summary>
    private bool OnCollectClick()
    {
        // 添加到背包
        AddMatureCropToBackpack();

        // 按成熟天数执行差异化逻辑
        switch (matureDays)
        {
            case CropMatureDays.ThreeDays:
                return HandleThreeDaysCrop();
            case CropMatureDays.SevenDays:
                return HandleSevenDaysCrop();
            case CropMatureDays.TwelveDays:
                return HandleTwelveDaysCrop();
            default:
                return false;
        }
    }

    /// <summary>
    /// 12天作物浇水后触发
    /// </summary>
    public bool OnWateringAfterMature()
    {
        if (matureDays != CropMatureDays.TwelveDays || !isMature)
        {
            return false;
        }

        int currentWaterCount = DBManager.Instance.UpdateCropWateringCount(cropId);
        if (currentWaterCount >= 2)
        {
            AddMatureCropToBackpack();
            DBManager.Instance.ResetCropWateringCount(cropId);
            isMature = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 3天作物：采集后销毁
    /// </summary>
   
    private bool HandleThreeDaysCrop()
    {
        try
        {
            DBManager.Instance.DeleteCropStatusById(cropId);
            CropManager.Instance.cropInstances.Remove(cropId);
            
            // 必须调用：添加作物到背包
            AddMatureCropToBackpack();
            // 2. 同步任务进度（触发"收获小麦"任务的进度+1）
            //TaskManager.Instance.UpdateProgress("收获小麦", 1);
            
            Destroy(gameObject);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 3天作物采集失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 7天作物：采集后销毁+30%种子掉落
    /// </summary>
    private bool HandleSevenDaysCrop()
    {
        try
        {
            DBManager.Instance.DeleteCropStatusById(cropId);
            CropManager.Instance.cropInstances.Remove(cropId);
            Destroy(gameObject);

            // 30%概率掉落种子（叠加到背包）
            if (Random.Range(0f, 1f) <= 0.3f)
            {
                string seedType = $"{cropType}_Seed";
                // 调用背包叠加方法，而非直接操作数据库
                BackpackManager.Instance.AddItem(seedType, 1);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 7天作物采集失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 12天作物：首次采集后禁用，可持续循环
    /// </summary>
    private bool HandleTwelveDaysCrop()
    {
        try
        {
            isMature = false;

            // 同步数据库状态
            var cropData = DBManager.Instance.dbConnection.Table<CropsStatus>()
                .FirstOrDefault(c => c.Id == cropId);
            if (cropData != null)
            {
                cropData.GrowthStage = 3;
                DBManager.Instance.UpdateCrop(cropData);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 12天作物采集失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 添加成熟作物到背包（加日志，确认是否执行）
    /// </summary>
    private void AddMatureCropToBackpack()
    {

        // 2调用背包叠加添加方法
        try
        {
            BackpackManager.Instance.AddItem(cropType, 1);
            Debug.Log($"✅ 【作物采集】已调用背包AddItem方法，作物：{cropType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 【作物采集】调用背包AddItem失败：{e.Message}");
        }
    }

    /// <summary>
    /// 添加种子到背包
    /// </summary>
    private void AddSeedToBackpack(string seedType, int count)
    {
        Debug.Log($"✅ 【背包】添加种子：{seedType} x {count}");
    }
}