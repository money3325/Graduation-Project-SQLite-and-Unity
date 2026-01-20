using SQLite4Unity3d;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        instance=this;
        DontDestroyOnLoad(gameObject);
        if(dbConnection==null)
        {
            string dbPath = Application.persistentDataPath + "/GameData.db";
            dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            CreateAllTables();
            // 仅当表中无数据时，插入初始耕地（避免重复）
            /*if (dbConnection.Table<FarmlandTiles>().Count() == 0)
            {
                InsertFarmlandTile(2, 3, true, false, -1); 
                Debug.Log("已插入初始耕地数据");
            }*/
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
    // 🔥 修正版：无重载、无递归，直接存储所有字段
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
public void InsertCrop(int farmlandId, string cropType,int saveBackupID)
{
    var crop = new CropsStatus
    {
        FarmlandId = farmlandId,
        CropType = cropType,
        GrowthStage = 0, // 初始为种子阶段
        DaysRemaining = 3 // 假设3天成熟
    };
    dbConnection.Insert(crop);
    
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
        return dbConnection.Table<FarmlandTiles>().Where(f => f.Id == farmlandId).FirstOrDefault();
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
        var targetBackup=dbConnection.Table<SaveBackups>().FirstOrDefault(b=>b.Id==backupId&&b.IsValid);
        if(targetBackup==null)
        {
            return false;
        }
        //删除当前游戏状态
        DeleteCurrentGameData();
        //恢复玩家数据
        var backupPlayer=dbConnection.Table<PlayerCore>().FirstOrDefault(p=>p.SaveBackupId==backupId);
        if (backupPlayer!=null)
        {
            var currentPalyer=new PlayerCore
            {
              CurrentDay=backupPlayer.CurrentDay,
              CurrentSeason=backupPlayer.CurrentSeason,
              CurrentTime=backupPlayer.CurrentTime,
              SaveBackupId=-1  
            };
            dbConnection.Insert(currentPalyer);
        }
        //恢复耕地信息
        var backupFarmlands=dbConnection.Table<FarmlandTiles>().Where(f=>f.SaveBackupID==backupId).ToList();
        foreach (var farmland in backupFarmlands)
        {
            InsertFarmlandTile(farmland.TileX,farmland.TileY,farmland.IsCultivated,farmland.IsWatered,-1); 
        }
        var backupCrops=dbConnection.Table<CropsStatus>().Where(c=>c.SaveBackupID==backupId).ToList();
        foreach (var crop in backupCrops)
        {
            InsertCrop(crop.FarmlandId,crop.CropType,-1);
        }
        return true;
    }
    private void DeleteCurrentGameData()
{
    // 条件删除：执行原生SQL（推荐，高效）
    dbConnection.Execute("DELETE FROM PlayerCore WHERE SaveBackupId = ?", -1);
    dbConnection.Execute("DELETE FROM FarmlandTiles WHERE SaveBackupID = ?", -1);
    dbConnection.Execute("DELETE FROM CropsStatus WHERE SaveBackupID = ?", -1);
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
            dbConnection.Close();
        }
    }
    public void UpdateFarmland(FarmlandTiles farmland)
    {
        if(farmland!=null)
        {
            dbConnection.Update(farmland);
        }
    }
        // 新增：获取所有作物
    public List<CropsStatus> GetAllCrops()
    {
        return dbConnection.Table<CropsStatus>().ToList();
    }

    // 新增：插入作物（适配新字段）
    public void InsertCrop(CropsStatus crop)
    {
        if (crop != null) dbConnection.Insert(crop);
    }

    // 新增：更新作物
    public void UpdateCrop(CropsStatus crop)
    {
        if (crop != null) dbConnection.Update(crop);
    }

}   
