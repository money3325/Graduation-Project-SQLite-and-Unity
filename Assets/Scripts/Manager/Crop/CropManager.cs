using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

[System.Serializable]
public class CropConfig
{
    public string cropType; 
    public int totalGrowthDays; 
    public GameObject seedPrefab; 
    public GameObject seedlingPrefab; 
    public GameObject maturePrefab; 
}

public class CropManager : MonoBehaviour
{
    [Header("依赖引用（拖入）")]
    public DBManager dbManager;
    public TimeManager timeManager; 
    public Transform cropParent; 
    public Tilemap farmlandTilemap; 
    public FarmlandVisualizer farmlandManager; // 用于清空浇水图标

    [Header("作物配置")]
    public List<CropConfig> cropConfigs; 

    [Header("播种状态（无需手动改）")]
    public CropConfig selectedCrop; 
    public Dictionary<int, GameObject> cropInstances = new Dictionary<int, GameObject>();
    public bool isSinglePlantMode = false; // 公开供FarmlandManager访问
    private bool isPlanting = false; // 🔥 新增：防重复点击播种

    void Awake()
    {
        if (dbManager == null) dbManager = DBManager.Instance;
        if (timeManager == null) timeManager = FindObjectOfType<TimeManager>();
        // 初始化父物体（避免为空）
        if (cropParent == null) cropParent = new GameObject("CropParent").transform;
    }

    void OnEnable()
    {
        if(timeManager != null)
            timeManager.OnNewDay += OnNewDay;
    }

    void OnDisable()
    {
        if(timeManager != null)
            timeManager.OnNewDay -= OnNewDay;
    }

    void Start()
    {
        // 🔥 加延迟加载，避免重复执行
        Invoke(nameof(LoadSavedCrops), 0.5f);
    }

    // 选择种子（单次播种模式）
    public void SelectSeed(string cropType)
    {
        if (isPlanting) return; // 防止重复选种
        selectedCrop = cropConfigs.FirstOrDefault(config => config.cropType == cropType);
        if (selectedCrop == null)
        {
            Debug.Log($"未找到{cropType}配置");
            isSinglePlantMode = false;
            return;
        }
        isSinglePlantMode = true;
        Debug.Log($"进入单次播种模式：{cropType}");
    }

    // 尝试播种（核心：防重复生成）
    public void TryPlantCrop(Vector3Int cellPos, FarmlandTiles farmland)
    {
        // 🔥 防重复点击
        if (isPlanting) return;
        isPlanting = true;

        try
        {
            // 基础校验
            if (!isSinglePlantMode || selectedCrop == null) { return; }
            if (!farmland.IsCultivated) { Debug.Log("仅已耕地可播种"); return; }
            if (dbManager.GetCropsByFarmlandId(farmland.Id).Any()) { Debug.Log("该耕地已有作物"); return; }

            // 1. 计算精准坐标（仅生成1个）
            Vector3 spawnPos = farmlandTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
            
            // 2. 生成预制体（仅1个）
            GameObject cropInst = Instantiate(selectedCrop.seedPrefab, spawnPos, Quaternion.identity, cropParent);
            cropInst.transform.localScale = Vector3.one;

            // 3. 写入数据库（生长阶段初始为0）
            CropsStatus newCrop = new CropsStatus
            {
                FarmlandId = farmland.Id,
                CropType = selectedCrop.cropType,
                GrowthStage = 0, // 明确初始阶段
                DaysRemaining = selectedCrop.totalGrowthDays,
                TotalGrowthDays = selectedCrop.totalGrowthDays
            };
            dbManager.InsertCrop(newCrop); 
            cropInstances[newCrop.Id] = cropInst;

            Debug.Log($"✅ 成功播种{newCrop.CropType}，ID：{newCrop.Id}，初始阶段：0");
        }
        finally
        {
            // 重置状态，仅生成1次
            isSinglePlantMode = false;
            selectedCrop = null;
            isPlanting = false;
        }
    }

    // 加载已保存作物（防重复生成）
    private void LoadSavedCrops()
    {
        // 清空旧实例（核心：避免重复）
        foreach (Transform child in cropParent) Destroy(child.gameObject);
        cropInstances.Clear();

        var allCrops = dbManager.GetAllCrops();
        if (allCrops == null || allCrops.Count == 0) return;

        foreach (var crop in allCrops)
        {
            CropConfig config = cropConfigs.FirstOrDefault(c => c.cropType == crop.CropType);
            FarmlandTiles farmland = dbManager.GetFarmlandById(crop.FarmlandId);
            if (config == null || farmland == null) continue;

            // 精准坐标
            Vector3 spawnPos = farmlandTilemap.CellToWorld(new Vector3Int(farmland.TileX, farmland.TileY, 0)) + new Vector3(0.5f, 0.5f, 0);
            
            // 按数据库阶段加载预制体
            GameObject prefab = crop.GrowthStage switch
            {
                0 => config.seedPrefab,
                1 => config.seedlingPrefab,
                2 => config.maturePrefab,
                _ => config.seedPrefab
            };
            GameObject inst = Instantiate(prefab, spawnPos, Quaternion.identity, cropParent);
            inst.transform.localScale = Vector3.one;
            cropInstances[crop.Id] = inst;

            Debug.Log($"🔄 加载作物ID：{crop.Id}，阶段：{crop.GrowthStage}，剩余天数：{crop.DaysRemaining}");
        }
    }

