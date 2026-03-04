using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MainUI : BaseUI
{
    enum Buttons
    {
        InventoryButton,
        CatLibraryButton,
        GoldButton,
        DiamondButton,
        GachaButton,
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

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var result = BackendManager.Instance.DoCharacterGacha(5);
            if (result.IsSuccess)
            {
                GachaPopupUI gachaPopupUI = UIManager.Instance.ShowPopupUI<GachaPopupUI>();
                gachaPopupUI.Initialize(result.Data);
            }
            else
            {
                Debug.Log(result.Message);
            }
        }
    }
#endif


    private void Start()
    {
        AddButtonListener(Buttons.InventoryButton, OpenInventoryPopup);
        AddButtonListener(Buttons.CatLibraryButton, () =>
        {
            UIManager.Instance.ShowPopupUI<CatLibraryPopupUI>();
        });
        AddButtonListener(Buttons.GoldButton, () =>
        {
            BackendManager.Instance.UpdateCurrencyTable(CurrencyType.Gold, 1, true);
        });
        AddButtonListener(Buttons.DiamondButton, () =>
        {
            BackendManager.Instance.UpdateCurrencyTable(CurrencyType.Diamond, 1, true);
        });

        AddButtonListener(Buttons.GachaButton, () =>
        {
            var result = BackendManager.Instance.DoCharacterGacha(5);
            if (result.IsSuccess)
            {
                GachaPopupUI gachaPopupUI = UIManager.Instance.ShowPopupUI<GachaPopupUI>();
                gachaPopupUI.Initialize(result.Data);
            }
            else
            {
                Debug.Log(result.Message);
            }
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
