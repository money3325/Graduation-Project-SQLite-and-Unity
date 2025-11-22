using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmlandVisualizer : MonoBehaviour
{
    [SerializeField] private Tilemap farmlandTilemap;
    [SerializeField] private TileBase farmlandTile;
    void Start()
    {
        VisualizeAllFarmlands(); 
    }
    //从数据库查询耕地，在地图上标记
    public void VisualizeAllFarmlands()
    {
        List<FarmlandTiles> allFarmlands = DBManager.Instance.GetAllFarmlands();
        foreach (var farmland in allFarmlands)
        {
            // 转换耕地坐标为Tilemap的世界位置（Grid的Cell Size为1时，直接用Vector3Int）
            Vector3Int tilePosition = new Vector3Int(farmland.TileX, farmland.TileY, 0);
            
            // 在Tilemap上放置耕地瓦片（标记）
            farmlandTilemap.SetTile(tilePosition, farmlandTile);
        }
    }
 
}
