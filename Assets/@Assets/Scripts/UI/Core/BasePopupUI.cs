using System;

using DG.Tweening;

using UnityEngine;

public class BasePopupUI : BaseUI
{
    public Action OnClosedPopup { get; set; }
    private void OnDestroy()
    {
        InitDestroy();
    }

    public virtual void InitDestroy()
    {
        if (this != null)
        {
            UIManager.Instance.ClosePopupUI(this);
        }
    }

    public virtual void Close()
    {
        UIManager.Instance.ClosePopupUI(this);

        if (OnClosedPopup != null)
        {
            OnClosedPopup?.Invoke();
        }
    }

    public void CloseDelay(float delayTime = 0)
    {
        DOTween.Sequence().AppendInterval(delayTime).AppendCallback(() =>
        {
            UIManager.Instance.ClosePopupUI(this);
            if (OnClosedPopup != null) OnClosedPopup?.Invoke();
        });
    }


    public override void OnResume()
    {
        base.OnResume();
    }


    #region Animation

    protected void ShowPopupAnimation(object idx, float duration = 0.3f, Vector3 customScale = default)
    {
        var obj = GetObject(idx);


        var scale = Vector3.one;

        if (customScale != default)
        {
            scale = customScale;
        }

        if (obj != null)
        {
            // 초기 스케일을 (0, 0, 0)으로 설정
            obj.transform.localScale = Vector3.zero;

            // 스케일을 (1, 1, 1)로 애니메이션
            obj.transform.DOScale(scale, duration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }


    protected void HidePopupAnimation(object idx, float duration = 0.3f)
    {
        var obj = GetObject(idx);
        if (obj != null)
        {
            obj.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack).SetUpdate(true);
        }
    }

    protected void FadeOutPopup(MonoBehaviour currentObject, float duration = 0.3f)
    {
        var _canvasGroup = currentObject.GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.DOFade(0, duration).SetUpdate(true);
    }

    protected void FadeInPopup(MonoBehaviour currentObject, float duration = 0.3f)
    {
        var _canvasGroup = currentObject.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 0;
        _canvasGroup.DOFade(1, duration).SetUpdate(true);
    }

    protected void ClosePopupWithAnimation(MonoBehaviour currentObject, object idx, float duration = 0.3f, Action onComplete = null)
    {
        FadeOutPopup(currentObject, duration);

        if (idx != null)
        {
            HidePopupAnimation(idx, duration);
        }
        CloseDelay(duration);
        DOTween.Sequence()
            .AppendInterval(duration)
            .AppendCallback(() =>
            {
                onComplete?.Invoke();
            }).SetUpdate(true);
    }

    protected void ShowPopupWithAnimation(MonoBehaviour currentObject, object idx, float duration = 0.3f)
    {
        if (idx != null)
        {
            ShowPopupAnimation(idx, duration);
        }
        FadeInPopup(currentObject, duration);
    }


    #endregion
}
