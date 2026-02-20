using System;
using System.Collections.Generic;

[Serializable]
public class InventoryTable
{
    public List<InventoryItem> FoodItems = new List<InventoryItem>();
    public List<InventoryItem> MaterialItems = new List<InventoryItem>();
    public List<InventoryItem> EtcItems = new List<InventoryItem>();

    public static InventoryTable CreateDefault()
    {
        return new InventoryTable
        {
            FoodItems = new List<InventoryItem>(),
            MaterialItems = new List<InventoryItem>(),
            EtcItems = new List<InventoryItem>()
        };
    }
}


[Serializable]
public class InventoryItem
{
    public string ItemId;
    public int Count; // 보유 수량
    public InventoryItemType Type;
}
