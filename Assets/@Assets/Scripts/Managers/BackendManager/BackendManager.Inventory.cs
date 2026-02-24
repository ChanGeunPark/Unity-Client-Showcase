using UnityEngine;

public partial class BackendManager : MonoBehaviour
{
    public BackendResponse<InventoryTable> GetInventoryTable()
    {
        return _backendTable.Get<InventoryCtrl>().GetInventoryTable();
    }

    public BackendResponse<InventoryTable> UpdateInventoryTable(string itemId, int count, ItemType type, bool isAdd)
    {
        return _backendTable.Get<InventoryCtrl>().UpdateInventoryTable(itemId, count, type, isAdd);
    }
}
