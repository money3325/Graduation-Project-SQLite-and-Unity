using SQLite4Unity3d;

public class CropsStatus
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//作物唯一标识
    public string CropType { get; set; }//作物类型，如小麦
    public int FarmlandId { get; set; }//与耕地id关联，作物要种在耕地块里
    public int GrowthStage { get; set; }//生长阶段（种子0，幼苗1，成熟2）
    public int DaysRemaining { get; set; }//距离成熟的天数
}
