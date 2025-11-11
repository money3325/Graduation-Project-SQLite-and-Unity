using SQLite4Unity3d;
using System.Collections.Generic;
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
}   
