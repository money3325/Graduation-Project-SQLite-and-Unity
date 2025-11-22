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
    private SQLiteConnection dbConnection;
    //对于每一帧，初始化数据库连接，将表放到这个里面
    void Awake()
    {
        string dbPath = Application.persistentDataPath + "/GameData.db";
        dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        CreateAllTables();
          // 仅当表中无数据时，插入初始耕地（避免重复）
    if (dbConnection.Table<FarmlandTiles>().Count() == 0)
    {
        InsertFarmlandTile(2, 3, true);
        Debug.Log("已插入初始耕地数据");
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
            player = new PlayerCore();
            dbConnection.Insert(player);
        }
        player.CurrentDay = day;
        player.CurrentSeason = season;
        dbConnection.Update(player);
    }
    public PlayerCore GetPlayerData()
    {
        return dbConnection.Table<PlayerCore>().FirstOrDefault();
    }
    public void InsertFarmlandTile(int tileX, int tileY, bool isCultivated)
    {
        var tile = new FarmlandTiles
        {
            TileX = tileX,
            TileY = tileY,
            IsCultivated = isCultivated
        };
        dbConnection.Insert(tile);
    }
    public List<FarmlandTiles> GetAllFarmlands()
    {
        return dbConnection.Table<FarmlandTiles>().ToList();
    }
    // 插入作物并关联耕地ID
public void InsertCrop(int farmlandId, string cropType)
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
    public List<CropsStatus> GetCropsByFarmlandId(int farmlandId)
    {
        return dbConnection.Table<CropsStatus>().Where(c => c.FarmlandId == farmlandId).ToList();
    }

    public FarmlandTiles GetFarmlandById(int farmlandId)
    {
        return dbConnection.Table<FarmlandTiles>().Where(f => f.Id == farmlandId).FirstOrDefault();
    }

}   
