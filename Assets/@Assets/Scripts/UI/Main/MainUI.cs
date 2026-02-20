using UnityEngine;

public class MainUI : BaseUI
{
    enum Buttons
    {
        InventoryButton
    }

    private void Awake()
    {
        BindButton(typeof(Buttons));
    }

    private void Start()
    {
        AddButtonListener(Buttons.InventoryButton, OpenInventoryPopup);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void OpenInventoryPopup()
    {
        UIManager.Instance.ShowPopupUI<InventoryPopupUI>();
    }
}
