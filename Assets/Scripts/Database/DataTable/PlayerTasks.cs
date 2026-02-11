using SQLite4Unity3d;

[Table("PlayerTasks")]
public class PlayerTasks
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//任务id
    public string TaskName { get; set; }//任务名字
    public string TaskType { get; set; }//任务类型
    public string TaskStatus { get; set; }//任务状态
    public string TaskDesc { get; set; }//任务描述
    public int TargetCount { get; set; }//目标要求
    public int CurrentProgress { get; set; }//当前进度
    public string RewardItems { get; set; }//奖励
    public int DayAssigned { get; set; } // 发布天数（如第3天）
    public int SaveBackupID { get; set; } = -1; // -1=当前有效，其他为备份ID
    public int DayLimit { get; set; } = 7; // 任务允许完成的最大天数（默认7天）
    public string YarnDialogueId { get; set; }//关联yarnspinner
}

