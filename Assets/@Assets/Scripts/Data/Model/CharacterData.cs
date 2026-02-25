using System;
using MessagePack;
using UnityEngine;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class CharacterData
{
    public string CharacterId;
    public CharacterGrade Grade;
    public int Level;


    public static CharacterData CreateDefault()
    {
        return new CharacterData
        {
            CharacterId = "",
            Grade = CharacterGrade.None,
            Level = 1
        };
    }

    public void Initialize(string characterId, CharacterGrade grade, int level)
    {
        CharacterId = characterId;
        Grade = grade;
        Level = level;
    }
}
