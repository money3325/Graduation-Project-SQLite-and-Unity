using SQLite4Unity3d;

public class PlayerTasks
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }//任务id
    public string TaskName { get; set; }//任务名字
    public string TaskType { get; set; }//任务类型
    public string TaskStatus { get; set; }//任务状态
    public string TaskDesc { get; set; }//任务描述
    public string TargetRequire { get; set; }//目标要求
    public string CurrentProgress { get; set; }//当前进度
    public string RewardItems { get; set; }//奖励
    public string YarnDialogueId { get; set; }//关联yarnspinner
}

