using SQLite4Unity3d;

[Table("YarnDislogueVars")]
public class YarnDislogueVars
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//变量id
    public string VarName { get; set; }//yarn变量名
    public string VarValue { get; set; }//变量值
    public string RelatedSystem { get; set; }//关联系统
    public int SaveBackupID { get; set; } = -1; // -1=当前有效，其他为备份ID

    
}
