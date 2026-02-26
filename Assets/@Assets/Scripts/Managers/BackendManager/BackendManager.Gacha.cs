using System.Collections.Generic;
using UnityEngine;

public partial class BackendManager
{
    public BackendResponse<List<CharacterChart>> DoCharacterGacha(int gachaCount)
    {
        return _backendTable.Get<GachaCtrl>().DoCharacterGacha(gachaCount);
    }
}
