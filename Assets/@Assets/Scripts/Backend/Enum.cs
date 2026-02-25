using UnityEngine;

public enum ItemType
{
  None = 0,
  Material,
  Gold,
  Energy,
}

public enum GameDataObject
{
  InventoryTable,
  CurrencyTable
}

public enum CurrencyType
{
  Gold,
  Diamond,
  Energy,
}


// 캐릭터 등급
public enum CharacterGrade
{
  None,
  Common,
  Rare,
  Epic,
  Legendary,
}