using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryPopupUI : BasePopupUI
{
    enum Buttons
    {
        BackgroundButton,
        CloseButton
    }

    enum Objects
    {
        InventoryItemList,
        InventoryPopup
    }


    private void Awake()
    {
        BindObject(typeof(Objects));
        BindButton(typeof(Buttons));

        ShowPopupWithAnimation(this, Objects.InventoryPopup);

        AddButtonListener(Buttons.BackgroundButton, ClosePopup);
        AddButtonListener(Buttons.CloseButton, ClosePopup);

        GameDataManager.Instance.EventBus.OnInventoryChanged += OnInventoryChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // base.OnDestroy() 에서 모든 버튼 리스너를 제거
    }

    private void Start()
    {
        RefreshUI();
    }

    private void OnInventoryChanged(GameDataEvent evt)
    {
        if (evt.Kind == GameDataEventKind.MaterialItemsChanged)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        Util.DestroyChilds(GetObject(Objects.InventoryItemList));

        foreach (var item in GameDataManager.Instance.Store.InventoryTable.MaterialItems)
        {
            var inventoryItemHolder = UIManager.Instance.MakeItemHolder<InventoryItemHolder>(GetObject(Objects.InventoryItemList).transform);
            inventoryItemHolder.SetItem(item);
        }

        int materialItemCount = GameDataManager.Instance.Store.InventoryTable.MaterialItems.Count;
        int minSlotCount = 20; // 빈 슬롯 미리 보여주는 용도

        if (materialItemCount < minSlotCount)
        {
            for (int i = 0; i < minSlotCount - materialItemCount; i++)
            {
                var inventoryItemHolder = UIManager.Instance.MakeItemHolder<InventoryItemHolder>(GetObject(Objects.InventoryItemList).transform);
            }
        }
    }

    private void ClosePopup()
    {
        ClosePopupWithAnimation(this, null);
    }
}
