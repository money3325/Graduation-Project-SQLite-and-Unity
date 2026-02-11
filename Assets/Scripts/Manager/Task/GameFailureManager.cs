using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏失败管理（任务超时直接触发游戏失败）
/// </summary>
public class GameFailureManager : MonoBehaviour
{
    public static GameFailureManager Instance { get; private set; }

    [Header("游戏失败2D面板")]
    public GameObject gameOverPanel;
    public Button restartGameButton; // 重新开始游戏按钮（仅重启游戏，不重置单个任务）

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 绑定重新开始游戏按钮（重启整个游戏，而非单个任务）
        restartGameButton.onClick.AddListener(RestartGame);
        gameOverPanel.SetActive(false); // 初始隐藏
    }

    /// <summary>
    /// 触发游戏失败（任务超时调用）
    /// </summary>
    public void TriggerGameOver(string failedTaskName)
    {
        // 停止游戏核心流程（如时间流逝、作物生长等）
        Time.timeScale = 0; 
        // 显示游戏失败面板
        gameOverPanel.SetActive(true);
    }

    /// <summary>
    /// 重新开始游戏（仅重启，无单个任务重置）
    /// </summary>
    private void RestartGame()
    {
        Time.timeScale = 1;
        gameOverPanel.SetActive(false);
        // 此处可调用场景重启逻辑（如加载初始场景）
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
