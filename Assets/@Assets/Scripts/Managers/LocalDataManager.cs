using UnityEngine;

using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using System;
using System.IO;

public class LocalDataManager : MonoBehaviour
{

    public static LocalDataManager Instance { get; private set; }

    public string PersistentDataPath { get; set; }
    public string PersistentDataPathParent { get; set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PersistentDataPath = Path.Combine(Application.persistentDataPath, "GameData");
        PersistentDataPathParent = Path.GetDirectoryName(PersistentDataPath);
        Directory.CreateDirectory(PersistentDataPath);
    }




    private static readonly MessagePackSerializerOptions _msgPackOptions = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            GeneratedMessagePackResolver.Instance,
            StandardResolver.Instance,
            UnityResolver.Instance
        ));


    public void SaveDataMsgPack<T>(T data, string fileName)
    {
        string actualFileName = $"{fileName}.mpk";

        try
        {
            // 1.MassagePack을 사용하여 객체를 byte배열로 직렬화
            byte[] serializedData = MessagePackSerializer.Serialize(data, _msgPackOptions);

            // 2. MassagePack 파일을 구분하기 위해 확장자 추가
            string filePath = Path.Combine(PersistentDataPath, actualFileName);

            // 3. byte배열을 파일에 저장
            File.WriteAllBytes(filePath, serializedData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
            throw;
        }
    }


    public LoadDataResult<T> LoadDataMsgPack<T>(string fileName)
    {
        string actualFileName = $"{fileName}.mpk";
        string filePath = Path.Combine(PersistentDataPath, actualFileName);

        var result = new LoadDataResult<T> { IsSuccess = false };

        if (!File.Exists(filePath))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[LocalDataManager] File not found: {actualFileName}");
#endif
            return result;
        }

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // MassagePack을 사용하여 byte배열을 객체로 역직렬화
            T deserializedData = MessagePackSerializer.Deserialize<T>(fileData, _msgPackOptions);
            result.Data = deserializedData;
            result.IsSuccess = true;
        }
        catch (Exception e)
        {
            result.IsSuccess = false;
            result.ErrorMessage = e.Message;
            Debug.LogError($"Failed to load data: {e.Message}");
        }

        return result;
    }


}


public class LoadDataResult<T>
{
    public T Data { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
}