using UnityEngine;
using UnityEngine.UI;

public class SeedPanel : MonoBehaviour
{
    [Header("种子按钮（拖入）")]
    public Button wheatBtn;   // 小麦种子按钮
    public Button cornBtn;    // 玉米种子按钮
    public Button carrotBtn;  // 胡萝卜种子按钮

    [Header("依赖（拖入）")]
    public CropManager cropManager;

    void Awake()
    {
        
        wheatBtn.onClick.RemoveAllListeners();
        // 绑定按钮点击事件（参数和CropConfig里的cropType一致）
        wheatBtn.onClick.AddListener(() => cropManager.SelectSeed("小麦"));
        cornBtn.onClick.AddListener(() => cropManager.SelectSeed("玉米"));
        carrotBtn.onClick.AddListener(() => cropManager.SelectSeed("中药"));
    }
}