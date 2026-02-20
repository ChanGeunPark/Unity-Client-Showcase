using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 루트·캔버스·씬/허드/팝업 생명주기 관리. 싱글톤.
/// </summary>
public class UIManager : MonoBehaviour
{
    private const int DefaultSortOrder = 10;
    private const int ToastSortOrderBase = 500;
    private const int SortOrderStep = 5;
    private static readonly Vector2 ReferenceResolution = new Vector2(1280, 720);
    public static UIManager Instance { get; private set; }

    private int _order = DefaultSortOrder;
    private int _toastOrder = ToastSortOrderBase;
    public Stack<BasePopupUI> PopupStack { get; private set; } = new Stack<BasePopupUI>();
    public BaseUI TransitionUI { get; set; }

    public Action<BasePopupUI> OnPopupUIShowed;
    public Action<BasePopupUI> OnPopupUIClosed;
    public Action OnClosePopupUI;

    #region Roots

    private GameObject _root;
    public GameObject Root
    {
        get
        {
            if (_root == null)
            {
                _root = GameObject.Find("@UI_Root");
                if (_root == null)
                    _root = new GameObject { name = "@UI_Root" };
            }
            return _root;
        }
    }

    private GameObject _worldSpaceRoot;
    public GameObject WorldSpaceRoot
    {
        get
        {
            if (_worldSpaceRoot == null)
            {
                _worldSpaceRoot = GameObject.Find("@UI_WorldSpaceRoot");
                if (_worldSpaceRoot == null)
                    _worldSpaceRoot = ResourceManager.Instance.Instantiate("@UI_WorldSpaceRoot", Root.transform);
            }
            return _worldSpaceRoot;
        }
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
    }

    public void Clear()
    {
        CloseAllPopupUI();
        Time.timeScale = 1f;
        Util.DestroyChilds(Root);
        _root = null;
        _worldSpaceRoot = null;
    }

    #endregion

    #region Canvas

    public void SetCanvas(GameObject go, bool sort = true, int sortOrder = 0, bool isToast = false)
    {
        var canvas = Util.GetOrAddComponent<Canvas>(go);
        if (canvas == null)
            return;

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;

        var scaler = Util.GetOrAddComponent<CanvasScaler>(go);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
        }

        Util.GetOrAddComponent<GraphicRaycaster>(go);

