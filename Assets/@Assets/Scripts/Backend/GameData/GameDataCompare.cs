using System;
using System.Collections.Generic;

/// <summary>
/// 테이블/리스트 변경 감지용 비교 유틸. Store setter 등에서 "이전과 같은지" 판단할 때 사용.
/// </summary>
public static class GameDataCompare
{
    /// <summary>
    /// 두 리스트가 동일한지 비교. null·Count·요소 순서까지 비교.
    /// itemEquals가 null이면 EqualityComparer&lt;T&gt;.Default 사용 (T가 IEquatable 구현 시 자동 적용).
    /// </summary>
    public static bool ListEquals<T>(
        List<T> a,
        List<T> b,
        Func<T, T, bool> itemEquals = null)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        if (itemEquals != null)
        {
            for (int i = 0; i < a.Count; i++)
            {
                var x = a[i];
                var y = b[i];
                if (x == null && y == null) continue;
                if (x == null || y == null) return false;
                if (!itemEquals(x, y)) return false;
            }
        }
        else
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!comparer.Equals(a[i], b[i])) return false;
            }
        }

        return true;
    }
}
