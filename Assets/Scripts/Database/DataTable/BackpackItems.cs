using SQLite4Unity3d;

[Table("BackpackItems")] 
public class BackpackItems
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//每个格子的id
    public string ItemType { get; set; }//物品类型，如作物，工具
    public string ItemName { get; set; }//物品名称
    public int ItemCount { get; set; }//物品数量
    public string ItemDesc { get; set; }//物品描述，后期与yarnspinner
    public int SaveBackupId { get; set; } 
}
