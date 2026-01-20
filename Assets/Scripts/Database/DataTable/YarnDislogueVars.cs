using SQLite4Unity3d;

[Table("YarnDislogueVars")]
public class YarnDislogueVars
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//变量id
    public string VarName { get; set; }//yarn变量名
    public string VarValue { get; set; }//变量值
    public string RelatedSystem { get; set; }//关联系统

    
}
