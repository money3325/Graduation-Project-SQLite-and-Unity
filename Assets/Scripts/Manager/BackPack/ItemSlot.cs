using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("格子UI引用")]
    public Image itemIcon; // 物品图标（Image组件）
    public Text itemCount; // 物品数量
    public Image highlightBg; // 选中高亮背景
    public Image grayMask; // 禁用灰罩

    // 格子数据
    public string currentItemType { get; private set; }
    public int currentItemCount { get; private set; }
    public bool isSelected { get; private set; }
    public bool isUsable { get; private set; }

    void Awake()
    {
        ClearSlot();
        highlightBg.gameObject.SetActive(false);
        grayMask.gameObject.SetActive(false);
    }

    /// <summary>
    /// 清空格子显示
    /// </summary>
    public void ClearSlot()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false; // 隐藏图标
        itemCount.text = "";
        itemCount.gameObject.SetActive(false);
        grayMask.gameObject.SetActive(false);
        highlightBg.gameObject.SetActive(false);

        currentItemType = null;
        currentItemCount = 0;
        isSelected = false;
        isUsable = false;
    }

    /// <summary>
    /// 设置格子显示（支持Sprite图标+数量叠加）
    /// </summary>
    public void SetSlot(string itemType, int count)
    {
        currentItemType = itemType;
        currentItemCount = count;

        // 1. 获取物品图标并显示
        Sprite itemSprite = ItemIconConfig.Instance.GetItemSprite(itemType);
        if (itemSprite != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.enabled = true; // 启用图标
        }
        else
        {
            itemIcon.enabled = false; // 无图标则隐藏
        }

        // 2. 判断是否可用（工具永久可用，种子/作物数量>0可用）
        isUsable = IsToolItem(itemType) || count > 0;
        grayMask.gameObject.SetActive(!isUsable);

        // 3. 数量显示（工具不显示数量，种子/作物显示叠加数量）
        if (IsToolItem(itemType))
        {
            itemCount.gameObject.SetActive(false);
        }
        else
        {
            itemCount.gameObject.SetActive(true);
            itemCount.text = count.ToString(); // 显示叠加后的数量
        }

        // 4. 重置选中状态
        isSelected = false;
        highlightBg.gameObject.SetActive(false);
    }

    /// <summary>
    /// 判断是否为工具物品
    /// </summary>
    private bool IsToolItem(string itemType)
    {
        return itemType == "WateringCan" || itemType == "Hoe";
    }

    // 以下原有方法（ToggleSelected、ForceCancelSelected、OnPointerClick）保持不变
    public void ToggleSelected()
    {
        if (!isUsable) return;

        isSelected = !isSelected;
        highlightBg.gameObject.SetActive(isSelected);
        BackpackManager.Instance.OnItemSlotToggled(this);
    }

    public void ForceCancelSelected()
    {
        isSelected = false;
        highlightBg.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleSelected();
    }
}
