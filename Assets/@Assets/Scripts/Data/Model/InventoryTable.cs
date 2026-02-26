using System;
using System.Collections.Generic;
using MessagePack;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
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
            MaxMaterialItemCount = 100
        };
    }
}


[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class InventoryItem
{
    public string ItemId;
    public int Count; // 보유 수량
    public ItemType Type;
}
