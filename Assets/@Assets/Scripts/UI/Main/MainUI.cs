using UnityEngine;

public class MainUI : BaseUI
{
    enum Buttons
    {
        InventoryButton,
        AddBoltButton,
        GoldButton,
        DiamondButton,
    }

    enum Texts
    {
        GoldCount,
        DiamondCount,
    }

    private void Awake()
    {
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
    }

    private void Start()
    {
        AddButtonListener(Buttons.InventoryButton, OpenInventoryPopup);
        AddButtonListener(Buttons.AddBoltButton, () =>
        {
            BackendManager.Instance.UpdateInventoryTable("bolt", 1, ItemType.Material, true);
        });
        AddButtonListener(Buttons.GoldButton, () =>
        {
            BackendManager.Instance.UpdateCurrencyTable(CurrencyType.Gold, 1, true);
        });
        AddButtonListener(Buttons.DiamondButton, () =>
        {
            BackendManager.Instance.UpdateCurrencyTable(CurrencyType.Diamond, 1, true);
        });


        GameDataManager.Instance.EventBus.OnCurrencyTableChanged += OnCurrencyTableChanged;
        Initialize();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameDataManager.Instance.EventBus.OnCurrencyTableChanged -= OnCurrencyTableChanged;
    }

    private void Initialize()
    {
        RefreshCurrencyTable();
    }
    private void OpenInventoryPopup()
    {
        UIManager.Instance.ShowPopupUI<InventoryPopupUI>();
    }

    private void RefreshCurrencyTable()
    {
        GetText(Texts.GoldCount).text = GameDataManager.Instance.Store.CurrencyTable.Gold.ToString();
        GetText(Texts.DiamondCount).text = GameDataManager.Instance.Store.CurrencyTable.Diamond.ToString();
    }

    private void OnCurrencyTableChanged(GameDataEvent evt)
    {
        RefreshCurrencyTable();
    }
}
