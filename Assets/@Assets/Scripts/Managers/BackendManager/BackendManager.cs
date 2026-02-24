using UnityEngine;

[DefaultExecutionOrder(-10000)]
public partial class BackendManager : MonoBehaviour
{
    public static BackendManager Instance { get; private set; }
    private BackendTable _backendTable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeBackendComponents();
    }

    private void InitializeBackendComponents()
    {
        _backendTable = new BackendTable();
    }


    public void InitGameData()
    {
        // 게임 데이터 초기화
        GameDataManager.Instance.Store.InitializeGameData();
        // 게임 데이터 핸들러 초기화
        GameDataHandler.EnsureInitialized();
    }
}

public class BackendResponse<T>
{
    public BackendResponse()
    {
        Data = default;
        Message = null;
        IsSuccess = false;
        StatusCode = null;
        MessageRaw = null;
    }

    public BackendResponse(bool isSuccess, int? statusCode = null, string message = null, string messageRaw = null, T data = default)
    {
        Data = data;
        Message = message;
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        MessageRaw = messageRaw;
    }

    public T Data { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public int? StatusCode { get; set; }
    public string MessageRaw { get; set; }
}
