using System.Collections.Generic;

/// <summary>
/// 게임 데이터 보관소. 테이블(Inventory 등)만 보유하며 로직은 갖지 않음.
/// </summary>
public class GameDataStore
{
    private InventoryTable _inventoryTable;
    public InventoryTable InventoryTable
    {
        get
        {
            if (_inventoryTable == null)
            {
                _inventoryTable = BackendManager.Instance.GetInventoryTable()?.Data;
            }
            return _inventoryTable;
        }
        set
        {
            if (_inventoryTable == value) return;
            InventoryTable prev = _inventoryTable;
            _inventoryTable = value;

            bool materialChanged = !GameDataCompare.ListEquals(prev?.MaterialItems, _inventoryTable?.MaterialItems, (x, y) => x.ItemId == y.ItemId && x.Count == y.Count && x.Type == y.Type);
            bool etcChanged = !GameDataCompare.ListEquals(prev?.EtcItems, _inventoryTable?.EtcItems, (x, y) => x.ItemId == y.ItemId && x.Count == y.Count && x.Type == y.Type);

            if (materialChanged)
                GameDataManager.Instance.Notify(GameDataEventKind.MaterialItemsChanged, _inventoryTable);
            if (etcChanged)
                GameDataManager.Instance.Notify(GameDataEventKind.EtcItemsChanged, _inventoryTable);

            GameDataManager.Instance.Notify(GameDataEventKind.InventoryChanged, _inventoryTable);

        }
    }


    private CurrencyTable _currencyTable;
    public CurrencyTable CurrencyTable
    {
        get
        {
            if (_currencyTable == null)
            {
                _currencyTable = BackendManager.Instance.GetCurrencyTable()?.Data;
            }
            return _currencyTable;
        }
        set
        {
            if (_currencyTable == value) return;
            _currencyTable = value;
            GameDataManager.Instance.Notify(GameDataEventKind.CurrencyTableChanged, _currencyTable);
        }
    }

    private bool _isInitialized = false;

    public async void InitializeGameData()
    {
        if (_isInitialized)
        {
            ResetGameData();
        }

        // TODO: 차트 데이터 넣기
        _isInitialized = true;
    }


    private void ResetGameData()
    {
        _isInitialized = false;
        InventoryTable = null;
    }

}
