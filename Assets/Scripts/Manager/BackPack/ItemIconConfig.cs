using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 背包物品图标全局配置（管理所有物品的Sprite）
/// </summary>
public class ItemIconConfig : MonoBehaviour
{
    public static ItemIconConfig Instance;

    [Header("工具图标")]
    public Sprite hoeSprite; // 锄头图标
    public Sprite wateringCanSprite; // 浇水壶图标

    [Header("作物配置（关联CropConfig，自动同步）")]
    public List<CropConfig> cropConfigs; // 与CropManager的cropConfigs一致

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 根据物品类型获取对应的Sprite图标
    /// </summary>
    public Sprite GetItemSprite(string itemType)
    {
        // 1. 匹配工具图标
        if (itemType == "Hoe") return hoeSprite;
        if (itemType == "WateringCan") return wateringCanSprite;

        // 2. 匹配种子图标
        if (itemType.EndsWith("_Seed"))
        {
            string cropType = itemType.Replace("_Seed", "");
            var config = cropConfigs.FirstOrDefault(c => c.cropType == cropType);
            return config?.seedSprite;
        }

        // 3. 匹配成熟作物图标
        var cropConfig = cropConfigs.FirstOrDefault(c => c.cropType == itemType);
        return cropConfig?.matureCropSprite;
    }
}
