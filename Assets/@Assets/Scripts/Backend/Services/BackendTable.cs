using System;
using System.Collections.Generic;
using UnityEngine;

public class BackendTable
{
    private readonly Dictionary<Type, object> _controllers = new();
    private readonly InventoryCtrl _inventoryCtrl = new();
    private readonly CurrencyCtrl _currencyCtrl = new();
    private readonly GachaCtrl _gachaCtrl = new();
    public BackendTable()
    {
        _controllers.Add(typeof(InventoryCtrl), _inventoryCtrl);
        _controllers.Add(typeof(CurrencyCtrl), _currencyCtrl);
        _controllers.Add(typeof(GachaCtrl), _gachaCtrl);
    }

    public TCtrl Get<TCtrl>() where TCtrl : class
    {
        if (!_controllers.TryGetValue(typeof(TCtrl), out var controller))
        {
            throw new Exception($"Controller of type {typeof(TCtrl)} not found");
        }
        return controller as TCtrl;
    }


}
