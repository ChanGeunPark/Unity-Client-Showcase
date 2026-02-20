using System;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Object = UnityEngine.Object;

/// <summary>
/// UI 루트 베이스 클래스. Enum 기반 자식 오브젝트/컴포넌트 바인딩, 버튼 리스너 수명 주기 관리.
/// </summary>
public class BaseUI : MonoBehaviour
{
    private const float DefaultTweenDuration = 1f;

    /// <summary>타입별로 바인딩된 오브젝트/컴포넌트 캐시 (Enum 순서 = 인덱스)</summary>
    protected Dictionary<Type, Object[]> _boundObjects = new();

    /// <summary>버튼별 등록된 리스너: Action -> UnityAction 래퍼 (제거 시 정확한 매칭용)</summary>
    private readonly Dictionary<Button, List<ButtonListenerEntry>> _buttonListeners = new();

    private sealed class ButtonListenerEntry
    {
        public readonly Action Action;
        public readonly UnityAction UnityAction;

        public ButtonListenerEntry(Action action, UnityAction unityAction)
        {
            Action = action;
            UnityAction = unityAction;
        }
    }

    private void Awake()
    {
        InitAwake();
    }

    public virtual void InitAwake() { }

    protected virtual void OnDestroy()
    {
        CleanupAllButtonListeners();
    }

    protected void PlayUIButtonPressSound() { }

    // ------ Lifecycle / HUD ------

    public void DelayActive(GameObject target, float delay, bool isActive)
    {
        StartCoroutine(DelayActiveCoroutine(target, delay, isActive));
    }

