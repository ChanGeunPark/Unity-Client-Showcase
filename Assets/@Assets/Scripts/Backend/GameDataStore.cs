using System.Collections.Generic;

/// <summary>
/// 게임 데이터 보관소. 테이블(Inventory 등)만 보유하며 로직은 갖지 않음.
/// </summary>
public class GameDataStore
{
    public InventoryTable Inventory { get; set; }

}
