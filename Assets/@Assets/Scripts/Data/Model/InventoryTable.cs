using System;
using System.Collections.Generic;

[Serializable]
public class InventoryTable
{
    public List<InventoryItem> MaterialItems = new List<InventoryItem>();
    public List<InventoryItem> EtcItems = new List<InventoryItem>();
    public int MaxMaterialItemCount;


    public static InventoryTable CreateDefault()
    {
        return new InventoryTable
        {
            MaterialItems = new List<InventoryItem>(),
            EtcItems = new List<InventoryItem>(),
            MaxMaterialItemCount = 10
        };
    }
}


[Serializable]
public class InventoryItem
{
    public string ItemId;
    public int Count; // 보유 수량
    public ItemType Type;
}
