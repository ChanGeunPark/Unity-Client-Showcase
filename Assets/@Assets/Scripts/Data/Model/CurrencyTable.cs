using System;
using MessagePack;
using UnityEngine;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class CurrencyTable
{
    public int Gold;
    public int Diamond;
    public int Energy;


    public static CurrencyTable CreateDefault()
    {
        return new CurrencyTable
        {
            Gold = 0,
            Diamond = 0,
            Energy = 0
        };
    }
}
