using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

[System.Serializable]
public class CropConfig
{
    public string cropType; 
    public int totalGrowthDays; // 对应3/7/12天成熟
    public GameObject seedPrefab; 
    public GameObject seedlingPrefab; 
    public GameObject maturePrefab; 
    [Header("背包图标配置")]
    public Sprite matureCropSprite; // 成熟作物的背包图标（拖入成熟预制体的Sprite）
    public Sprite seedSprite; // 种子的背包图标（可选，优化种子显示）
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
    private bool isPlanting = false; // 防重复点击播种
    private bool isLoaded = false; // 防重复加载标志

    private static CropManager instance;
    public static CropManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CropManager>();
            }
            return instance;
        }
    }
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
        // 取消延迟加载，立即执行 + 防重复
        if (!isLoaded)
        {
            // 新增：先清理数据库中的重复作物，再加载
            dbManager.CleanDuplicateCrops();
            
            isLoaded = true;
            LoadSavedCrops(); 
        }
    }

    // 选择种子（单次播种模式）
    public void SelectSeed(string cropType)
    {
        if (isPlanting) return; // 防止重复选种
        selectedCrop = cropConfigs.FirstOrDefault(config => config.cropType == cropType);
        if (selectedCrop == null)
        {
            isSinglePlantMode = false;
            return;
        }
        isSinglePlantMode = true;
    }

    // 尝试播种（核心：防重复生成）
    // 尝试播种（核心：防重复生成+种子消耗）
public void TryPlantCrop(Vector3Int cellPos, FarmlandTiles farmland)
{
    if (isPlanting) return;
    isPlanting = true;

    try
    {
        // 基础校验（不变）
        if (!isSinglePlantMode || selectedCrop == null) { return; }
        
        var currentCrops = dbManager.dbConnection.Table<CropsStatus>()
            .Where(c => c.FarmlandId == farmland.Id && c.SaveBackupID == -1)
            .ToList();

        // 新增：获取当前选中的种子类型，准备消耗
        string seedType = $"{selectedCrop.cropType}_Seed";
        var seedItem = DBManager.Instance.GetBackpackItemByType(seedType);
        if (seedItem == null || seedItem.ItemCount <= 0)
        {
            BackpackManager.Instance.ExitAllModes(); // 耗尽后退出播种模式
            return;
        }

        Vector3 spawnPos = GetSpawnPos(farmland.TileX, farmland.TileY);
        GameObject cropInst = Instantiate(selectedCrop.seedPrefab, spawnPos, Quaternion.identity, cropParent);
        cropInst.transform.localScale = Vector3.one;

        // 关键：接收InsertCrop返回的正确自增ID，存字典
        int newCropId = dbManager.InsertCrop(farmland.Id, selectedCrop.cropType, -1); 
        cropInstances[newCropId] = cropInst;

        // 新增：消耗1个对应种子（同步数据库+背包UI）
        BackpackManager.Instance.ConsumeItem(seedType, 1);
    }
    finally
    {
        // 修改：播种后不退出播种模式（保持选中状态，可继续播种直到种子耗尽）
        // isSinglePlantMode = false; 
        // selectedCrop = null;
        isPlanting = false;
    }
}

    // 加载已保存作物（防重复生成）
    private void LoadSavedCrops()
    {
        isLoaded = false; 
        if (isLoaded)
        {
            Debug.LogWarning("作物已加载，无需重复执行");
            return;
        }
        isLoaded = true;
        var allCropsFromDB = dbManager.GetAllCrops();

        var allValidCrops = allCropsFromDB?.Where(c => c.SaveBackupID == -1 && c.Id > 0)?.ToList() ?? new List<CropsStatus>();

        if (allValidCrops.Count == 0)
        {
            ClearInvalidCropInstances(new List<CropsStatus>());
            return;
        }

        var uniqueCrops = new List<CropsStatus>();
        var processedFarmlandIds = new HashSet<int>();
        foreach (var crop in allValidCrops.OrderByDescending(c => c.Id))
        {
            if (processedFarmlandIds.Contains(crop.FarmlandId))
            {
                dbManager.DeleteCropStatusById(crop.Id);
                continue;
            }
            processedFarmlandIds.Add(crop.FarmlandId);
            uniqueCrops.Add(crop);
        }

        // 清空旧实例+字典（避免残留）
        foreach (var inst in cropInstances.Values) Destroy(inst);
        cropInstances.Clear();

        var cropMapByTilePos = new Dictionary<(int x, int y), CropsStatus>();
        foreach (var crop in uniqueCrops)
        {

            // 找耕地
            FarmlandTiles farmland = dbManager.GetAllFarmlands()
                .FirstOrDefault(f => f.Id == crop.FarmlandId && f.SaveBackupID == -1);
            if (farmland == null)
            {
                dbManager.DeleteCropStatusById(crop.Id);
                continue;
            }

            // 找作物配置
            CropConfig config = cropConfigs.FirstOrDefault(c => c.cropType == crop.CropType);
            if (config == null)
            {
                dbManager.DeleteCropStatusById(crop.Id);
                continue;
            }

            // 计算坐标
            Vector3 spawnPos = GetSpawnPos(farmland.TileX, farmland.TileY);

            //  核心恢复：3个生长阶段明确映射，不合并、不跳过
            GameObject prefab = crop.GrowthStage switch
            {
                0 => config.seedPrefab,    // 阶段0：种子预制体（播种初始状态）
                1 => config.seedlingPrefab, // 阶段1：幼苗预制体（生长中期）
                2 => config.maturePrefab,   // 阶段2：成熟预制体（可采集）
                3 => config.maturePrefab,   // 阶段3：12天作物首次采集后（视觉仍用成熟，不影响3个生长阶段玩法）
                _ => config.seedPrefab
            };
            if (prefab == null)
            {
                Debug.LogError($"❌ 作物ID={crop.Id}：阶段{crop.GrowthStage}无预制体（请检查的{GetStageName(crop.GrowthStage)}预制体是否拖入），跳过");
                continue;
            }

            // 生成实例
            GameObject inst = Instantiate(prefab, spawnPos, Quaternion.identity, cropParent);
            inst.transform.localScale = Vector3.one;
            cropInstances[crop.Id] = inst;

            // 成熟/12天作物挂载采集脚本
            if (crop.GrowthStage >= 2)
            {
                AddCropCollectScript(crop, inst);
            }
        }

        ClearInvalidCropInstances(uniqueCrops);
    }

    // 新增：辅助方法（打印阶段名称，更清晰，不影响玩法）
    private string GetStageName(int stage)
    {
        return stage switch
        {
            0 => "种子",
            1 => "幼苗",
            2 => "成熟",
            3 => "12天作物首次采集后",
            _ => "未知"
        };
    }

