using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryCtrl
{
    public BackendResponse<InventoryTable> GetInventoryTable()
    {
        LoadDataResult<InventoryTable> localDataResult = LocalDataManager.Instance.LoadDataMsgPack<InventoryTable>(BConst.Table.InventoryTable);
        if (localDataResult.IsSuccess)
            return new BackendResponse<InventoryTable>(true, 200, null, null, localDataResult.Data);

        // 최초 실행 등 파일 없음 → 기본 데이터 생성 후 저장
        InventoryTable defaultTable = InventoryTable.CreateDefault();
        LocalDataManager.Instance.SaveDataMsgPack(defaultTable, BConst.Table.InventoryTable);
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

        // Material 추가 시만, 수정된 데이터(response.Data) 기준으로 슬롯 상한 검사
        if (type == ItemType.Material)
        {
            int totalCount = response.Data.MaterialItems.Sum(x => x.Count);
            if (totalCount >= response.Data.MaxMaterialItemCount)
                return new BackendResponse<InventoryTable>(false, null, "Max material item count");
        }

        LocalDataManager.Instance.SaveDataMsgPack(response.Data, BConst.Table.InventoryTable);
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
