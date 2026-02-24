
using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameDataHandler
{
    private static Dictionary<GameDataObject, object> _dataMatchers;

    public static void InitializeMatchers()
    {
        _dataMatchers = new Dictionary<GameDataObject, object>
        {
            {
                GameDataObject.InventoryTable,
                new GameDataAccessor<InventoryTable>(() => GameDataManager.Instance.Store.InventoryTable, value => GameDataManager.Instance.Store.InventoryTable = value)
            },
            {
                GameDataObject.CurrencyTable,
                new GameDataAccessor<CurrencyTable>(() => GameDataManager.Instance.Store.CurrencyTable, value => GameDataManager.Instance.Store.CurrencyTable = value)
            }
        };
    }


    public static void EnsureInitialized()
    {
        try
        {
            if (_dataMatchers == null) InitializeMatchers();
        }
        catch (Exception e)
        {
            Debug.LogError("데이터 매쳐가 이상함  " + e.Message);
        }
    }
}


public interface IGameDataAccessor
{
    void SetValue(object value);
}

public class GameDataAccessor<T> : IGameDataAccessor
{
    private Func<T> _getter;
    private Action<T> _setter;

    public GameDataAccessor(Func<T> getter, Action<T> setter)
    {
        _getter = getter;
        _setter = setter;
    }

    public T Getter() => _getter();

    public void Setter(T value) => _setter(value);

    public void SetValue(object value)
    {
        if (value is T typedValue)
        {
            Setter(typedValue);
        }
        else
        {
            Debug.LogError($"Invalid type for setting value. Expected {typeof(T)}, but got {value.GetType()}.");
        }
    }
}



