using UnityEngine;

public class InventoryItemHolder : BaseUI
{

    enum Images
    {
        ItemIcon
    }

    enum Texts
    {
        ItemCount
    }

    private void Awake()
    {
        BindImage(typeof(Images));
        BindText(typeof(Texts));

        HideText(Texts.ItemCount);
        HideImage(Images.ItemIcon);
    }

    public void SetItem(InventoryItem item)
    {
        ShowText(Texts.ItemCount);
        ShowImage(Images.ItemIcon);

        var itemData = ResourceManager.Instance.LoadSOData<ItemDataSO>(item.ItemId);
        SetImage(Images.ItemIcon, itemData.Icon);
        SetText(Texts.ItemCount, item.Count.ToString());
    }
}
