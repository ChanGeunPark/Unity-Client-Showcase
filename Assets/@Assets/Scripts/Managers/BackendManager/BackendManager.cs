using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public partial class BackendManager : MonoBehaviour
{
    public static BackendManager Instance { get; private set; }
    private BackendTable _backendTable;
    private BackendChart _backendChart;
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

    private void Start()
    {
        InitGameData();
    }

    private void InitializeBackendComponents()
    {
        _backendTable = new BackendTable();
        _backendChart = new BackendChart();
    }


    public void InitGameData()
    {
        // 게임 데이터 초기화
        GameDataManager.Instance.Store.InitializeGameData().Forget();
        // 게임 데이터 핸들러 초기화
        GameDataHandler.EnsureInitialized();
    }

    public BackendResponse<JsonData> GetChartFromLocal(string chartName)
    {
        return _backendChart.GetChartFromLocal(chartName);
    }

    // 차트 내용 반환
    public BackendResponse<object> GetChartContents(string chartName)
    {
        BackendResponse<JsonData> localData = GetChartFromLocal(chartName);
        if (localData == null || !localData.IsSuccess || localData.Data == null) return null;

        switch (chartName)
        {
            case "CharacterChart":
                BackendResponse<List<CharacterChart>> characterChartRes = _backendChart.LoadCharacterChart(localData.Data);
                return new BackendResponse<object>
                {
                    Data = characterChartRes.Data,
                    Message = characterChartRes.Message,
                    IsSuccess = characterChartRes.IsSuccess,
                    StatusCode = characterChartRes.StatusCode,
                    MessageRaw = characterChartRes.MessageRaw
                };
            case "CharacterGachaProbability":
                BackendResponse<Dictionary<string, float>> gachaProbRes = _backendChart.LoadCharacterGachaProbability(localData.Data);
                return new BackendResponse<object>
                {
                    Data = gachaProbRes.Data,
                    Message = gachaProbRes.Message,
                    IsSuccess = gachaProbRes.IsSuccess,
                    StatusCode = gachaProbRes.StatusCode,
                    MessageRaw = gachaProbRes.MessageRaw
                };
        }

        return null;
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
