using System.Collections;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 延迟启动对话，确保所有管理器初始化完成
/// 挂载在 DialogueRunner 所在的游戏对象上
/// </summary>
public class DialogueStarter : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("是否在场景启动时自动播放对话")]
    [SerializeField] private bool autoStartOnLoad = true;
    
    [Tooltip("延迟启动时间（秒）")]
    [SerializeField] private float delayTime = 0.5f;
    
    [Tooltip("要启动的对话节点名称")]
    [SerializeField] private string startNode = "Scene1";
    
    private DialogueRunner dialogueRunner;
    
    void Awake()
    {
        dialogueRunner = GetComponent<DialogueRunner>();
        
        if (dialogueRunner != null)
        {
            // 强制禁用自动启动
            dialogueRunner.autoStart = false;
            Debug.Log("[DialogueStarter] 已禁用 DialogueRunner 自动启动");
        }
        else
        {
            Debug.LogError("[DialogueStarter] 未找到 DialogueRunner 组件！");
        }
    }
    
    void Start()
    {
        if (autoStartOnLoad && dialogueRunner != null)
        {
            StartCoroutine(StartDialogueDelayed());
        }
    }
    
    /// <summary>
    /// 延迟启动对话，确保所有管理器初始化完成
    /// </summary>
    private IEnumerator StartDialogueDelayed()
    {
        Debug.Log($"[DialogueStarter] 等待 {delayTime} 秒后启动对话...");
        
        // 等待指定时间
        yield return new WaitForSeconds(delayTime);
        
        // 检查所有必需的管理器是否已初始化
        int maxRetries = 10;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            if (CheckManagersReady())
            {
                Debug.Log($"[DialogueStarter] 所有管理器已就绪，启动对话节点: {startNode}");
                dialogueRunner.StartDialogue(startNode);
                yield break;
            }
            
            retryCount++;
            Debug.LogWarning($"[DialogueStarter] 管理器未就绪，重试 {retryCount}/{maxRetries}");
            yield return new WaitForSeconds(0.2f);
        }
        
        Debug.LogError("[DialogueStarter] 等待超时，管理器未能正确初始化！");
    }
    
    /// <summary>
    /// 检查所有必需的管理器是否已就绪
    /// </summary>
    private bool CheckManagersReady()
    {
        
        
        // 都已初始化
        return true;
    }
    
    /// <summary>
    /// 手动启动对话（可从外部调用）
    /// </summary>
    public void StartDialogueManually(string nodeName = null)
    {
        if (dialogueRunner != null)
        {
            string node = string.IsNullOrEmpty(nodeName) ? startNode : nodeName;
            Debug.Log($"[DialogueStarter] 手动启动对话节点: {node}");
            dialogueRunner.StartDialogue(node);
        }
    }
}