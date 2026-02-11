using SQLite4Unity3d;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DBManager : MonoBehaviour
{
    //单例
    private static DBManager  instance;
    public static DBManager Instance
    {
        get
        {
            //如果单例为空，在页面找有挂该脚本的物体
            if (instance == null)
            {
                instance = FindObjectOfType<DBManager>();
                if (instance == null)//如果还是空，新建一个挂改脚本的物体
                {
                    GameObject obj = new GameObject("DBManager");
                    instance = obj.AddComponent<DBManager>();
                }
            }
            return instance;
        }
    }
    //将sqlite关联到该脚本
    public SQLiteConnection dbConnection;
    //对于每一帧，初始化数据库连接，将表放到这个里面
    void Awake()
    {
        // 单例去重：如果已有实例，销毁当前物体
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
            if (dbConnection == null)
            {
                string dbPath = Application.persistentDataPath + "/GameData.db";
                // 尝试创建连接
                dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

                // 创建表
                CreateAllTables();
            }
    }
    void Start()
    {
        Debug.Log(Application.persistentDataPath);
    }
    //创建表的方法
    public void CreateAllTables()
    {
        dbConnection.CreateTable<BackpackItems>();
        dbConnection.CreateTable<CropAtlas>();
        dbConnection.CreateTable<CropsStatus>();
        dbConnection.CreateTable<FarmlandTiles>();
        dbConnection.CreateTable<PlayerCore>();
        dbConnection.CreateTable<PlayerTasks>();
        dbConnection.CreateTable<SaveBackups>();
        dbConnection.CreateTable<YarnDislogueVars>();
    }
    public void UpdatePlayer(string season, int day)
    {
        var player = dbConnection.Table<PlayerCore>().FirstOrDefault();
        if (player == null)
        {
            // 表中无数据，先插入一条初始记录
            player = new PlayerCore
            {
                SaveBackupId = -1, // 显式设置为当前未备份状态
                CurrentDay = day,
                CurrentSeason = season
            };
            dbConnection.Insert(player);
        }
        else
        {
            player.CurrentDay = day;
            player.CurrentSeason = season;
            dbConnection.Update(player);
        }
        
    }
    public PlayerCore GetPlayerData()
    {
        return dbConnection.Table<PlayerCore>().FirstOrDefault();
    }
    // 修正版：无重载、无递归，直接存储所有字段
    public void InsertFarmlandTile(int tileX, int tileY, bool isCultivated, bool isWatered, int saveBackupID)
    {
        // 先校验参数（避免无效插入）
        if (dbConnection == null)
        {
            Debug.LogError("数据库连接为空，无法插入耕地数据！");
            return;
        }

        // 新建耕地记录（无任何递归调用）
        var tile = new FarmlandTiles
        {
            TileX = tileX,
            TileY = tileY,
            IsCultivated = isCultivated,
            IsWatered = isWatered,
            SaveBackupID = saveBackupID
        };

        // 执行插入（直接调用SQLite的Insert，无递归）
        try
        {
            dbConnection.Insert(tile);
            Debug.Log($"成功插入耕地数据：({tileX},{tileY})，已耕地：{isCultivated}，已浇水：{isWatered}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"插入耕地数据失败：{e.Message}");
        }
    }
    public void InsertFarmlandTile(int tileX, int tileY, bool isCultivated, int saveBackupID)
    {
        // 调用带IsWatered的方法，默认未浇水（无递归！）
        InsertFarmlandTile(tileX, tileY, isCultivated, false, saveBackupID);
    }
    public List<FarmlandTiles> GetAllFarmlands()
    {
        return dbConnection.Table<FarmlandTiles>().ToList();
    }
    // 插入作物并关联耕地ID
    public int InsertCrop(int farmlandId, string cropType, int saveBackupID)
    {
        var crop = new CropsStatus
        {
            FarmlandId = farmlandId,
            CropType = cropType,
            GrowthStage = 0,
            DaysRemaining = 3,
            SaveBackupID = saveBackupID,
            WateringCount = 0
        };
        return InsertCrop(crop); // 调用重载1，拿到正确ID
    }
    public int InsertCrop(CropsStatus crop)
    {
        if (crop == null) return -1;
        if (crop.SaveBackupID == 0) crop.SaveBackupID = -1; 
        //  关键：SQLite4Unity3d的Insert会返回自增ID，必须接收！
        int newCropId = dbConnection.Insert(crop);
        crop.Id = newCropId; //  把自增ID赋值给crop的Id字段（解决ID=0）
        return newCropId; // 返回ID，供外部使用
    }
    public List<SaveBackups> QueryValidBackups()
    {
        return dbConnection.Table<SaveBackups>()
        .Where(b=>b.IsValid)
        .OrderByDescending(b=>b.SaveTime)
        .ToList();
    }
    public List<CropsStatus> GetCropsByFarmlandId(int farmlandId)
    {
        return dbConnection.Table<CropsStatus>().Where(c => c.FarmlandId == farmlandId).ToList();
    }

    public FarmlandTiles GetFarmlandById(int farmlandId)
    {
        var farmland = dbConnection.Table<FarmlandTiles>().FirstOrDefault(f => f.Id == farmlandId);
        return farmland;
    }
    public void SaveGame(string season,int day,string time)
    {
        
        //保存备份主记录
        var backup=new SaveBackups
        {
            CurrentSeason=season,
            CurrentDay=day,
            SaveTime=time,
            IsValid=true
        };
        dbConnection.Insert(backup);
        int currentBackupId=backup.Id;
        //保存玩家数据
        var player=dbConnection.Table<PlayerCore>().FirstOrDefault(p=>p.SaveBackupId==-1);
        if(player==null)
        {
            player =new PlayerCore
            {
              LastSaveTime=time,
               CurrentDay=day,
               CurrentSeason=season,
               SaveBackupId=currentBackupId
            };
            
            dbConnection.Insert(player);//更新玩家表的存档时间
        }else
        {
            player.LastSaveTime=time;
            player.CurrentDay=day;
            player.CurrentSeason=season;
            player.SaveBackupId=currentBackupId;
            dbConnection.Update(player);  
        }
        //保存当前耕地状态
        var currentFarmlands=dbConnection.Table<FarmlandTiles>().Where(f=>f.SaveBackupID==-1).ToList();
        foreach (var farmland in currentFarmlands)
        {
            InsertFarmlandTile(farmland.TileX,farmland.TileY,farmland.IsCultivated,farmland.IsWatered,currentBackupId); 
        }
        //保存当前作物状态
        var currentCrops=dbConnection.Table<CropsStatus>().Where(c=>c.SaveBackupID==-1).ToList();
        foreach (var crop in currentCrops)
        {
            InsertCrop(crop.FarmlandId,crop.CropType,currentBackupId);
        }
        //背包其他的同理
    }
    /// <summary>
    /// 根据备份id恢复游戏
    /// </summary>
    /// <returns></returns>
    
    
    public bool LoadBackupByBackupId(int backupId)
    {
        var targetBackup = dbConnection.Table<SaveBackups>().FirstOrDefault(b => b.Id == backupId && b.IsValid);

        // 删除当前游戏数据
        DeleteCurrentGameData();

        // 1. 恢复玩家数据
        var backupPlayer = dbConnection.Table<PlayerCore>().FirstOrDefault(p => p.SaveBackupId == backupId);
        if (backupPlayer != null)
        {
            var currentPlayer = new PlayerCore
            {
                CurrentDay = backupPlayer.CurrentDay,
                CurrentSeason = backupPlayer.CurrentSeason,
                CurrentTime = backupPlayer.CurrentTime,
                SaveBackupId = -1
            };
            dbConnection.Insert(currentPlayer);
        }

        // 2. 恢复耕地数据（按坐标插入，生成新的自增ID）
        var backupFarmlands = dbConnection.Table<FarmlandTiles>().Where(f => f.SaveBackupID == backupId).ToList();
        foreach (var farmland in backupFarmlands)
        {
            InsertFarmlandTile(farmland.TileX, farmland.TileY, farmland.IsCultivated, farmland.IsWatered, -1);
        }

        // 3.  核心修改：恢复作物数据（同步FarmlandId为新耕地ID）
        // 修复后的作物恢复核心代码（无CS1061报错）
        var backupCrops = dbConnection.Table<CropsStatus>().Where(c => c.SaveBackupID == backupId).ToList();
        foreach (var crop in backupCrops)
    {
        // 关键：先查备份里的旧耕地（获取坐标），不是从crop取TileY！
        FarmlandTiles oldFarmland = dbConnection.Table<FarmlandTiles>()
            .FirstOrDefault(f => f.Id == crop.FarmlandId && f.SaveBackupID == backupId);
        
        if (oldFarmland == null)
        {
            continue;
        }

        // 按旧耕地的坐标，找当前游戏的新耕地（SaveBackupID=-1）
        FarmlandTiles newFarmland = GetFarmlandByTilePos(oldFarmland.TileX, oldFarmland.TileY);
        if (newFarmland == null)
        {
            continue;
        }

        // 用新耕地ID创建作物，同步关联
        CropsStatus newCrop = new CropsStatus
        {
            FarmlandId = newFarmland.Id, // 核心：用新耕地ID
            CropType = crop.CropType,
            GrowthStage = crop.GrowthStage,
            DaysRemaining = crop.DaysRemaining,
            TotalGrowthDays = crop.TotalGrowthDays,
            WateringCount = crop.WateringCount,
            SaveBackupID = -1
        };
        InsertCrop(newCrop);
    }
        CleanDuplicateCrops();
        return true;
    }
    
    public void DeleteCurrentGameData()
    {
        if (dbConnection == null) return;
        // 条件删除：执行原生SQL，确保删干净未备份（SaveBackupID=-1）的所有数据
        try
        {
            int delPlayer = dbConnection.Execute("DELETE FROM PlayerCore WHERE SaveBackupId = ?", -1);
            int delFarmland = dbConnection.Execute("DELETE FROM FarmlandTiles WHERE SaveBackupID = ?", -1);
            int delCrop = dbConnection.Execute("DELETE FROM CropsStatus WHERE SaveBackupID = ?", -1);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"清空数据失败：{e.Message}");
        }
    }
        /// <summary>
    /// 软删除备份（标记IsValid=0，不实际删除数据）
    /// </summary>
    public void DeleteBackup(int backupId)
    {
        var backup = dbConnection.Table<SaveBackups>().FirstOrDefault(b => b.Id == backupId);
        if (backup != null)
        {
            backup.IsValid = false;
            dbConnection.Update(backup);
        }
    }
    // 关闭数据库连接（可选，退出游戏时调用）
    private void OnDestroy()
    {
        if (dbConnection != null)
        {
            dbConnection.Commit(); //  强制提交所有数据（退出时必存）
            dbConnection.Close();
        }
    }
    public void UpdateFarmland(FarmlandTiles farmland)
    {
        try
        {
            dbConnection.Update(farmland);
            Debug.Log($"🔍 【数据库】更新耕地成功，ID：{farmland.Id}，状态：{(farmland.IsCultivated ? "已开垦" : "未开垦")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 【数据库】更新耕地失败，ID：{farmland.Id}，错误：{e.Message}");
        }
    }
        // 新增：获取所有作物
    public List<CropsStatus> GetAllCrops()
    {
        return dbConnection.Table<CropsStatus>().ToList();
    }

    // 新增：更新作物
    public void UpdateCrop(CropsStatus crop)
    {
        if (crop != null) dbConnection.Update(crop);
    }
    #region 作物采集逻辑
    /// <summary>
    /// 删除单个作物状态
    /// </summary>
    /// <param name="cropId"></param>
    public void DeleteCropStatusById(int cropId)
    {
        if(dbConnection==null)
        {
            return;
        }
        try
        {
            // 按ID删除指定作物数据
            dbConnection.Execute("DELETE FROM CropsStatus WHERE Id = ?", cropId);
            Debug.Log($"成功删除作物数据（ID：{cropId}）");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除作物数据失败：{e.Message}");
        }
    }
    /// <summary>
    /// 更新作物浇水次数（12天的作物）
    /// </summary>
    /// <param name="cropId"></param>
    /// <returns></returns>
    public int UpdateCropWateringCount(int cropId)
    {
        if(dbConnection==null)
        {
            return -1;
        }
        //获取作物数据
        var crop=dbConnection.Table<CropsStatus>().FirstOrDefault(c=>c.Id==cropId);
        if(crop==null)
        {
            return -1;
        }
        //浇水次数
        crop.WateringCount +=1;
        dbConnection.Update(crop);
        return crop.WateringCount;
    }
    /// <summary>
    /// 重置作物浇水12天的
    /// </summary>
    /// <param name="cropId"></param>
    public void ResetCropWateringCount(int cropId)
    {
        var crop=dbConnection.Table<CropsStatus>().FirstOrDefault(c=>c.Id==cropId);
        if(crop==null) return;
        crop.WateringCount=0;
        dbConnection.Update(crop);
    }
    /// <summary>
    /// 背包
    /// </summary>
    /// <param name="cropType"></param>
    /// <param name="count"></param>
    public void AddSeedToDB(string cropType, int count)
    {
        
    }
    #endregion
    
    /// <summary>
    /// 按坐标（TileX/TileY）获取耕地（解决自增ID不匹配的核心）
    /// </summary>
    public FarmlandTiles GetFarmlandByTilePos(int tileX, int tileY)
    {
        if (dbConnection == null) return null;
        return dbConnection.Table<FarmlandTiles>()
            .FirstOrDefault(f => f.TileX == tileX && f.TileY == tileY && f.SaveBackupID == -1);
    }

    /// <summary>
    /// 清理重复作物数据：同一耕地（SaveBackupID=-1）仅保留最新一条（按Id降序）
    /// </summary>
    public void CleanDuplicateCrops()
    {

        try
        {
            // 1. 先查询所有重复的作物记录（同一FarmlandId+SaveBackupID=-1，存在多条）
            var duplicateFarmlandIds = dbConnection.Query<int>(@"
                SELECT FarmlandId 
                FROM CropsStatus 
                WHERE SaveBackupID = -1 
                GROUP BY FarmlandId 
                HAVING COUNT(*) > 1
            ");

            if (duplicateFarmlandIds.Count == 0)
            {
                Debug.Log("无重复作物数据，无需清理");
                return;
            }

            // 2. 逐个耕地清理，仅保留最新一条（Id最大的那条）
            foreach (int farmlandId in duplicateFarmlandIds)
            {
                // 获取该耕地的所有当前作物记录，按Id降序排序
                var crops = dbConnection.Table<CropsStatus>()
                    .Where(c => c.FarmlandId == farmlandId && c.SaveBackupID == -1)
                    .OrderByDescending(c => c.Id)
                    .ToList();

                if (crops.Count <= 1) continue;

                // 保留第一条（最新），删除其余所有重复记录
                for (int i = 1; i < crops.Count; i++)
                {
                    dbConnection.Delete(crops[i]);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"清理重复作物失败：{e.Message}");
        }
    }
    #region 背包数据管理
    /// <summary>
    /// 查询当前所有物品
    /// </summary>
    /// <returns></returns>
    public List<BackpackItems> QueryBackpackItems()
    {
        if(dbConnection==null) return new List<BackpackItems>();
        return dbConnection.Table<BackpackItems>()
            .Where(item =>item.SaveBackupId==-1)
            .ToList();
    }
    /// <summary>
    /// 根据物品类型获取背包物品
    /// </summary>
    /// <param name="itemType"></param>
    /// <returns></returns>
    public BackpackItems GetBackpackItemByType(string itemType)
    {
        if(dbConnection==null) return null;
        return dbConnection.Table<BackpackItems>()
            .FirstOrDefault(item=>item.ItemType==itemType&&item.SaveBackupId==-1);
    }
    /// <summary>
    /// 添加物品（支持叠加：同类型物品累加数量，无则新增）
    /// </summary>
    /// <param name="itemType">物品类型（如Wheat、Tomato_Seed）</param>
    /// <param name="count">添加数量</param>
    public void AddItem(string itemType, int count)
    {
        try
        {
            var existingItem = dbConnection.Table<BackpackItems>()
                .FirstOrDefault(item => item.ItemType == itemType);

            if (existingItem != null)
            {
                existingItem.ItemCount += count;
                dbConnection.Update(existingItem);
                Debug.Log($" 【背包数据库】叠加物品：{itemType}，当前数量：{existingItem.ItemCount}");
            }
            else
            {
                BackpackItems newItem = new BackpackItems
                {
                    ItemType = itemType,
                    ItemCount = count,
                    SaveBackupId = -1 // 必须标记为有效数据，否则LoadBackpackItems读取不到
                };
                dbConnection.Insert(newItem);
                Debug.Log($" 【背包数据库】新增物品：{itemType}，数量：{count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($" 【背包数据库】添加物品失败：{e.Message}");
        }
    }
    /// <summary>
    /// 更新物品
    /// </summary>
    /// <param name="itemType"></param>
    /// <param name="deltaCount"></param>
    public void UpdateItemCount(string itemType, int deltaCount)
    {
        if(string.IsNullOrEmpty(itemType)||dbConnection==null)return;

        var existItem=GetBackpackItemByType(itemType);
        if(existItem==null)
        {
            return;
        }

        existItem.ItemCount+=deltaCount;
        if(existItem.ItemCount<=0)
        {
            dbConnection.Delete(existItem);
        }
        else
        {
            dbConnection.Update(existItem);
        }
    }
    #endregion
    /// <summary>
    /// 删除背包中指定类型的物品（数量为0时调用）
    /// </summary>
    public void DeleteBackpackItem(string itemType)
    {
        try
        {
            dbConnection.Delete<BackpackItems>($"WHERE ItemType = '{itemType}'");
            Debug.Log($" 成功删除背包物品：{itemType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($" 删除背包物品失败：{e.Message}");
        }
    }
    public void InitFarmlandDataFromTilemap(Tilemap farmlandTilemap)
{
    // 清空旧有效耕地数据（SaveBackupID=-1为当前有效）
    int delCount = dbConnection.Delete<FarmlandTiles>("WHERE SaveBackupID = -1");

    int genCount = 0;
    BoundsInt bounds = farmlandTilemap.cellBounds;
    foreach (Vector3Int cellPos in bounds.allPositionsWithin)
    {
        TileBase tile = farmlandTilemap.GetTile(cellPos);
        if (tile != null) // 有Tile的位置视为耕地（可按你的Tile类型筛选）
        {
            FarmlandTiles farmland = new FarmlandTiles
            {
                TileX = cellPos.x,
                TileY = cellPos.y,
                IsCultivated = false,
                IsWatered = false,
                SaveBackupID = -1
            };
            dbConnection.Insert(farmland);
            genCount++;
        }
    }
    int validCount = dbConnection.Table<FarmlandTiles>().Count(f => f.SaveBackupID == -1);
}
    #region 任务数据管理
    /// <summary>
    /// 插入玩家任务（当前有效任务：SaveBackupID=-1）
    /// </summary>
    public int InsertPlayerTask(PlayerTasks task)
    {
        if (dbConnection == null)
        {
            return -1;
        }
        if (task == null)
        {
            return -1;
        }
        // 强制标记为当前有效任务
        task.SaveBackupID = -1;
        try
        {
            int insertId = dbConnection.Insert(task);
            // 验证：插入后立即查询，确认是否存入
            var checkTask = dbConnection.Table<PlayerTasks>().FirstOrDefault(t => t.Id == insertId);
            if (checkTask != null)
            {
                Debug.Log($"任务插入成功并验证：ID={insertId}，名称={checkTask.TaskName}");
            }
            else
            {
                Debug.LogError($" 任务插入返回ID={insertId}，但数据库中查询不到！");
                return -1;
            }
            return insertId;
        }
        catch (System.Exception e)
        {
            Debug.LogError($" 插入任务失败：{e.Message}");
            return -1;
        }
    }

    /// <summary>
    /// 获取当前有效任务（SaveBackupID=-1）
    /// </summary>
    public List<PlayerTasks> GetCurrentPlayerTasks()
    {
        if (dbConnection == null)
        {
            return new List<PlayerTasks>();
        }
        var tasks = dbConnection.Table<PlayerTasks>()
            .Where(t => t.SaveBackupID == -1) // 仅查当前有效任务
            .OrderBy(t => t.DayAssigned)
            .ToList();
        return tasks;
    }

    /// <summary>
    /// 更新任务进度/状态
    /// </summary>
    public bool UpdatePlayerTask(PlayerTasks task)
    {
        if (dbConnection == null || task == null)
        {
            Debug.LogError("数据库连接或任务对象为空，无法更新任务！");
            return false;
        }
        try
        {
            int updateCount = dbConnection.Update(task);
            Debug.Log($" 任务更新结果：{task.TaskName}，影响行数={updateCount}");
            return updateCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($" 更新任务失败：{e.Message}");
            return false;
        }
    }
    #endregion
}   
