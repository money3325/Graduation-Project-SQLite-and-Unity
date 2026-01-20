using SQLite4Unity3d;

[Table("FarmlandTiles")]
public class FarmlandTiles
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//耕地唯一id
    public int TileX { get; set; }//耕地在x的位置
    public int TileY { get; set; }//耕地在y的位置
    public bool IsCultivated { get; set; }//是否耕地
    public bool IsWatered{ get; set; }=false;//是否浇水
    public int SaveBackupID{get;set;}//关联backup
}