    private static IEnumerator DelayActiveCoroutine(GameObject target, float delay, bool isActive)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            target.SetActive(isActive);
    }

    public virtual void OnPause() { }
    public virtual void OnResume() { }

    #region Bind

    protected void Bind<T>(Type type) where T : Object
    {
        string[] names = Enum.GetNames(type);
        var objects = new Object[names.Length];
        _boundObjects[typeof(T)] = objects;

        for (int i = 0; i < names.Length; i++)
        {
            objects[i] = typeof(T) == typeof(GameObject)
                ? Util.FindChild(gameObject, names[i], true)
                : Util.FindChild<T>(gameObject, names[i], true);
        }
    }

    protected void BindObject(Type type) => Bind<GameObject>(type);
    protected void BindImage(Type type) => Bind<Image>(type);
    protected void BindText(Type type) => Bind<TextMeshProUGUI>(type);
    protected void BindButton(Type type) => Bind<Button>(type);
    protected void BindToggle(Type type) => Bind<Toggle>(type);
    protected void BindSlider(Type type) => Bind<Slider>(type);
    protected void BindInputField(Type type) => Bind<TMP_InputField>(type);
    protected void BindScrollView(Type type) => Bind<ScrollRect>(type);

    #endregion

    #region Get

    protected T Get<T>(object idx) where T : Object
    {
        if (!_boundObjects.TryGetValue(typeof(T), out Object[] objects))
            return null;
        int index = Convert.ToInt32(idx);
        if (index < 0 || index >= objects.Length)
            return null;
        return objects[index] as T;
    }

    /// <summary>다른 타입 키로 저장된 배열에서 가져올 때 사용 (키가 typeof(T)가 아닌 경우)</summary>
    protected T Get<T>(Type typeKey, object idx) where T : Object
    {
        if (!_boundObjects.TryGetValue(typeKey, out Object[] objects))
            return null;
        int index = Convert.ToInt32(idx);
        if (index < 0 || index >= objects.Length)
            return null;
        return objects[index] as T;
    }

    protected GameObject GetObject(object idx) => Get<GameObject>(idx);
    protected ScrollRect GetScrollView(object idx) => Get<ScrollRect>(idx);
    protected TextMeshProUGUI GetText(object idx) => Get<TextMeshProUGUI>(idx);
    protected TMP_InputField GetInputField(object idx) => Get<TMP_InputField>(idx);
    protected Button GetButton(object idx) => Get<Button>(idx);
    protected Image GetImage(object idx) => Get<Image>(idx);
    protected Toggle GetToggle(object idx) => Get<Toggle>(idx);
    protected Slider GetSlider(object idx) => Get<Slider>(idx);

    #endregion

    #region Unbind

    protected void Unbind<T>(Type type) where T : Object
    {
        if (!_boundObjects.TryGetValue(typeof(T), out Object[] objects))
            return;
        string[] names = Enum.GetNames(type);
        foreach (string name in names)
        {
            int idx = Array.FindIndex(objects, o => o != null && o.name == name);
            if (idx >= 0)
                objects[idx] = null;
        }
    }

    protected void UnbindObject(Type type) => Unbind<GameObject>(type);
    protected void UnbindImage(Type type) => Unbind<Image>(type);
    protected void UnbindText(Type type) => Unbind<TextMeshProUGUI>(type);
    protected void UnbindButton(Type type) => Unbind<Button>(type);
    protected void UnbindToggle(Type type) => Unbind<Toggle>(type);

    #endregion

    #region Set

    protected void SetText<T>(object idx, string text) where T : TextMeshProUGUI
    {
        var uiText = Get<T>(idx);
        if (uiText != null)
            uiText.text = text;
    }

    protected void SetText(object idx, string text)
    {
        var uiText = GetText(idx);
        if (uiText != null)
            uiText.text = text;
    }

    protected void SetImage<T>(object idx, Sprite image) where T : Image
    {
        var uiImage = Get<T>(idx);
        if (uiImage != null)
            uiImage.sprite = image;
    }

    protected void SetImage(object idx, Sprite image)
    {
        var uiImage = GetImage(idx);
        if (uiImage != null)
            uiImage.sprite = image;
    }

    protected void SetSliderValue(object sliderType, float value, float maxValue, bool isAnimation = false, float startValue = 0f)
    {
        var slider = GetSlider(sliderType);
        if (slider == null)
            return;
        slider.minValue = startValue;
        slider.maxValue = maxValue;
        if (isAnimation)
            slider.DOValue(value, DefaultTweenDuration).SetEase(Ease.InOutQuart);
        else
            slider.value = value;
    }

    protected void SetImageFillAmount(Image image, float value, float maxValue, bool isAnimation = false)
    {
        if (image == null)
            return;
        float fill = value / maxValue;
        if (isAnimation)
            image.DOFillAmount(fill, DefaultTweenDuration).SetEase(Ease.InOutQuart);
        else
            image.fillAmount = fill;
    }

    protected void AddButtonListener(object buttonType, Action action)
    {
        var button = GetButton(buttonType);
        if (button == null)
            return;

        UnityAction unityAction = () => action.Invoke();
        button.onClick.AddListener(unityAction);

        if (!_buttonListeners.TryGetValue(button, out var list))
        {
            list = new List<ButtonListenerEntry>();
            _buttonListeners[button] = list;
        }
        list.Add(new ButtonListenerEntry(action, unityAction));
    }

    protected void RemoveButtonListener(object idx, Action action)
    {
        var button = GetButton(idx);
        if (button == null || !_buttonListeners.TryGetValue(button, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Action != action)
                continue;
            var entry = list[i];
            button.onClick.RemoveListener(entry.UnityAction);
            list.RemoveAt(i);
            break;
        }
        if (list.Count == 0)
            _buttonListeners.Remove(button);
    }

    protected void RemoveAllButtonListeners(object idx)
    {
        var button = GetButton(idx);
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        _buttonListeners.Remove(button);
    }

    private void CleanupAllButtonListeners()
    {
        foreach (var kvp in _buttonListeners)
        {
            if (kvp.Key == null)
                continue;
            foreach (var entry in kvp.Value)
                kvp.Key.onClick.RemoveListener(entry.UnityAction);
        }
        _buttonListeners.Clear();
    }

    #endregion

    #region Visibility (Show / Hide)

    protected void SetObjectActive(object idx, bool active)
    {
        var obj = GetObject(idx);
        if (obj != null)
            obj.SetActive(active);
    }

    protected void HideObject(object idx) => SetObjectActive(idx, false);
    protected void ShowObject(object idx) => SetObjectActive(idx, true);
    protected void HideButton(object idx) => SetComponentActive(GetButton(idx), false);
    protected void ShowButton(object idx) => SetComponentActive(GetButton(idx), true);
    protected void HideText(object idx) => SetComponentActive(GetText(idx), false);
    protected void ShowText(object idx) => SetComponentActive(GetText(idx), true);
    protected void HideImage(object idx) => SetComponentActive(GetImage(idx), false);
    protected void ShowImage(object idx) => SetComponentActive(GetImage(idx), true);

    private static void SetComponentActive(Component component, bool active)
    {
        if (component != null && component.gameObject != null)
            component.gameObject.SetActive(active);
    }

    #endregion

    #region Util

    protected void DelayAction(Action action, float delay)
    {
        DOTween.Sequence().AppendInterval(delay).AppendCallback(() => action());
    }

    protected Sprite LoadSprite(string spriteName)
    {
        return ResourceManager.Instance.LoadSprite(spriteName);
    }

    #endregion
}
