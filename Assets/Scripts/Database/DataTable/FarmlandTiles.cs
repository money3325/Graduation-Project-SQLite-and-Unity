using SQLite4Unity3d;

public class FarmlandTiles
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//耕地唯一id
    public int TileX { get; set; }//耕地在x的位置
    public int TileY { get; set; }//耕地在y的位置
    public int IsCultivated { get; set; }//是否耕地
    public int IsWatered{ get; set; }//是否浇水
}