    // 新一天处理逻辑（核心：同步生长阶段到数据库）
    private void OnNewDay()
    {
        Debug.Log("\n===== 新一天生长检查 =====");
        var allFarmlands = dbManager.GetAllFarmlands();
        var prevWatered = allFarmlands.ToDictionary(f => f.Id, f => f.IsWatered);

        // 🔥 新增：先清空所有浇水图标（可视化同步）
        if (farmlandManager != null && farmlandManager.statusIconTilemap != null)
        {
            BoundsInt bounds = farmlandManager.statusIconTilemap.cellBounds;
            foreach (Vector3Int cellPos in bounds.allPositionsWithin)
            {
                farmlandManager.statusIconTilemap.SetTile(cellPos, null);
            }
            Debug.Log("💧 所有浇水图标已清空（可视化同步）");
        }

        // 重置浇水状态
        foreach (var farmland in allFarmlands)
        {
            farmland.IsWatered = false;
            dbManager.UpdateFarmland(farmland);
        }

        // 处理作物生长
        var allCrops = dbManager.GetAllCrops();
        if (allCrops == null || allCrops.Count == 0) return;

        foreach (var crop in allCrops)
        {
            // 前一天未浇水 → 不生长
            if (!prevWatered.TryGetValue(crop.FarmlandId, out bool watered) || !watered)
            {
                Debug.Log($"🚫 作物{crop.Id}（{crop.CropType}）前一天未浇水，不生长");
                continue;
            }

            // 🔥 新增：成熟阶段（2）不再生长，避免重复生成
            if (crop.GrowthStage == 2)
            {
                Debug.Log($"🌿 作物{crop.Id}已成熟，停止生长");
                continue;
            }

            // 剩余天数-1
            crop.DaysRemaining = Mathf.Max(0, crop.DaysRemaining - 1);
            
            // 重新计算生长阶段
            int oldStage = crop.GrowthStage;
            if (crop.DaysRemaining <= 0)
                crop.GrowthStage = 2; // 成熟
            else if (crop.DaysRemaining <= crop.TotalGrowthDays / 2)
                crop.GrowthStage = 1; // 幼苗
            else
                crop.GrowthStage = 0; // 种子

            // 同步阶段到数据库
            dbManager.UpdateCrop(crop);

            // 阶段变化 → 切换预制体
            if (crop.GrowthStage != oldStage)
            {
                Debug.Log($"🌱 作物{crop.Id}阶段更新：{oldStage}→{crop.GrowthStage}，剩余天数：{crop.DaysRemaining}");
                UpdateCropPrefab(crop);
            }
            else
            {
                Debug.Log($"📌 作物{crop.Id}阶段未变：{crop.GrowthStage}，剩余天数：{crop.DaysRemaining}");
            }
        }
    }

    // 切换作物预制体（确保执行）
    private void UpdateCropPrefab(CropsStatus crop)
    {
        CropConfig config = cropConfigs.FirstOrDefault(c => c.cropType == crop.CropType);
        FarmlandTiles farmland = dbManager.GetFarmlandById(crop.FarmlandId);
        if (config == null || farmland == null) return;

        // 🔥 强制清理旧实例（确保无残留）
        if (cropInstances.TryGetValue(crop.Id, out GameObject oldInst))
        {
            Destroy(oldInst);
            cropInstances.Remove(crop.Id);
        }

        // 🔥 额外检查：清理该格子上的所有其他作物实例（防止重复）
        Vector3Int cellPos = new Vector3Int(farmland.TileX, farmland.TileY, 0);
        Vector3 spawnPos = farmlandTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
        foreach (var kvp in cropInstances)
        {
            if (Vector3.Distance(kvp.Value.transform.position, spawnPos) < 0.1f)
            {
                Destroy(kvp.Value);
                cropInstances.Remove(kvp.Key);
                break;
            }
        }

        // 选择新阶段预制体
        GameObject prefab = crop.GrowthStage switch
        {
            0 => config.seedPrefab,
            1 => config.seedlingPrefab,
            2 => config.maturePrefab,
            _ => config.seedPrefab
        };

        // 生成新预制体
        GameObject newInst = Instantiate(prefab, spawnPos, Quaternion.identity, cropParent);
        newInst.transform.localScale = Vector3.one;
        cropInstances[crop.Id] = newInst;

        Debug.Log($"🔄 作物{crop.Id}预制体切换为：{prefab.name}");
    }
}