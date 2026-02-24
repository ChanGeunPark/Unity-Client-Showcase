using System.Collections.Generic;
using UnityEngine;

public class InventoryCtrl
{
    public BackendResponse<InventoryTable> GetInventoryTable()
    {
        LoadDataResult<InventoryTable> localDataResult = LocalDataManager.Instance.LoadDataMsgPack<InventoryTable>("InventoryTable");
        if (localDataResult.IsSuccess)
            return new BackendResponse<InventoryTable>(true, 200, null, null, localDataResult.Data);

        // 최초 실행 등 파일 없음 → 기본 데이터 생성 후 저장
        InventoryTable defaultTable = InventoryTable.CreateDefault();
        LocalDataManager.Instance.SaveDataMsgPack(defaultTable, "InventoryTable");
        return new BackendResponse<InventoryTable>(true, 200, null, null, defaultTable);
    }


    // 인벤토리 업데이트
    public BackendResponse<InventoryTable> UpdateInventoryTable(string itemId, int count, ItemType type, bool isAdd)
    {
        BackendResponse<InventoryTable> response = GetInventoryTable();
        if (!response.IsSuccess)
            return new BackendResponse<InventoryTable>(false, null, response.Message);

        List<InventoryItem> list = GetItemList(response.Data, type);

        if (isAdd)
            AddOrUpdateItem(list, itemId, count, type);
        else if (!TrySubtractItem(list, itemId, count))
            return new BackendResponse<InventoryTable>(false, null, "Not enough items");

        LocalDataManager.Instance.SaveDataMsgPack(response.Data, "InventoryTable");
        GameDataManager.Instance.Store.InventoryTable = response.Data;
        return new BackendResponse<InventoryTable>(true, 200, null, null, response.Data);
    }

    private static List<InventoryItem> GetItemList(InventoryTable table, ItemType type)
    {
        return type == ItemType.Material ? table.MaterialItems : table.EtcItems;
    }

    private static void AddOrUpdateItem(List<InventoryItem> list, string itemId, int count, ItemType type)
    {
        InventoryItem item = list.Find(x => x.ItemId == itemId);
        if (item != null)
            item.Count += count;
        else
            list.Add(new InventoryItem { ItemId = itemId, Count = count, Type = type });
    }

    private static bool TrySubtractItem(List<InventoryItem> list, string itemId, int count)
    {
        InventoryItem item = list.Find(x => x.ItemId == itemId);
        if (item == null || item.Count < count)
            return false;
        item.Count -= count;
        return true;
    }

}
