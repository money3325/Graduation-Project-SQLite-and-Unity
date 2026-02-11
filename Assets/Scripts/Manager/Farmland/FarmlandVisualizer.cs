using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 农场视觉管理器（耕地/浇水图标/作物视觉）
/// </summary>
public class FarmlandVisualizer : MonoBehaviour
{
    [Header("核心Tilemap引用（拖入）")]
    public Tilemap farmlandTilemap; // 农场土地主Tilemap
    public Tilemap statusIconTilemap; // 状态图标（浇水/耕地）Tilemap

    [Header("视觉资源（拖入对应Tile）")]
    public TileBase cultivatedTile; // 【已耕地Tile】拖入你的耕地Tile资源
    public TileBase waterIconTile;  // 【浇水图标Tile】拖入你的浇水图标Tile资源

    /// <summary>
    /// 更新耕地视觉（切换已耕地/未耕地Tile）
    /// </summary>
    public void UpdateFarmlandVisual(FarmlandTiles farmland)
    {

        // 基础校验
        if (farmlandTilemap == null || farmland == null)
        {
            Debug.LogError(" 【视觉更新】farmlandTilemap或farmland为空，无法更新");
            return;
        }
        if (cultivatedTile == null)
        {
            Debug.LogError(" 【视觉更新】未拖入【已耕地Tile】，请在Inspector中配置！");
            return;
        }

        // 切换Tile（先清空再设置，避免缓存）
        Vector3Int cellPos = new Vector3Int(farmland.TileX, farmland.TileY, 0);
        farmlandTilemap.SetTile(cellPos, null);
        farmlandTilemap.SetTile(cellPos, farmland.IsCultivated ? cultivatedTile : null);
        farmlandTilemap.RefreshTile(cellPos); // 强制刷新Tilemap
    }

    /// <summary>
    /// 显示浇水图标
    /// </summary>
    public void ShowWaterIcon(Vector3Int cellPos)
    {
        // 校验Tilemap和图标资源
        if (statusIconTilemap == null) return;
        if (waterIconTile == null)
        {
            Debug.LogError("⚠️ 未拖入【浇水图标Tile】资源，请在FarmlandVisualizer的Inspector中配置！");
            return;
        }

        statusIconTilemap.SetTile(cellPos, waterIconTile);
    }

    /// <summary>
    /// 清空所有状态图标（新一天时调用，来自CropManager）
    /// </summary>
    public void ClearAllStatusIcons()
    {
        if (statusIconTilemap == null) return;
        BoundsInt bounds = statusIconTilemap.cellBounds;
        foreach (Vector3Int cellPos in bounds.allPositionsWithin)
        {
            statusIconTilemap.SetTile(cellPos, null);
        }
    }
}