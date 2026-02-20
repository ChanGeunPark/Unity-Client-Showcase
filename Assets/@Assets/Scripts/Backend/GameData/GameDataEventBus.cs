using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 데이터 변경 이벤트 배치 및 발행.
/// BeginBatch~EndBatch 구간에서는 이벤트를 모았다가 EndBatch 시 한 번에 발행합니다.
/// </summary>
public class GameDataEventBus
{
    /// <summary>
    /// 변경 사항 옵저버용. 배치 시 리스트로, 비배치 시 Count=1로 전달됩니다.
    /// </summary>
    public event Action<IReadOnlyList<GameDataEvent>> OnEvents;
    public event Action<GameDataEvent> OnInventoryChanged;

    /// <summary>
    /// 저장 요청. 구독처(Repository 등)에서 저장을 수행합니다.
    /// </summary>
    public event Action SaveRequested;

    private int _batchDepth;
    private readonly List<GameDataEvent> _queuedEvents = new List<GameDataEvent>(32);

    public void BeginBatch()
    {
        _batchDepth++;
    }

    public void EndBatch()
    {
        _batchDepth = Mathf.Max(0, _batchDepth - 1);
        if (_batchDepth != 0) return;
        FlushEvents();
    }

    /// <summary>
    /// action 안에서 여러 Notify를 호출해도 EndBatch를 반드시 호출함. 예외/return으로 짝이 틀어지는 실수 방지.
    /// </summary>
    public void RunBatched(Action action)
    {
        if (action == null) return;
        BeginBatch();
        try
        {
            action();
        }
        finally
        {
            EndBatch();
        }
    }

    /// <summary>
    /// Controller가 데이터 변경 후 호출. 이벤트 발행 및 필요 시 SaveRequested 발생.
    /// </summary>
    public void Notify(GameDataEventKind kind, string id = null, int intValue = 0, bool requestSave = true)
    {
        Emit(new GameDataEvent(kind, id, intValue, null));
        if (requestSave)
        {
            SaveRequested?.Invoke();
        }
    }

    /// <summary>
    /// 변경된 데이터 T를 담아서 발행. 구독측에서 evt.GetValue&lt;T&gt;()로 꺼내 씀.
    /// </summary>
    public void Notify<T>(GameDataEventKind kind, T value, string id = null, int intValue = 0, bool requestSave = true)
    {
        Emit(new GameDataEvent(kind, id, intValue, value));
        if (requestSave)
        {
            SaveRequested?.Invoke();
        }
    }


    private void Emit(GameDataEvent evt)
    {
        if (_batchDepth > 0)
        {
            _queuedEvents.Add(evt);
            return;
        }

        _queuedEvents.Add(evt);
        FlushEvents();
    }

    private void FlushEvents()
    {
        if (_queuedEvents.Count == 0) return;

        for (int i = 0; i < _queuedEvents.Count; i++)
        {
            var evt = _queuedEvents[i];
            if (IsInventoryEvent(evt.Kind))
            {
                OnInventoryChanged?.Invoke(evt);
            }
        }

        OnEvents?.Invoke(_queuedEvents);
        _queuedEvents.Clear();
    }

    private static bool IsInventoryEvent(GameDataEventKind kind)
    {
        return kind == GameDataEventKind.InventoryLoaded
            || kind == GameDataEventKind.InventoryChanged;
    }
}
