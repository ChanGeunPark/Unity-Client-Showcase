using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;

public static class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    public static Color HexToColor(string color)
    {
        Color parsedColor;
        ColorUtility.TryParseHtmlString("#" + color, out parsedColor);

        return parsedColor;
    }

    public static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    public static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public static string MyMethod()
    {
        // 현재 메서드의 호출 스택 추적
        StackTrace stackTrace = new StackTrace();

        // 첫 번째 프레임(현재 메서드를 호출한 위치)의 메서드 정보 가져오기
        StackFrame stackFrame = stackTrace.GetFrame(1);
        MethodBase method = stackFrame.GetMethod();

        // 메서드명 출력
        string methodName = method.Name;
        return methodName;
    }

    public static void DestroyChilds(GameObject go, float delay = 0f, Action callback = null)
    {
        if (go == null || go.transform == null) return;

        Transform[] children = new Transform[go.transform.childCount];
        for (int i = 0; i < go.transform.childCount; i++)
        {
            children[i] = go.transform.GetChild(i);
        }

        if (delay > 0f)
        {
            DOTween.Sequence().AppendInterval(delay).AppendCallback(() =>
            {
                foreach (Transform child in children)
                {
                    if (child != null)
                    {
                        // 실제로 제거하여 월드에 남지 않도록 파괴
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
                callback?.Invoke();
            });
        }
        else
        {
            // 모든 자식 오브젝트 삭제
            foreach (Transform child in children)
            {
                if (child != null)
                {
                    // 부모에서 떼는 대신 바로 파괴하여 씬에 남지 않도록 처리
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }




    public static List<T> FindChild02<T>(GameObject root, string containsName = null)
    {
        List<T> result = new List<T>();

        if (root.activeSelf == false)
        {
            return result;
        }

        T childRoot = root.GetComponent<T>();
        if (childRoot != null)
        {
            if (containsName != null && root.name.Contains(containsName))
            {
                result.Add(childRoot);
            }
            else
            {
                result.Add(childRoot);
            }

        }

        foreach (Transform child in root.transform)
        {
            result.AddRange(FindChild02<T>(child.gameObject));
        }

        return result;
    }

    public static IEnumerable<T> UnionAll<T>(params IEnumerable<T>[] lists)
    {
        foreach (var list in lists)
        {
            foreach (var item in list)
            {
                yield return item;
            }
        }
    }

    public static Vector3 Vector3ZtoZero(Vector3 vector3)
    {
        return new Vector3(vector3.x, vector3.y, 0);
    }

    public static Vector3Int ZtoZero(Vector3Int vector3)
    {
        return new Vector3Int(vector3.x, vector3.y, 0);
    }

    public static Vector2 GetRandomPosition(float maxX, float maxY, float minX = 0, float minY = 0)
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        return new Vector2(randomX, randomY);
    }

    public static int CantorPairing(Vector3Int vector3Int)
    {
        return CantorPairing(vector3Int.x, vector3Int.y);
    }

    public static int Zigzag(int n)
    {
        return n >= 0 ? 2 * n : -2 * n - 1;
    }

    public static int CantorPairing(int x, int y)
    {
        // x = Zigzag(x);
        // y = Zigzag(y);
        return (x + y) * (x + y + 1) / 2 + y;
    }

    public static float FrameToTime(int frames)
    {
        return frames / 60.0f;
    }

    public static void ExecuteActionWithCallback(Action action, Action callback)
    {
        action?.Invoke();
        callback?.Invoke();
    }

    public static void ExecuteActionWithCallback<T>(Action<T> action, T param, Action callback)
    {
        action?.Invoke(param);
        callback?.Invoke();
    }

    public static double ConvertIntToDecimal(int value, int value2 = 10)
    {
        int digitCount = value.ToString().Length;
        return value / Math.Pow(value2, digitCount);
    }


    public static GameObject GetObjectInChild(GameObject parent, string objectName)
    {
        if (parent == null)
        {
            UnityEngine.Debug.LogWarning($"Parent object is null while searching for {objectName}");
            return null;
        }

        // 현재 오브젝트의 이름이 찾고자 하는 이름과 일치하는지 확인
        if (parent.name == objectName)
        {
            return parent;
        }


        // 모든 자식들을 재귀적으로 검색
        foreach (Transform child in parent.transform)
        {
            // 현재 자식의 이름 확인
            if (child.name == objectName)
            {
                return child.gameObject;
            }

            // 재귀적으로 자식의 자식들을 검색
            GameObject found = GetObjectInChild(child.gameObject, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static (string tierName, int tierLevel) ParseTierData(string rawTierData)
    {
        string[] parts = rawTierData.Split('_');
        if (parts.Length != 2)
        {
            return (rawTierData, 0);
        }

        string tierName = parts[0];
        if (!int.TryParse(parts[1], out int tierLevel))
        {
            return (rawTierData, 0);
        }

        return (tierName, tierLevel);
    }

    // ========================= 추가 유틸리티 =========================

    // 값의 범위를 다른 범위로 선형 맵핑합니다. 예: 0~1 -> 0~100
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (Math.Abs(fromMax - fromMin) < float.Epsilon)
            return toMin;
        float t = (value - fromMin) / (fromMax - fromMin);
        return Mathf.Lerp(toMin, toMax, t);
    }

    // 각도를 -180~180 범위로 정규화합니다.
    public static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle <= -180f) angle += 360f;
        return angle;
    }

    // 거의 0인지(부동소수점 오차 허용) 판단합니다.
    public static bool ApproximatelyZero(float value, float epsilon = 1e-6f)
    {
        return Mathf.Abs(value) <= epsilon;
    }

    // 범위 내에 포함되는지(포함 범위) 확인합니다.
    public static bool IsInRange(float value, float minInclusive, float maxInclusive)
    {
        return value >= minInclusive && value <= maxInclusive;
    }

    // 숫자를 K/M/B/T 표기로 압축해서 문자열로 반환합니다. 예: 15300 -> "15.3K"
    public static string FormatNumberCompact(double value, int decimals = 1)
    {
        double abs = Math.Abs(value);
        string suffix = "";
        double divisor = 1d;

        if (abs >= 1_000_000_000_000d) { suffix = "T"; divisor = 1_000_000_000_000d; }
        else if (abs >= 1_000_000_000d) { suffix = "B"; divisor = 1_000_000_000d; }
        else if (abs >= 1_000_000d) { suffix = "M"; divisor = 1_000_000d; }
        else if (abs >= 1_000d) { suffix = "K"; divisor = 1_000d; }

        if (divisor == 1d)
            return Math.Round(value).ToString();

        double compact = value / divisor;
        string format = "F" + Mathf.Clamp(decimals, 0, 3);
        string str = compact.ToString(format);
        // 불필요한 0 제거
        if (decimals > 0)
            str = str.TrimEnd('0').TrimEnd('.');
        return str + suffix;
    }

    // CanvasGroup의 알파값을 설정합니다. (선택적으로 인터랙션도 토글)
    public static void SetAlpha(CanvasGroup canvasGroup, float alpha, bool toggleInteraction = false)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        if (toggleInteraction)
        {
            bool interactable = canvasGroup.alpha > 0.001f;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    // Color에 알파만 변경한 새 Color를 반환합니다.
    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // GameObject의 활성 상태가 달라질 때만 SetActive를 호출합니다.
    public static void SetActiveIfChanged(GameObject go, bool desiredActive)
    {
        if (go != null && go.activeSelf != desiredActive)
        {
            go.SetActive(desiredActive);
        }
    }

    // 이벤트 시스템 기반으로 현재 포인터(마우스/터치)가 UI 위에 있는지 확인합니다. (모바일 대응)
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // 터치가 하나라도 UI 위에 있으면 true
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
            return false;
        }

        // 마우스(에디터/PC)
        return EventSystem.current.IsPointerOverGameObject();
    }

    // 월드 좌표를 Canvas(LocalPoint / AnchoredPosition 기준) 좌표로 변환합니다.
    public static Vector2 WorldToCanvasPosition(Canvas canvas, Vector3 worldPosition, Camera worldCamera)
    {
        if (canvas == null) return Vector2.zero;
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return Vector2.zero;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out localPoint
        );
        return localPoint;
    }

    // 모든 자식 포함, 레이어를 재귀적으로 변경합니다.
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // 리스트를 제자리에서 셔플합니다. (Fisher–Yates)
    public static void ShuffleInPlace<T>(IList<T> list, int? seed = null)
    {
        if (list == null || list.Count <= 1) return;
        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    // 리스트에서 임의의 원소 하나를 반환합니다. 비어있으면 default 반환.
    public static T RandomElement<T>(IList<T> list)
    {
        if (list == null || list.Count == 0) return default;
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    // 가중치 목록에서 인덱스를 하나 선택합니다. (합이 0 이하이면 -1)
    public static int WeightedRandomIndex(IList<float> weights)
    {
        if (weights == null || weights.Count == 0) return -1;
        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            float w = Mathf.Max(0f, weights[i]);
            total += w;
        }
        if (total <= 0f) return -1;

        float r = Random.value * total;
        float cumulative = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += Mathf.Max(0f, weights[i]);
            if (r <= cumulative)
                return i;
        }
        return weights.Count - 1;
    }

    // 아이템과 가중치로부터 하나를 선택합니다. (실패 시 default 반환)
    public static T WeightedRandom<T>(IList<T> items, IList<float> weights)
    {
        if (items == null || weights == null) return default;
        if (items.Count == 0 || items.Count != weights.Count) return default;
        int idx = WeightedRandomIndex(weights);
        if (idx < 0 || idx >= items.Count) return default;
        return items[idx];
    }

    // 문자열을 열거형으로 안전하게 파싱합니다.
    public static bool TryParseEnum<T>(string value, out T result, bool ignoreCase = true) where T : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default;
            return false;
        }
        return Enum.TryParse(value, ignoreCase, out result);
    }

    // 초 단위를 "HH:MM:SS" 또는 "MM:SS" 형태로 변환합니다.
    public static string SecondsToTimeString(int totalSeconds, bool forceHours = false)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        if (forceHours || hours > 0)
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        return $"{minutes:D2}:{seconds:D2}";
    }

    // RectTransform의 X/Y 위치만 간편하게 변경합니다.
    public static void SetAnchoredPosX(RectTransform rt, float x)
    {
        if (rt == null) return;
        Vector2 p = rt.anchoredPosition;
        p.x = x;
        rt.anchoredPosition = p;
    }

    public static void SetAnchoredPosY(RectTransform rt, float y)
    {
        if (rt == null) return;
        Vector2 p = rt.anchoredPosition;
        p.y = y;
        rt.anchoredPosition = p;
    }

    // 부모의 직계 자식 중 이름으로 찾고, 없으면 새로 생성해서 반환합니다.
    public static GameObject GetOrCreateChild(GameObject parent, string childName, bool worldPositionStays = false)
    {
        if (parent == null || string.IsNullOrEmpty(childName)) return null;
        Transform found = null;
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform c = parent.transform.GetChild(i);
            if (c.name == childName)
            {
                found = c;
                break;
            }
        }

        if (found == null)
        {
            GameObject created = new GameObject(childName);
            created.transform.SetParent(parent.transform, worldPositionStays);
            return created;
        }

        return found.gameObject;
    }

    // 액션을 안전하게 호출합니다(null 체크 포함).
    public static void SafeInvoke(Action action)
    {
        action?.Invoke();
    }



    public static ScrollRect CenterScrollRectOnHolder(ScrollRect scrollRect, Transform container, string holderName, bool isHorizontal)
    {
        // ScrollRect 내에서 특정 홀더(아이템 등)를 중앙에 맞추는 기능입니다.
        if (scrollRect == null || container == null) return null; // ScrollRect와 컨테이너 유효성 검사

        // 컨테이너에서 홀더 이름으로 RectTransform을 찾음
        RectTransform target = container.Find(holderName) as RectTransform;
        if (target == null && container.childCount > 0)
        {
            // 이름이 정확히 일치하는 자식이 없으면 대소문자 무시하고 반복 탐색
            for (int i = 0; i < container.childCount; i++)
            {
                if (container.GetChild(i).name.Equals(holderName, StringComparison.OrdinalIgnoreCase))
                {
                    target = container.GetChild(i) as RectTransform;
                    break;
                }
            }
        }

        if (target == null) return null; // 못 찾으면 중단

        // ScrollRect의 뷰포트와 컨텐츠 RectTransform 가져오기
        RectTransform viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
        RectTransform content = scrollRect.content;
        if (viewport == null || content == null) return null; // 유효성 검사

        // 캔버스 갱신하여 RectTransform 레이아웃을 최신으로 만듦
        Canvas.ForceUpdateCanvases();

        // 뷰포트 중앙과 타겟 중앙의 월드 좌표 계산
        Vector3 viewportWorldCenter = viewport.TransformPoint(viewport.rect.center);
        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);

        // content 좌표계로 변환하여 각 중앙점 위치를 비교
        Vector3 viewportCenterInContent = content.InverseTransformPoint(viewportWorldCenter);
        Vector3 targetCenterInContent = content.InverseTransformPoint(targetWorldCenter);

        // 뷰포트 중앙이 타겟 중앙에 오도록 content의 위치를 보정
        Vector3 offset = viewportCenterInContent - targetCenterInContent;
        Vector3 newPosition = content.localPosition;

        if (isHorizontal && scrollRect.horizontal)
        {
            newPosition.x += offset.x;
        }
        else if (!isHorizontal && scrollRect.vertical)
        {
            newPosition.y += offset.y;
        }
        else if (isHorizontal && !scrollRect.horizontal)
        {
            newPosition.x += offset.x;
        }
        else if (!isHorizontal && !scrollRect.vertical)
        {
            newPosition.y += offset.y;
        }

        content.localPosition = newPosition;

        return scrollRect;
    }
}