        if (isToast)
        {
            _toastOrder++;
            canvas.sortingOrder = _toastOrder;
        }
        else
        {
            canvas.sortingOrder = sort ? _order : sortOrder;
            if (sort)
                _order += SortOrderStep;
        }
    }

    #endregion

    #region Popup — Show

    public T ShowPopupUI<T>(bool useSetCanvas = true) where T : BasePopupUI
    {
        string name = typeof(T).Name;
        var go = InstantiateUI(name);
        if (go == null)
            return null;

        var popup = Util.GetOrAddComponent<T>(go);
        PushPopupAndNotify(popup, go, useSetCanvas);
        return popup;
    }

    public async UniTask ShowPopupUI<T>(string label, Action<T> callback) where T : BasePopupUI
    {
        await ResourceManager.Instance.LoadAsync<UnityEngine.Object>(label);
        var popup = ShowPopupUI<T>();
        callback?.Invoke(popup);
    }

    public BasePopupUI ShowPopupUI(string popupName, bool useSetCanvas = true)
    {
        if (string.IsNullOrEmpty(popupName))
        {
            Debug.LogError("ShowPopupUI(string): popupName is null or empty.");
            return null;
        }

        var go = InstantiateUI(popupName);
        if (go == null)
            return null;

        var popupType = FindPopupTypeByName(popupName);
        if (popupType == null)
        {
            Debug.LogError($"ShowPopupUI(string): No type named '{popupName}' deriving from BasePopupUI.");
            ResourceManager.Instance.Destroy(go);
            return null;
        }

        var popup = go.GetComponent(popupType) as BasePopupUI ?? go.AddComponent(popupType) as BasePopupUI;
        if (popup == null)
        {
            ResourceManager.Instance.Destroy(go);
            return null;
        }

        PushPopupAndNotify(popup, go, useSetCanvas);
        return popup;
    }

    private void PushPopupAndNotify(BasePopupUI popup, GameObject go, bool useSetCanvas)
    {
        PopupStack.Push(popup);

        if (PopupStack.Count == 1)
            return;

        go.transform.SetParent(Root.transform);
        if (useSetCanvas)
            SetCanvas(go, sort: true);

        OnPopupUIShowed?.Invoke(popup);
    }

    #endregion

    #region Popup — Close

    public void ClosePopupUI(BasePopupUI popup)
    {
        if (popup == null)
            return;
        ClosePopupFromStack(popup);
        ResumeSceneOrTopPopup();
        if (PopupStack.Count == 0)
            OnClosePopupUI?.Invoke();
    }

    public void ClosePopupUI()
    {
        if (PopupStack.Count == 0)
            return;

        var popup = PopupStack.Pop();
        DestroyPopup(popup);
        ResumeSceneOrTopPopup();
    }

    public void CloseAllPopupUI()
    {
        while (PopupStack.Count > 0)
            ClosePopupUI();
    }

    public int GetPopupCount() => PopupStack.Count;

    private void ClosePopupFromStack(BasePopupUI popup)
    {
        if (PopupStack.Count == 0)
            return;

        bool wasTop = PopupStack.Peek() == popup;
        var toReopen = new Stack<BasePopupUI>();

        while (PopupStack.Count > 0)
        {
            var top = PopupStack.Pop();
            if (top != popup)
                toReopen.Push(top);
            else
                break;
        }

        OnPopupUIClosed?.Invoke(popup);
        DestroyPopup(popup);

        while (toReopen.Count > 0)
            PopupStack.Push(toReopen.Pop());

        if (wasTop)
            ResumeSceneOrTopPopup();
    }

    private static void DestroyPopup(BasePopupUI popup)
    {
        if (popup == null)
            return;
        popup.gameObject.SetActive(false);
        ResourceManager.Instance.Destroy(popup.gameObject);
    }

    private void ResumeSceneOrTopPopup()
    {
        if (PopupStack.Count == 0)
            return;
        else
            PopupStack.Peek()?.OnResume();
    }

    /// <summary>스택에서 null 항목만 제거해 순서 유지. 필요 시 외부에서 호출.</summary>
    public void CleanNullPopups()
    {
        var temp = new List<BasePopupUI>(PopupStack.Count);
        while (PopupStack.Count > 0)
        {
            var item = PopupStack.Pop();
            if (item != null)
                temp.Add(item);
        }
        for (int i = temp.Count - 1; i >= 0; i--)
            PopupStack.Push(temp[i]);
    }

    #endregion

    #region Tooltip / Item holder

    public T ShowTooltipUI<T>(string name = null) where T : BaseUI
    {
        string prefabName = string.IsNullOrEmpty(name) ? typeof(T).Name : name;
        var go = InstantiateUI(prefabName);
        if (go == null)
            return null;
        var tooltip = Util.GetOrAddComponent<T>(go);
        go.transform.SetParent(Root.transform);
        return tooltip;
    }

    public T ShowTooltipInParent<T>(string name, Transform parent) where T : BaseUI
    {
        if (string.IsNullOrEmpty(name) || parent == null)
            return null;
        var go = ResourceManager.Instance.Instantiate(name, parent);
        if (go == null)
            return null;
        go.transform.SetParent(parent);
        return Util.GetOrAddComponent<T>(go);
    }

    public T MakeItemHolder<T>(Transform parent = null, string name = null) where T : BaseUI
    {
        string prefabName = string.IsNullOrEmpty(name) ? typeof(T).Name : name;
        var go = ResourceManager.Instance.Instantiate(prefabName, parent);
        if (go == null)
            return null;
        if (parent != null)
            go.transform.SetParent(parent);
        return Util.GetOrAddComponent<T>(go);
    }

    #endregion

    #region Query (active popups)

    public bool IsAnyPopupOpen() => GetActivePopups().Any(p => p != null && p.gameObject.activeInHierarchy);
    public int GetActivePopupCount() => GetActivePopups().Count(p => p != null && p.gameObject.activeInHierarchy);

    public List<string> GetActivePopupNames() => GetActivePopups()
        .Where(p => p != null && p.gameObject.activeInHierarchy)
        .Select(p => p.GetType().Name)
        .ToList();

    private BasePopupUI[] GetActivePopups() => Root.GetComponentsInChildren<BasePopupUI>(false) ?? Array.Empty<BasePopupUI>();

    #endregion

    #region Helpers

    private GameObject InstantiateUI(string prefabName)
    {
        var go = ResourceManager.Instance.Instantiate(prefabName);
        if (go == null)
            Debug.LogError($"Failed to Instantiate prefab: {prefabName}");
        return go;
    }

    private static readonly Dictionary<string, Type> PopupTypeCache = new Dictionary<string, Type>();

    private static Type FindPopupTypeByName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;
        if (PopupTypeCache.TryGetValue(typeName, out var cached))
            return cached;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetTypes().FirstOrDefault(x =>
                    x.Name == typeName && typeof(BasePopupUI).IsAssignableFrom(x));
                if (t != null)
                {
                    PopupTypeCache[typeName] = t;
                    return t;
                }
            }
            catch
            {
                // some assemblies throw on GetTypes
            }
        }

        PopupTypeCache[typeName] = null;
        return null;
    }

    public void ResetStackAndSortingOrder()
    {
        PopupStack.Clear();
        _order = DefaultSortOrder;
    }

    #endregion
}