// 其他辅助方法（GetSpawnPos、ClearInvalidCropInstances）不变，保留即可

    // 辅助方法（不变）
    private Vector3 GetSpawnPos(int tileX, int tileY)
    {
        Vector3Int cellPos = new Vector3Int(tileX, tileY, 0);
        return farmlandTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
    }

    // 辅助方法（不变）
    private void ClearInvalidCropInstances(List<CropsStatus> validCrops)
    {
        var validCropIds = validCrops.Select(c => c.Id).ToHashSet();
        var invalidIds = cropInstances.Keys.Where(id => !validCropIds.Contains(id)).ToList();
        foreach (int id in invalidIds)
        {
            Destroy(cropInstances[id]);
            cropInstances.Remove(id);
        }
    }


    // 新一天处理逻辑（核心：同步生长阶段到数据库）
    private void OnNewDay()
    {
        var allFarmlands = dbManager.GetAllFarmlands();
        var prevWatered = allFarmlands.ToDictionary(f => f.Id, f => f.IsWatered);

        //  新增：先清空所有浇水图标（可视化同步）
        if (farmlandManager != null && farmlandManager.statusIconTilemap != null)
        {
            BoundsInt bounds = farmlandManager.statusIconTilemap.cellBounds;
            foreach (Vector3Int cellPos in bounds.allPositionsWithin)
            {
                farmlandManager.statusIconTilemap.SetTile(cellPos, null);
            }
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

            // 新增：成熟阶段（2）不再生长，避免重复生成
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
                Debug.Log($" 作物{crop.Id}阶段更新：{oldStage}→{crop.GrowthStage}，剩余天数：{crop.DaysRemaining}");
                UpdateCropPrefab(crop);
            }
            else
            {
                Debug.Log($" 作物{crop.Id}阶段未变：{crop.GrowthStage}，剩余天数：{crop.DaysRemaining}");
            }
        }
    }

    // 切换作物预制体（确保执行）
    private void UpdateCropPrefab(CropsStatus crop)
    {
        CropConfig config = cropConfigs.FirstOrDefault(c => c.cropType == crop.CropType);
        FarmlandTiles farmland = dbManager.GetFarmlandById(crop.FarmlandId);
        if (config == null || farmland == null) return;

        //  强制清理旧实例（确保无残留）
        if (cropInstances.TryGetValue(crop.Id, out GameObject oldInst))
        {
            Destroy(oldInst);
            cropInstances.Remove(crop.Id);
        }

        //  额外检查：清理该格子上的所有其他作物实例（防止重复）
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

        //  核心修改：成熟作物自动挂载采集脚本
        if (crop.GrowthStage == 2)
        {
            AddCropCollectScript(crop, newInst);
        }
    }

    //  新增：给成熟作物挂载采集脚本并配置参数
    private void AddCropCollectScript(CropsStatus crop, GameObject cropInst)
    {
        // 避免重复挂载
        if (cropInst.GetComponent<CropCollect>() != null) return;

        // 获取作物成熟天数（对应3/7/12天）
        int matureDays = GetCropMatureDays(crop.CropType);
        if (matureDays == 0)
        {
            Debug.LogWarning($"作物{crop.CropType}未配置成熟天数，无法挂载采集脚本");
            return;
        }

        // 添加采集脚本
        CropCollect collectScript = cropInst.AddComponent<CropCollect>();
        // 配置核心参数
        collectScript.cropId = crop.Id;
        collectScript.farmlandId = crop.FarmlandId;
        collectScript.cropType = crop.CropType;
        // 映射成熟天数枚举
        collectScript.matureDays = matureDays switch
        {
            3 => CropMatureDays.ThreeDays,
            7 => CropMatureDays.SevenDays,
            12 => CropMatureDays.TwelveDays,
            _ => CropMatureDays.ThreeDays
        };
    }

    //  新增：根据作物类型获取成熟天数（从配置读取）
    private int GetCropMatureDays(string cropType)
    {
        CropConfig config = cropConfigs.FirstOrDefault(c => c.cropType == cropType);
        return config?.totalGrowthDays ?? 0;
    }
}