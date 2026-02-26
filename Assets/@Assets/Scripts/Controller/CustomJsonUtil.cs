using System;
using System.Collections.Generic;

using LitJson;

using UnityEngine;


/// <summary>
/// Null Exception을 방지하기 위한 CustomJsonUtil
/// 해당 유틸을 사용하면 Null Exception이 발생할 경우 기본값을 반환
/// </summary>
public static class CustomJsonUtil
{
    public static string GetString(JsonData jsonData, string key, string defaultValue = null)
    {
        if (
            !jsonData.Keys.Contains(key) ||
            jsonData[key].ToString().ToLower() == "true" ||
            jsonData[key] == null
        )
        {
            return defaultValue;
        }

        return jsonData[key].ToString();
    }

    public static int GetInt(JsonData jsonData, string key, int defaultValue = 0)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            string value = jsonData[key].ToString();
            if (int.TryParse(value, out int result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    public static long GetLong(JsonData jsonData, string key, long defaultValue = 0)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            string value = jsonData[key].ToString();
            if (long.TryParse(value, out long result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    public static bool GetBool(JsonData jsonData, string key, bool defaultValue = false)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            string value = jsonData[key].ToString();
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    public static float GetFloat(JsonData jsonData, string key, float defaultValue = 0f)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            string value = jsonData[key].ToString();
            if (float.TryParse(value, out float result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    public static decimal GetDecimal(JsonData jsonData, string key, decimal defaultValue = 0m)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            string value = jsonData[key].ToString();
            if (decimal.TryParse(value, out decimal result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// JSON 배열을 List<T>로 안전하게 파싱합니다. 키가 없거나 null인 경우 빈 리스트를 반환합니다.
    /// </summary>
    /// <typeparam name="T">리스트 항목 타입</typeparam>
    /// <param name="jsonData">JSON 데이터</param>
    /// <param name="key">JSON 키</param>
    /// <param name="converter">JSON 항목을 T 타입으로 변환하는 함수</param>
    /// <returns>파싱된 리스트 또는 빈 리스트</returns>
    public static List<T> ParseJsonList<T>(JsonData jsonData, string key, Func<JsonData, T> converter)
    {
        var result = new List<T>();

        try
        {
            if (
                jsonData != null &&
                jsonData.Keys.Contains(key) &&
                jsonData[key] != null &&
                jsonData[key].IsArray
            )
            {
                for (int i = 0; i < jsonData[key].Count; i++)
                {
                    result.Add(converter(jsonData[key][i]));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON list for key '{key}': {e.Message}");
        }

        return result;
    }

    /// <summary>
    /// JSON 객체를 Dictionary<TKey, TValue>로 안전하게 파싱합니다. 키가 없거나 null인 경우 빈 사전을 반환합니다.
    /// </summary>
    /// <typeparam name="TKey">사전 키 타입</typeparam>
    /// <typeparam name="TValue">사전 값 타입</typeparam>
    /// <param name="jsonData">JSON 데이터</param>
    /// <param name="key">JSON 키</param>
    /// <param name="keyConverter">JSON 키를 TKey 타입으로 변환하는 함수</param>
    /// <param name="valueConverter">JSON 값을 TValue 타입으로 변환하는 함수</param>
    /// <returns>파싱된 사전 또는 빈 사전</returns>
    public static Dictionary<TKey, TValue> ParseJsonDictionary<TKey, TValue>(
        JsonData jsonData,
        string key,
        Func<string, TKey> keyConverter,
        Func<JsonData, TValue> valueConverter)
    {
        var result = new Dictionary<TKey, TValue>();

        try
        {
            if (
                jsonData != null &&
                jsonData.Keys.Contains(key) &&
                jsonData[key] != null &&
                jsonData[key].IsObject
            )
            {
                foreach (string dictKey in jsonData[key].Keys)
                {
                    TKey convertedKey = keyConverter(dictKey);
                    TValue convertedValue = valueConverter(jsonData[key][dictKey]);
                    result[convertedKey] = convertedValue;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON dictionary for key '{key}': {e.Message}");
        }

        return result;
    }

    public static T GetEnum<T>(JsonData jsonData, string key, T defaultValue) where T : struct
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null)
        {
            if (Enum.TryParse(jsonData[key].ToString(), out T result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    public static Vector3 GetVector3(JsonData jsonData, string key, Vector3 defaultValue = default)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null && jsonData[key].IsObject)
        {
            JsonData vectorJson = jsonData[key];
            float x = GetFloat(vectorJson, "x", defaultValue.x);
            float y = GetFloat(vectorJson, "y", defaultValue.y);
            float z = GetFloat(vectorJson, "z", defaultValue.z);
            return new Vector3(x, y, z);
        }
        return defaultValue;
    }

    public static BoundsInt GetBoundsInt(JsonData jsonData, string key, BoundsInt defaultValue = default)
    {
        if (jsonData.Keys.Contains(key) && jsonData[key] != null && jsonData[key].IsObject)
        {
            JsonData boundsJson = jsonData[key];
            int x = GetInt(boundsJson, "x", defaultValue.position.x);
            int y = GetInt(boundsJson, "y", defaultValue.position.y);
            int z = GetInt(boundsJson, "z", defaultValue.position.z);
            int sizeX = GetInt(boundsJson, "sizeX", defaultValue.size.x);
            int sizeY = GetInt(boundsJson, "sizeY", defaultValue.size.y);
            int sizeZ = GetInt(boundsJson, "sizeZ", defaultValue.size.z);
            return new BoundsInt(new Vector3Int(x, y, z), new Vector3Int(sizeX, sizeY, sizeZ));
        }
        return defaultValue;
    }
}