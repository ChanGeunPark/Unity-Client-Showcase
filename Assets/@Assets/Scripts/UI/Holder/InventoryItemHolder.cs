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
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        gameObject.GetComponent<ImageController>().SetImageState(ImageController.ImageState.Disabled);
        HideText(Texts.ItemCount);
        HideImage(Images.ItemIcon);
    }

    public void SetItem(InventoryItem item)
    {
        gameObject.GetComponent<ImageController>().SetImageState(ImageController.ImageState.Normal);
        ShowText(Texts.ItemCount);
        ShowImage(Images.ItemIcon);

        var itemData = ResourceManager.Instance.LoadSOData<ItemDataSO>(item.ItemId);
        SetImage(Images.ItemIcon, itemData.Icon);
        SetText(Texts.ItemCount, item.Count.ToString());
    }
}
