using System;
using System.Collections.Generic;
using System.Linq;
using LitJson;
using UnityEngine;

public class CharacterChart
{
    public string CharacterId;
    public CharacterGrade Grade;

    public CharacterChart() { }

    public CharacterChart(JsonData json)
    {
        try
        {
            CharacterId = CustomJsonUtil.GetString(json, BConst.ChartColumn.CharacterName);
            Grade = CustomJsonUtil.GetEnum(json, BConst.ChartColumn.CharacterGrade, CharacterGrade.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"CharacterChart Error: {e.Message}");
        }
    }
}
