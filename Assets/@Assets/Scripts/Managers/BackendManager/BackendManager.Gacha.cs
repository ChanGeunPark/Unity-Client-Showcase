using System.Collections.Generic;
using UnityEngine;

public partial class BackendManager
{
    public BackendResponse<CharacterTable> GetCharacterTable()
    {
        return _backendTable.Get<GachaCtrl>().GetCharacterTable();
    }

    public BackendResponse<List<CharacterChart>> DoCharacterGacha(int gachaCount)
    {
        return _backendTable.Get<GachaCtrl>().DoCharacterGacha(gachaCount);
    }
}
