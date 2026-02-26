using System;
using System.Collections.Generic;

using LitJson;

using UnityEngine;

public partial class BackendChart
{
    // 캐릭터 차트
    public BackendResponse<List<CharacterChart>> LoadCharacterChart(JsonData json)
    {
        BackendResponse<List<CharacterChart>> result = new();
        List<CharacterChart> characterChartList = new();

        try
        {
            for (int i = 0; i < json.Count; i++)
            {
                CharacterChart item = new(json[i]);
                characterChartList.Add(item);
            }

            result.IsSuccess = true;
            result.StatusCode = 200;
            result.Data = characterChartList;
        }
        catch (Exception e)
        {
            result.IsSuccess = false;
            result.MessageRaw = e.Message;
            result.StatusCode = 400;
            result.Message = "Parsing Error";
        }

        return result;
    }

    // 캐릭터 가챠 확률 차트
    public BackendResponse<Dictionary<string, float>> LoadCharacterGachaProbability(JsonData json)
    {
        BackendResponse<Dictionary<string, float>> result = new();
        var dict = new Dictionary<string, float>();

        try
        {
            for (int i = 0; i < json.Count; i++)
            {
                JsonData row = json[i];
                string characterId = row["name"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(characterId)) continue;

                string probStr = row["percent"]?.ToString();
                if (string.IsNullOrEmpty(probStr)) continue;
                if (!float.TryParse(probStr, out float percent)) continue;

                dict.Add(characterId, percent);
            }

            result.IsSuccess = true;
            result.StatusCode = 200;
            result.Data = dict;
        }
        catch (Exception e)
        {
            result.IsSuccess = false;
            result.MessageRaw = e.Message;
            result.StatusCode = 400;
            result.Message = "Parsing Error";
        }

        return result;
    }

}
