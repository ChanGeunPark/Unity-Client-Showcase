using UnityEngine;

public class CurrencyCtrl
{
    public BackendResponse<CurrencyTable> GetCurrencyTable()
    {
        LoadDataResult<CurrencyTable> localDataResult = LocalDataManager.Instance.LoadDataMsgPack<CurrencyTable>("CurrencyTable");
        if (localDataResult.IsSuccess)
            return new BackendResponse<CurrencyTable>(true, 200, null, null, localDataResult.Data);

        // 최초 실행 등 파일 없음 → 기본 데이터 생성 후 저장
        CurrencyTable defaultTable = CurrencyTable.CreateDefault();
        LocalDataManager.Instance.SaveDataMsgPack(defaultTable, "CurrencyTable");
        return new BackendResponse<CurrencyTable>(true, 200, null, null, defaultTable);
    }

    public BackendResponse<CurrencyTable> UpdateCurrencyTable(CurrencyType type, int amount, bool isAdd)
    {
        BackendResponse<CurrencyTable> response = GetCurrencyTable();
        if (!response.IsSuccess)
            return new BackendResponse<CurrencyTable>(false, null, response.Message);

        switch (type)
        {
            case CurrencyType.Gold:
                response.Data.Gold = response.Data.Gold + amount;
                break;
            case CurrencyType.Diamond:
                response.Data.Diamond = response.Data.Diamond + amount;
                break;
            case CurrencyType.Energy:
                response.Data.Energy = response.Data.Energy + amount;
                break;
        }

        LocalDataManager.Instance.SaveDataMsgPack(response.Data, "CurrencyTable");
        GameDataManager.Instance.Store.CurrencyTable = response.Data;

        return new BackendResponse<CurrencyTable>(true, 200, null, null, response.Data);
    }
}
