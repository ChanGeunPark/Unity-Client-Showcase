using System;
using System.Collections.Generic;
using UnityEngine;

public class GachaCtrl
{

    public BackendResponse<CharacterTable> GetCharacterTable()
    {
        LoadDataResult<CharacterTable> localDataResult = LocalDataManager.Instance.LoadDataMsgPack<CharacterTable>("CharacterTable");
        if (localDataResult.IsSuccess)
            return new BackendResponse<CharacterTable>(true, 200, null, null, localDataResult.Data);

        CharacterTable characterTable = CharacterTable.CreateDefault();
        LocalDataManager.Instance.SaveDataMsgPack(characterTable, "CharacterTable");
        return new BackendResponse<CharacterTable>(true, 200, null, null, characterTable);
    }


    public BackendResponse<List<CharacterChart>> DoCharacterGacha(int gachaCount)
    {
        try
        {
            var result = new List<CharacterChart>();
            var gachaProbabilityData = GameDataManager.Instance.Store.GachaProbabilityData;
            var characterChart = GameDataManager.Instance.Store.CharacterChart;
            var characterTable = GetCharacterTable();

            if (!characterTable.IsSuccess || characterTable.Data == null)
                return new BackendResponse<List<CharacterChart>>(false, null, "Character table not found");
            if (gachaProbabilityData == null || gachaProbabilityData.Count == 0)
                return new BackendResponse<List<CharacterChart>>(false, null, "Gacha probability data not found");
            if (characterChart == null || characterChart.Count == 0)
                return new BackendResponse<List<CharacterChart>>(false, null, "Character chart not found");

            // 가중치 합계
            float totalWeight = 0f;
            foreach (float percent in gachaProbabilityData.Values)
                totalWeight += percent;

            if (totalWeight <= 0f)
                return new BackendResponse<List<CharacterChart>>(false, null, "Invalid gacha probability total");

            var characterChartById = new Dictionary<string, CharacterChart>(characterChart.Count);
            foreach (var c in characterChart)
                characterChartById[c.CharacterId] = c;

            // gachaCount번 확률에 따라 뽑기
            for (int i = 0; i < gachaCount; i++)
            {
                float roll = UnityEngine.Random.Range(0f, totalWeight);
                foreach (var kv in gachaProbabilityData)
                {
                    roll -= kv.Value;
                    if (roll <= 0f)
                    {
                        if (characterChartById.TryGetValue(kv.Key, out var chart))
                            result.Add(chart);
                        break;
                    }
                }
            }

            // 뽑은 결과를 CharacterTable에 추가
            CharacterTable table = characterTable.Data;
            foreach (var chart in result)
            {
                table.Characters.Add(new CharacterData
                {
                    CharacterId = chart.CharacterId,
                    Grade = chart.Grade,
                    Level = 1
                });
            }
            LocalDataManager.Instance.SaveDataMsgPack(table, "CharacterTable");
            GameDataManager.Instance.Store.CharacterTable = table;

            return new BackendResponse<List<CharacterChart>>(true, data: result);
        }
        catch (Exception ex)
        {
            Debug.LogError("DoCharacterGacha Error: " + ex.Message);
            return new BackendResponse<List<CharacterChart>>(false, null, ex.Message);
        }
    }
}
