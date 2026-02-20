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
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // base.OnDestroy() 에서 모든 버튼 리스너를 제거
    }

    private void Start()
    {
        Util.DestroyChilds(GetObject(Objects.InventoryItemList));

        foreach (var item in GameDataManager.Instance.InventoryTable.MaterialItems)
        {
            var inventoryItemHolder = UIManager.Instance.MakeItemHolder<InventoryItemHolder>(GetObject(Objects.InventoryItemList).transform);
            inventoryItemHolder.SetItem(item);
        }

        int materialItemCount = GameDataManager.Instance.InventoryTable.MaterialItems.Count;
        int maxMaterialItemCount = GameDataManager.Instance.InventoryTable.MaxMaterialItemCount;


        for (int i = 0; i < maxMaterialItemCount - materialItemCount; i++)
        {
            var inventoryItemHolder = UIManager.Instance.MakeItemHolder<InventoryItemHolder>(GetObject(Objects.InventoryItemList).transform);
        }
    }

    private void ClosePopup()
    {
        ClosePopupWithAnimation(this, null);
    }
}
