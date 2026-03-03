using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 데이터 보관소. 테이블(Inventory 등)만 보유하며 로직은 갖지 않음.
/// </summary>
public class GameDataStore
{


    ///////////////////////////////////////// [Chart] /////////////////////////////////////////
    private List<CharacterChart> _characterChart;
    public List<CharacterChart> CharacterChart
    {
        get => _characterChart;
        set { if (_characterChart == null) _characterChart = value; }
    }


    private Dictionary<string, float> _gachaProbabilityData;
    public Dictionary<string, float> GachaProbabilityData
    {
        get => _gachaProbabilityData;
        set { if (_gachaProbabilityData == null) _gachaProbabilityData = value; }
    }



    ///////////////////////////////////////// [Table] /////////////////////////////////////////
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

    private CharacterTable _characterTable;
    public CharacterTable CharacterTable
    {
        get
        {
            if (_characterTable == null)
                _characterTable = BackendManager.Instance.GetCharacterTable()?.Data;
            return _characterTable;
        }
        set
        {
            if (_characterTable == value) return;
            _characterTable = value;
            GameDataManager.Instance.Notify(GameDataEventKind.CharacterTableChanged, _characterTable);
        }
    }


    private bool _isInitialized = false;

    public UniTask InitializeGameData()
    {
        if (_isInitialized)
        {
            ResetGameData();
        }

        BackendResponse<object> characterChartRes = BackendManager.Instance.GetChartContents("CharacterChart");
        if (characterChartRes != null && characterChartRes.IsSuccess && characterChartRes.Data != null)
        {
            CharacterChart = characterChartRes.Data as List<CharacterChart>;
        }

        BackendResponse<object> gachaProbRes = BackendManager.Instance.GetChartContents("CharacterGachaProbability");
        if (gachaProbRes != null && gachaProbRes.IsSuccess && gachaProbRes.Data != null)
        {
            GachaProbabilityData = gachaProbRes.Data as Dictionary<string, float>;
        }

        _isInitialized = true;
        return UniTask.CompletedTask;
    }


    private void ResetGameData()
    {
        _isInitialized = false;
        _characterChart = null;
        _gachaProbabilityData = null;
        _characterTable = null;
        InventoryTable = null;
    }

}
