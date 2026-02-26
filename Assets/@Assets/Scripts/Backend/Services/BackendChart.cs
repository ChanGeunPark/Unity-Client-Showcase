using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public partial class BackendChart
{
    // 로컬 CSV 파일에서 차트 내용 반환
    public BackendResponse<JsonData> GetChartFromLocal(string chartName)
    {
        try
        {
            // Resources/Charts 폴더에서 CSV 로드 (확장자 없이 에셋 이름만 사용)
            TextAsset csvFile = Resources.Load<TextAsset>($"Charts/{chartName}");

            if (csvFile == null)
            {
                Debug.LogError($"[BackendChart] CSV file not found: Charts/{chartName} (Resources/Charts/ 폴더에 있어야 하며, Addressable 체크 해제 필요)");
                return null;
            }

            // CSV를 JsonData로 변환
            JsonData jsonData = ConvertCSVToJsonData(csvFile.text);

            if (jsonData == null)
            {
                Debug.LogError($"[BackendChart] Failed to convert CSV to JsonData: {chartName}");
                return null;
            }

            Debug.Log($"[BackendChart] Successfully loaded chart: {chartName}");
            return new BackendResponse<JsonData>(true, 200, null, null, jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BackendChart] Error getting chart from local: {chartName}, Error: {e.Message}");
            return new BackendResponse<JsonData>(false, null, e.Message);
        }
    }


    // CSV 파일 내용을 JsonData로 변환
    private JsonData ConvertCSVToJsonData(string csvContent)
    {
        try
        {
            var lines = csvContent.Split('\n');
            if (lines.Length < 2)
            {
                Debug.LogError("[BackendChart] CSV file has insufficient data");
                return null;
            }

            // 헤더 파싱
            var headers = ParseCSVLine(lines[0].Trim());

            // JsonData 배열 생성
            JsonData jsonArray = new JsonData();
            jsonArray.SetJsonType(JsonType.Array);

            // 데이터 행 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var values = ParseCSVLine(line);
                if (values.Count == 0)
                    continue;

                // 각 행을 JsonData 객체로 변환
                JsonData rowObject = new JsonData();
                rowObject.SetJsonType(JsonType.Object);

                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    string header = headers[j].Trim();
                    string value = values[j].Trim();

                    if (!string.IsNullOrEmpty(header))
                    {
                        rowObject[header] = value;
                    }
                }

                jsonArray.Add(rowObject);
            }

            return jsonArray;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BackendChart] Error converting CSV to JsonData: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }


    // CSV 파일 한 줄을 파싱하여 문자열 리스트로 반환
    private List<string> ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // 연속된 따옴표는 이스케이프된 따옴표
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField += '"';
                    i++; // 다음 따옴표 건너뛰기
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result;
    }



}
