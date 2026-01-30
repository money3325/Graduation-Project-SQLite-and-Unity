using UnityEngine;
using System.Linq;

public class TestResetData : MonoBehaviour
{
    [Header("需要重置的管理器（拖入场景中的对应物体）")]
    public DBManager dbManager;
    public CropManager cropManager;
    public FarmlandVisualizer farmlandManager;
    public TimeManager timeManager;
    

    void Update()
    {
        // 按空格键触发重置
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetAllData();
        }
    }

    // 核心：重置所有数据（场景+数据库），保留Tilemap原始地图
    void ResetAllData()
    {
        Debug.Log("\n===== 开始重置所有数据（保留原始地图） =====");

        // 1. 清理场景中的所有作物预制体
        ClearCropInstances();

        // 2. 重置数据库（清空作物、重置耕地状态，保留地图）
        ResetDatabase();

        // 3. 重置管理器状态（仅清空浇水图标，不改动原始地图）
        ResetManagers();

        // 4. 重置时间为初始状态（第1天，春天，6点）
        ResetTime();

        Debug.Log("✅ 所有数据重置完成！原始地图已保留");
    }

    // 清理场景中的作物实例
    void ClearCropInstances()
    {
        if (cropManager == null || cropManager.cropParent == null) return;

        // 销毁所有作物预制体
        foreach (Transform child in cropManager.cropParent)
        {
            Destroy(child.gameObject);
        }
        // 清空实例字典
        cropManager.cropInstances.Clear();
        Debug.Log("🗑️ 场景作物实例已清理");
    }

    // 重置数据库核心逻辑（仅重置状态，不删地图）
    void ResetDatabase()
    {
        if (dbManager == null || dbManager.dbConnection == null) return;

        // 清空作物表
        dbManager.dbConnection.Execute("DELETE FROM CropsStatus");
        
        // 重置耕地表：保留格子记录，仅把IsCultivated/IsWatered设为false（不删地图）
        var allFarmlands = dbManager.GetAllFarmlands();
        if (allFarmlands != null && allFarmlands.Count > 0)
        {
            foreach (var farmland in allFarmlands)
            {
                farmland.IsCultivated = false; // 重置为未耕地
                farmland.IsWatered = false;    // 重置为未浇水
                dbManager.UpdateFarmland(farmland);
            }
            Debug.Log("🗄️ 耕地状态已重置为未耕地/未浇水（保留地图格子）");
        }
        
        // 重置玩家数据为初始状态（第1天，春天，6点）
        dbManager.dbConnection.Execute("DELETE FROM PlayerCore");
        //dbManager.UpdatePlayer("春", 1);

        Debug.Log("🗄️ 数据库已重置（作物清空/耕地状态重置/玩家时间重置）");
    }

    // 重置管理器状态（仅清空浇水图标，不改动原始地图）
    void ResetManagers()
    {
        // 重置CropManager
        if (cropManager != null)
        {
            //cropManager.selectedCrop = null;
            //cropManager.isSinglePlantMode = false;
        }

        // 🔥 关键修改：仅清空浇水图标（statusIconTilemap），不改动farmlandTilemap的原始地图
        if (farmlandManager != null && farmlandManager.statusIconTilemap != null)
        {
            BoundsInt bounds = farmlandManager.statusIconTilemap.cellBounds;
            foreach (Vector3Int cellPos in bounds.allPositionsWithin)
            {
                farmlandManager.statusIconTilemap.SetTile(cellPos, null); // 仅清空浇水水滴图标
            }
            Debug.Log("💧 浇水图标已清空（原始地图保留）");
        }
    }

    // 重置时间为初始状态
    void ResetTime()
    {
        if (timeManager != null)
        {
            timeManager.currentSeason = "春";
            timeManager.currentDay = 1;
            timeManager.currentHour = 6;
            timeManager.UpdateTimeUI();
            timeManager.UpdateMaskColor();
            Debug.Log("⏰ 时间已重置为：春第1天 06:00");
        }
    }
}