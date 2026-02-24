using UnityEngine;

public partial class BackendManager : MonoBehaviour
{
    public BackendResponse<CurrencyTable> GetCurrencyTable()
    {
        return _backendTable.Get<CurrencyCtrl>().GetCurrencyTable();
    }

    public BackendResponse<CurrencyTable> UpdateCurrencyTable(CurrencyType type, int amount, bool isAdd)
    {
        return _backendTable.Get<CurrencyCtrl>().UpdateCurrencyTable(type, amount, isAdd);
    }
}
