using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// 农场土地输入处理器（无射线，纯Tilemap坐标转换处理点击）
/// </summary>
public class FarmlandInputHandler : MonoBehaviour
{
    [Header("依赖引用（拖入）")]
    public Tilemap farmlandTilemap; // 农场土地Tilemap（和CropManager中的一致）
    public FarmlandVisualizer farmlandVisualizer; // 农场视觉管理器
    public CropManager cropManager; // 作物管理器
    public BackpackManager backpackManager; // 背包管理器

    void Start()
    {
        // 强制初始化（先清空旧数据，再生成，测试用，正式版可加表空判断）
        DBManager.Instance.InitFarmlandDataFromTilemap(farmlandTilemap);
    }
    private void Update()
    {
        // 仅处理鼠标左键点击（避免重复操作）
        if (Input.GetMouseButtonDown(0) && !IsClickingUI())
        {
            HandleFarmlandClick();
        }
    }

    /// <summary>
    /// 处理农场土地点击（核心：无射线，Tilemap坐标转换）
    /// </summary>
    private void HandleFarmlandClick()
    {

        // 1. 屏幕→世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 强制2D Z轴为0，避免偏移

        // 2. 世界→Tile单元格
        Vector3Int cellPos = farmlandTilemap.WorldToCell(mouseWorldPos);

        // 3. 查找耕地数据
        FarmlandTiles targetFarmland = DBManager.Instance.GetFarmlandByTilePos(cellPos.x, cellPos.y);

        // 4. 执行对应模式操作
        ExecuteOperationByBackpackMode(targetFarmland, cellPos);
    }

    /// <summary>
    /// 根据当前背包模式执行对应操作（耕地/浇水/种植）
    /// </summary>
    private void ExecuteOperationByBackpackMode(FarmlandTiles farmland, Vector3Int cellPos)
    {
        switch (backpackManager.currentMode)
        {
            case BackpackManager.BackpackMode.Cultivate:
                HandleCultivate(farmland);
                break;
            case BackpackManager.BackpackMode.Water:
                HandleWater(farmland, cellPos);
                break;
            case BackpackManager.BackpackMode.Plant:
                cropManager.TryPlantCrop(cellPos, farmland);
                break;
            case BackpackManager.BackpackMode.None:
                break;
        }
    }

    /// <summary>
    /// 处理耕地操作（锄头模式）
    /// </summary>
    private void HandleCultivate(FarmlandTiles farmland)
    {

        // 校验：已开垦则跳过
        if (farmland.IsCultivated)
        {
            Debug.LogWarning($"【耕地逻辑】耕地ID={farmland.Id}已开垦，无需重复操作");
            return;
        }

        // 1. 更新数据库耕地状态
        farmland.IsCultivated = true;
        DBManager.Instance.UpdateFarmland(farmland);

        // 2. 更新视觉（切换耕地Tile）
        if (farmlandVisualizer != null)
        {
            farmlandVisualizer.UpdateFarmlandVisual(farmland);
        }
    }

    /// <summary>
    /// 处理浇水操作（浇水壶模式）
    /// </summary>
    private void HandleWater(FarmlandTiles farmland, Vector3Int cellPos)
    {
        // 校验条件：已耕地、未浇水、有作物（可选，根据需求调整）
        if (!farmland.IsCultivated)
        {
            Debug.LogWarning($" 耕地ID={farmland.Id}未开垦，无法浇水");
            return;
        }
        if (farmland.IsWatered)
        {
            Debug.LogWarning($" 耕地ID={farmland.Id}已浇水，无需重复浇水");
            return;
        }

        // 1. 更新数据库浇水状态
        farmland.IsWatered = true;
        DBManager.Instance.UpdateFarmland(farmland);

        // 2. 显示浇水图标（视觉反馈）
        if (farmlandVisualizer != null && farmlandVisualizer.statusIconTilemap != null)
        {
            farmlandVisualizer.ShowWaterIcon(cellPos);
        }

        // 3. 处理12天作物浇水后生长（触发CropCollect的后续逻辑）
        HandleTwelveDaysCropWater(farmland);
    }

    /// <summary>
    /// 处理12天作物的浇水逻辑（可选，对应你现有12天作物循环逻辑）
    /// </summary>
    private void HandleTwelveDaysCropWater(FarmlandTiles farmland)
    {
        // 查找该耕地对应的作物
        var crop = DBManager.Instance.dbConnection.Table<CropsStatus>()
            .FirstOrDefault(c => c.FarmlandId == farmland.Id && c.SaveBackupID == -1);
        if (crop == null || crop.GrowthStage != 3)
        {
            return;
        }

        // 查找作物实例上的CropCollect脚本并触发浇水回调
        GameObject cropInst = cropManager.cropInstances.TryGetValue(crop.Id, out var inst) ? inst : null;
        if (cropInst != null)
        {
            CropCollect cropCollect = cropInst.GetComponent<CropCollect>();
            cropCollect?.OnWateringAfterMature();
        }
    }

    /// <summary>
    /// 判断是否点击了UGUI（避免点击UI时触发场景操作）
    /// </summary>
    // FarmlandInputHandler.cs
    private bool IsClickingUI()
    {

        // PC端鼠标点击
        if (Input.touchCount == 0)
        {
            bool isUI = EventSystem.current.IsPointerOverGameObject();
            return isUI;
        }
        // 移动端触摸
        else
        {
            bool isUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return isUI;
        }
    }
}
