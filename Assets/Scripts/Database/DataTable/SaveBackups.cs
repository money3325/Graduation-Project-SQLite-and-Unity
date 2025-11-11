using SQLite4Unity3d;

public class SaveBackups
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//备份id
    public string SaveDate { get; set; }//备份日期
    public string SaveTime { get; set; }//备份时间
    public string CurrentSeason { get; set; }//备份季节
    public int CurrentDay { get; set; }//备份天数
    public string BackupNote { get; set; }//备份备注，如自动，手动
    public int IsValid { get; set; }//备份是否有效
    
}