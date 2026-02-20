/// <summary>
/// 게임 데이터 변경 이벤트. Kind별로 바뀐 데이터를 Value(GetValue&lt;T&gt;)로 전달할 수 있음.
/// </summary>
public class GameDataEvent
{
    public readonly GameDataEventKind Kind;
    public readonly string Id;
    public readonly int IntValue;
    /// <summary>Kind에 따른 변경 데이터. 없으면 null.</summary>
    public readonly object Value;

    public GameDataEvent(GameDataEventKind kind, string id = null, int intValue = 0, object value = null)
    {
        Kind = kind;
        Id = id;
        IntValue = intValue;
        Value = value;
    }

    /// <summary>바뀐 데이터를 타입 지정해서 가져옴. Value가 null이면 default(T).</summary>
    public T GetValue<T>()
    {
        if (Value == null) return default;
        return (T)Value;
    }

    /// <summary>타입이 맞을 때만 값을 꺼냄. 캐스트 실패 시 예외 없이 false 반환.</summary>
    public bool TryGetValue<T>(out T value)
    {
        if (Value == null)
        {
            value = default;
            return false;
        }
        if (Value is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }
}

public enum GameDataEventKind
{
    None = 0,
    UserInfoLoaded,
    InventoryLoaded,
    InventoryChanged,

}
