using SQLite4Unity3d;

public class PlayerCore 
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//玩家唯一表示
    public string PlayerName { get; set; }//玩家名字
    public string CurrentSeason { get; set; }//当前季节
    public int CurrentDay { get; set; }//当前季节天数
    public string CurrentTime { get; set; }//当前时间段，比如清晨
    public string LastSaveTime { get; set; }//最后的数据保存
   
}
