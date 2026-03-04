using System;


[Serializable]
public static class BConst
{
  /// <summary>Resources 내 차트 CSV 경로 접두사</summary>
  public static class Chart
  {
    public const string PathPrefix = "Charts/";
    public const string Character = "CharacterChart";
    public const string CharacterGachaProbability = "CharacterGachaProbability";
  }

  /// <summary>CSV/차트 컬럼명 (파싱 시 사용)</summary>
  public static class ChartColumn
  {
    public const string CharacterName = "Name";
    public const string CharacterGrade = "Grade";
    public const string GachaProbabilityId = "name";
    public const string GachaProbabilityPercent = "percent";
  }

  public static class Table
  {
    public const string CharacterTable = "CharacterTable";
    public const string InventoryTable = "InventoryTable";
    public const string CurrencyTable = "CurrencyTable";
  }

  public static class PublicData
  {
    public const int AnHour = 3600;
    public const int ADay = 86400;
    public const int AWeek = 604800;
    public const int AMonth = 2592000; // 30일
  }


  public static class ErrorCode
  {
    public const int Ok = 200;

    // 4xx Client errors
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int PaymentRequired = 402;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;

    // 5xx Server errors
    public const int InternalServerError = 500;
  }
}