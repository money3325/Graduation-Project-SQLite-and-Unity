using SQLite4Unity3d;

public class CropAtlas
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//作物表图鉴id
    public string CropName { get; set; }//作物名称
    public string SeedName { get; set; }//种子名称
    public int TotalGrowthDays { get; set; }//总生长天数
}
