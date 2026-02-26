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
            CharacterId = CustomJsonUtil.GetString(json, "Name");
            Grade = CustomJsonUtil.GetEnum(json, "Grade", CharacterGrade.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"CharacterChart Error: {e.Message}");
        }
    }
}
