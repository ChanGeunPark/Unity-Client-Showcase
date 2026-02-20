using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 데이터 진입점. Singleton 생명주기 + Store/EventBus 보유 및 API 노출.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("Lifecycle")]

    private readonly GameDataStore _store = new GameDataStore();
    private readonly GameDataEventBus _eventBus = new GameDataEventBus();

    #region Data (Store 위임)

    /// <summary>
    /// 인벤토리 테이블. Store에 보관되며 여기서 노출합니다.
    /// </summary>
    public InventoryTable InventoryTable
    {
        get => _store.InventoryTable;
        set => _store.InventoryTable = value;
    }

    /// <summary>
    /// 데이터 보관소 참조.
    /// </summary>
    public GameDataStore Store => _store;

    /// <summary>
    /// 이벤트 버스 참조.
    /// </summary>
    public GameDataEventBus EventBus => _eventBus;

    #endregion

    #region Events (EventBus 포워딩)

    public event Action<IReadOnlyList<GameDataEvent>> OnEvents
    {
        add => _eventBus.OnEvents += value;
        remove => _eventBus.OnEvents -= value;
    }

    public event Action<GameDataEvent> OnInventoryChanged
    {
        add => _eventBus.OnInventoryChanged += value;
        remove => _eventBus.OnInventoryChanged -= value;
    }

    public event Action SaveRequested
    {
        add => _eventBus.SaveRequested += value;
        remove => _eventBus.SaveRequested -= value;
    }

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _store.InventoryTable = InventoryTable.CreateDefault();
    }

    #endregion

    #region Batch / Notify (EventBus 위임)

    public void BeginBatch() => _eventBus.BeginBatch();
    public void EndBatch() => _eventBus.EndBatch();

    /// <summary>
    /// action 안에서 여러 Notify를 호출해도 EndBatch를 반드시 호출함. 예외/return으로 짝이 틀어지는 실수 방지.
    /// </summary>
    public void RunBatched(Action action) => _eventBus.RunBatched(action);

    /// <summary>
    /// Controller가 데이터 변경 후 호출. 이벤트 발행 및 필요 시 SaveRequested 발생.
    /// </summary>
    public void Notify(GameDataEventKind kind, string id = null, int intValue = 0, bool requestSave = true)
    {
        _eventBus.Notify(kind, id, intValue, requestSave);
    }

    /// <summary>
    /// 변경된 데이터 T를 담아서 발행. 구독측에서 evt.GetValue&lt;T&gt;()로 꺼내 씀.
    /// </summary>
    public void Notify<T>(GameDataEventKind kind, T value, string id = null, int intValue = 0, bool requestSave = true)
    {
        _eventBus.Notify(kind, value, id, intValue, requestSave);
    }

    #endregion
}
