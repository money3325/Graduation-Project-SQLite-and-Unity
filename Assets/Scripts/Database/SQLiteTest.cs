using UnityEngine;
using SQLite4Unity3d;

public class SQLiteTest : MonoBehaviour
{
    private SQLiteConnection _dbConnection;

    void Start()
    {
        // 正确路径：持久化路径（可读写）
        string dbPath = Application.persistentDataPath + "/test_db.db";
        // 初始化连接（自动创建数据库文件）
        _dbConnection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

        // 后续表创建、数据插入/查询逻辑不变（参考之前的SQLiteTest.cs代码）
        _dbConnection.CreateTable<Vector2Data>();
        Vector2Data testData = new Vector2Data { X = 5.2f, Y = 10.5f };
        _dbConnection.Insert(testData);
        var result = _dbConnection.Query<Vector2Data>("SELECT * FROM Vector2Data");
        foreach (var data in result)
        {
            Debug.Log($"查询到2D坐标：X = {data.X}, Y = {data.Y}");
        }
    }

    public class Vector2Data
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }
}