using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class FarmlandVisualizer : MonoBehaviour
{
    [Header("拖入对应Tilemap")]
    public Tilemap farmlandTilemap; // 耕地Tilemap
    public Tilemap statusIconTilemap; // 水滴Tilemap

    [Header("拖入对应Rule Tile")]
    public TileBase unCultivatedTile; // 未耕地
    public TileBase cultivatedTile; // 已耕地
    public TileBase waterDropTile; // 水滴

    public CropManager cropManager; // 拖入CropManager
    private Camera mainCamera;
    private float lastClickTime = 0f; // 新增：点击冷却

    void Start()
    {
        mainCamera = Camera.main;

        // 校验配置
        if (farmlandTilemap == null || statusIconTilemap == null)
        {
            Debug.LogError("请拖入耕地和状态图标Tilemap！");
            return;
        }
        if (unCultivatedTile == null || cultivatedTile == null || waterDropTile == null)
        {
            Debug.LogError("请拖入未耕地/已耕地/水滴Rule Tile！");
            return;
        }

        // 从数据库加载耕地状态（用你DBManager里的GetAllFarmlands方法）
        InitFarmlandFromDB();
    }

    // 从数据库加载耕地状态，同步到Tilemap
    private void InitFarmlandFromDB()
    {
        if (DBManager.Instance == null)
        {
            Debug.LogError("DBManager未初始化！");
            return;
        }

        var allFarmlands = DBManager.Instance.GetAllFarmlands(); // 用你现有的方法名
        if (allFarmlands == null || allFarmlands.Count == 0)
        {
            Debug.Log("数据库暂无耕地数据");
            return;
        }

        // 遍历数据库记录，同步显示
        foreach (var farmland in allFarmlands)
        {
            Vector3Int cellPos = new Vector3Int(farmland.TileX, farmland.TileY, 0);
            // 同步耕地状态
            farmlandTilemap.SetTile(cellPos, farmland.IsCultivated ? cultivatedTile : unCultivatedTile);
            // 同步浇水状态
            statusIconTilemap.SetTile(cellPos, farmland.IsWatered ? waterDropTile : null);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleTileClick();
        }
    }

    // 点击交互：普通点击耕地，Shift点击浇水，同步数据库
    private void HandleTileClick()
    {
        // 🔥 加防重复点击（1秒内仅响应一次）
        if (Time.time - lastClickTime < 1f) return;
        lastClickTime = Time.time;
        if (mainCamera == null || DBManager.Instance == null) return;

        // 转换鼠标坐标到Tilemap格子
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        if (hit.collider == null || hit.collider.gameObject != farmlandTilemap.gameObject)
            return;

        Vector3Int cellPos = farmlandTilemap.WorldToCell(mouseWorldPos);
        int tileX = cellPos.x;
        int tileY = cellPos.y;

        // 查找该格子的数据库记录（无则新建）
        var farmland = DBManager.Instance.GetAllFarmlands()
            .FirstOrDefault(f => f.TileX == tileX && f.TileY == tileY);

        if (farmland == null)
        {
            // 新建记录：默认未耕地、未浇水
            DBManager.Instance.InsertFarmlandTile(tileX, tileY, false, false, -1);
            farmland = DBManager.Instance.GetAllFarmlands()
                .FirstOrDefault(f => f.TileX == tileX && f.TileY == tileY);
        }
        // 优先处理「播种」（已选种子时）
        if (cropManager != null && cropManager.isSinglePlantMode)
        {
            cropManager.TryPlantCrop(cellPos, farmland);
            return;
        }

    // 3. 普通点击：耕地（修复逻辑，更可靠）
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // 浇水：仅已耕地可浇水
            if (!farmland.IsCultivated)
            {
                Debug.Log("请先耕地再浇水");
                return;
            }
            farmland.IsWatered = !farmland.IsWatered;
            DBManager.Instance.UpdateFarmland(farmland);
            statusIconTilemap.SetTile(cellPos, farmland.IsWatered ? waterDropTile : null);
            Debug.Log($"耕地({tileX},{tileY})浇水状态：{farmland.IsWatered}");
        }
        else
        {
            // 耕地：未耕地→已耕地
            if (!farmland.IsCultivated)
            {
                farmland.IsCultivated = true;
                DBManager.Instance.UpdateFarmland(farmland);
                farmlandTilemap.SetTile(cellPos, cultivatedTile);
                Debug.Log($"耕地({tileX},{tileY})已开垦");
            }
            else
            {
                Debug.Log($"耕地({tileX},{tileY})已是已耕地");
            }
        }
        
    }
}