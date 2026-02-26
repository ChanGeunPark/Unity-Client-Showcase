using System;
using System.Collections.Generic;
using UnityEngine;

public class GachaCtrl
{
    public BackendResponse<List<CharacterChart>> DoCharacterGacha(int gachaCount)
    {
        try
        {
            var result = new List<CharacterChart>();
            var gachaProbabilityData = GameDataManager.Instance.Store.GachaProbabilityData;
            var characterChart = GameDataManager.Instance.Store.CharacterChart;

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

            return new BackendResponse<List<CharacterChart>>(true, data: result);
        }
        catch (Exception ex)
        {
            Debug.LogError("DoCharacterGacha Error: " + ex.Message);
            return new BackendResponse<List<CharacterChart>>(false, null, ex.Message);
        }
    }
}
