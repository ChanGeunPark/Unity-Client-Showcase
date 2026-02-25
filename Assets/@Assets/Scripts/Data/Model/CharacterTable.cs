using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;


[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class CharacterTable
{
    public List<CharacterData> Characters = new List<CharacterData>();

    public static CharacterTable CreateDefault()
    {
        return new CharacterTable
        {
            Characters = new List<CharacterData>()
        };
    }

    public void Initialize()
    {
        Characters = new List<CharacterData>();
    }
}
