using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using AirFishLab.ScrollingList;
using AssetKits.ParticleImage;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GachaPopupUI : BasePopupUI
{
    #region Enums

    enum Objects
    {
        CircularWithAligning,
        ListBank,
        SmokeAni,
        FrontStoneCats,
        BackgroundGroup,
        Grass,
        BackGroup,
        MainGroup,
        LeftLeaf,
        RightLeaf,
        Portal,
        ShootingStar,
        Bell,
        ShootingStarEffect,
        Sunlight,
        Step1,
        SeowonPopup,
        ShootingStarTail,
        ShootingStarIcon,
        CatFace,
        AfterPortalEnergySpread,
    }

    enum Images
    {
        WhiteOverlay,
        Step1Background,
        GachaStoryImage,
        ShootingStarImage,
    }

    enum Texts
    {
        GachaSpeachText,
    }

    enum Buttons
    {
        SkipButton,
    }

    #endregion

    #region Serialized Fields

    [Tooltip("팝업 연출 설정. Create > Scriptable Objects > Gacha > GachaPopupConfig 로 에셋 생성 후 할당")]
    [SerializeField] private GachaPopupConfig _config;
    [SerializeField] private GameObject _smokeAnimation;
    [SerializeField] private GameObject _afterPortalGroup;
    [SerializeField] private GameObject _step1;

    #endregion

    #region Private State

    private Coroutine _catFaceBlinkRoutine;
    private CircularScrollingList _circularScrollingList;
    private int _tier;
    private CharacterChart _mainGachaCharacterChart;
    private List<CharacterChart> _gachaCharacterChartList;
    private CharacterChart _currentGachaCharacterChart;
    private bool _isProcessingClick;

    #endregion

    #region Unity Lifecycle

    protected override void OnDestroy()
    {
        UnsubscribeScrollAndHolders();
        base.OnDestroy();
    }

    private void Awake()
    {
        BindUI();
        SetInitialVisibility();
        GetButton(Buttons.SkipButton).onClick.AddListener(OnSkipButtonClicked);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            Close();
    }
#endif

    #endregion

    #region Initialization

    public void Initialize(List<CharacterChart> gachaCharacterChartList)
    {
        if (_config == null) return;

        _gachaCharacterChartList = gachaCharacterChartList ?? new List<CharacterChart>();
        _currentGachaCharacterChart = _gachaCharacterChartList.FirstOrDefault();
        _mainGachaCharacterChart = _gachaCharacterChartList
            .OrderBy(g => g.Grade)
            .FirstOrDefault();
        _tier = GetTierFromGrade(_mainGachaCharacterChart?.Grade ?? CharacterGrade.Common);

        InitAnimation();
        DOTween.Sequence()
            .AppendInterval(_config.introDelay)
            .AppendCallback(OnIntroDelayComplete);
    }

    private void BindUI()
    {
        BindObject(typeof(Objects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));
    }

    /// <summary>
    /// 초기 표시 상태 설정
    /// </summary>
    private void SetInitialVisibility()
    {
        GetObject(Objects.CircularWithAligning).gameObject.SetActive(false);
        _afterPortalGroup.gameObject.SetActive(false);
        _step1.gameObject.SetActive(false);
        GetImage(Images.WhiteOverlay).gameObject.SetActive(false);
        GetObject(Objects.Portal).gameObject.SetActive(false);
        GetObject(Objects.ShootingStarEffect).gameObject.SetActive(false);
        GetObject(Objects.ShootingStar).gameObject.SetActive(false);
        GetObject(Objects.Sunlight).gameObject.SetActive(false);
        HideButton(Buttons.SkipButton);
    }

    /// <summary>
    /// 인트로 딜레이 완료 후 초기 표시 상태 설정
    /// </summary>
    private void OnIntroDelayComplete()
    {
        _step1.gameObject.SetActive(true);
        GetImage(Images.Step1Background).DOFade(0.95f, _config.step1FadeDuration).From(0).SetEase(Ease.Linear).SetAutoKill(true);
        GetObject(Objects.CircularWithAligning).gameObject.SetActive(true);

        _circularScrollingList = GetObject(Objects.CircularWithAligning).GetComponent<CircularScrollingList>();
        _smokeAnimation.gameObject.SetActive(false);

        _circularScrollingList.ListSetting.OnMovementEnd.RemoveListener(OnScrollEnded);
        _circularScrollingList.ListSetting.OnMovementEnd.AddListener(OnScrollEnded);

        StartCoroutine(SubscribeToHoldersNextFrame());
    }

    /// <summary>
    /// 등급에 맞는 티어 반환
    /// </summary>
    private static int GetTierFromGrade(CharacterGrade grade)
    {
        return grade == CharacterGrade.Legendary ? 1 : grade == CharacterGrade.Epic ? 2 : 3;
    }

    /// <summary>
    /// 카드 클릭 구독 해제
    /// </summary>
    private void UnsubscribeScrollAndHolders()
    {
        if (_circularScrollingList?.ListSetting != null)
            _circularScrollingList.ListSetting.OnMovementEnd.RemoveListener(OnScrollEnded);
        if (_circularScrollingList == null) return;
        foreach (var item in _circularScrollingList.ListBoxes)
        {
            var holder = item?.GetComponent<GachaIntroHolder>();
            if (holder != null)
                holder.OnClick -= OnItemClicked;
        }
    }

    /// <summary>
    /// 카드 클릭 구독 설정
    /// </summary>
    private IEnumerator SubscribeToHoldersNextFrame()
    {
        yield return null;
        if (_circularScrollingList == null) yield break;
        foreach (var item in _circularScrollingList.ListBoxes)
        {
            if (item == null) continue;
            var holder = item.GetComponent<GachaIntroHolder>();
            if (holder == null) continue;
            holder.OnClick -= OnItemClicked;
            holder.OnClick += OnItemClicked;
            holder.Tier = _tier;
        }
    }

    #endregion

    #region Intro Animation
    /// <summary>
    /// 초기 애니메이션 설정
    /// </summary>
    private void InitAnimation()
    {
        if (_config == null) return;
        GetObject(Objects.MainGroup).transform
            .DOScale(1f, _config.initDuration).From(_config.mainGroupScaleFrom).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.Grass).transform
            .DOScale(1f, _config.initDuration).From(_config.grassScaleFrom).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.FrontStoneCats).transform
            .DOScale(_config.frontCatsScaleTo, _config.initDuration).From(_config.frontCatsScaleFrom).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.LeftLeaf).transform
            .DOScale(1f, _config.initDuration).From(_config.leafScaleFrom).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.RightLeaf).transform
            .DOScale(1f, _config.initDuration).From(_config.leafScaleFrom).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);

        if (_catFaceBlinkRoutine == null)
            _catFaceBlinkRoutine = StartCoroutine(CatFaceBlinkRoutine());
    }

    /// <summary>
    /// 고양이 얼굴 깜빡임
    /// </summary>
    private IEnumerator CatFaceBlinkRoutine()
    {
        if (_config == null) yield break;
        while (true)
        {
            GetObject(Objects.CatFace).SetActive(true);
            yield return new WaitForSeconds(_config.catFaceBlinkOn);
            GetObject(Objects.CatFace).SetActive(false);
            yield return new WaitForSeconds(_config.catFaceBlinkOff);
        }
    }

    #endregion

    #region Scroll & Item Events

    private void OnScrollEnded()
    {
        int focusingId = _circularScrollingList.GetFocusingContentID();
    }

    /// <summary>
    /// 카드 클릭 이벤트
    /// </summary>
    private void OnItemClicked(GachaIntroHolder gachaIntroHolder)
    {
        if (_isProcessingClick || _config == null) return;
        _isProcessingClick = true;

        DelayAction(() => ShowButton(Buttons.SkipButton), _config.skipButtonShowDelay);
        SetScrollingEnabled(false);
        ProcessCardSelection(gachaIntroHolder);
        DelayAction(ShowSmokeAndStartGachaAnimation, _config.smokeShowDelay);
    }

    private void SetScrollingEnabled(bool enabled)
    {
        _circularScrollingList.enabled = enabled;
        var scrollRect = _circularScrollingList.GetComponent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.enabled = enabled;
    }

    private void ProcessCardSelection(GachaIntroHolder selectedHolder)
    {
        if (_config?.cardFadeOutDelays == null || _config.cardFadeOutDelays.Length == 0) return;
        var delays = new List<float>(_config.cardFadeOutDelays);
        foreach (var item in _circularScrollingList.ListBoxes)
        {
            var holder = item.GetComponent<GachaIntroHolder>();
            if (holder == null) continue;

            if (item == selectedHolder)
            {
                selectedHolder.transform.SetAsLastSibling();
                selectedHolder.HideCardName();
            }
            else
            {
                holder.FadeOutImage();
                holder.HideCardName();
                float delay = delays[UnityEngine.Random.Range(0, delays.Count)];
                delays.Remove(delay);
                item.transform
                    .DOLocalMoveY(_config.cardMoveY, _config.cardFadeOutDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetAutoKill(true)
                    .SetDelay(delay)
                    .OnComplete(() => item.gameObject.SetActive(false));
            }
        }
    }

    private void ShowSmokeAndStartGachaAnimation()
    {
        _smokeAnimation.gameObject.SetActive(true);
        DelayAction(() =>
        {
            GetImage(Images.Step1Background).DOFade(0, _config.step1BackgroundFadeDuration).SetAutoKill(true);
            GachaAnimation();
        }, _config.gachaAnimationStartDelay - _config.smokeShowDelay);
    }

    #endregion

    #region Gacha Animation

    public void GachaAnimation()
    {
        DelayAction(PlayShootingStarSequence, _config.shootingStarDelay);
        DelayAction(GoPortal, _config.portalShowDelay);
    }


    /// <summary>
    /// 별똥별 애니메이션
    /// </summary>
    private void PlayShootingStarSequence()
    {
        GetObject(Objects.Sunlight).gameObject.SetActive(true);
        GetObject(Objects.ShootingStar).gameObject.SetActive(true);

        var starsParticle = GetObject(Objects.ShootingStar).GetComponent<ParticleImage>();
        _config.ApplyShootingStarParticleByTier(starsParticle, _tier);

        GetImage(Images.ShootingStarImage).GetComponent<DOTweenAnimation>().DOPlayForward();
        GetObject(Objects.ShootingStarTail).transform.DOScaleY(0, _config.shootingStarTailScaleDuration).SetDelay(_config.shootingStarTailScaleDelay).SetEase(Ease.OutQuart).SetAutoKill(true);

        var starTargetPosition = GetObject(Objects.Bell).transform.position;
        starTargetPosition.y -= 100f;

        GetObject(Objects.ShootingStar).transform
            .DOMove(starTargetPosition, _config.shootingStarMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(OnShootingStarReachedBell);
    }

    /// <summary>
    /// 별똥별 애니메이션 완료 후 벨 좌우 흔들기
    /// </summary>
    private void OnShootingStarReachedBell()
    {
        GetObject(Objects.ShootingStar).GetComponent<ParticleImage>().lifetime = 0;
        ShowObject(Objects.ShootingStarEffect);
        HideImage(Images.ShootingStarImage);

        DelayAction(() =>
        {
            HideObject(Objects.ShootingStar);
            HideObject(Objects.ShootingStarEffect);
        }, _config.shootingStarLifetimeEndDelay);

        PlayBellAndShakeAnimation();
    }

    private void PlayBellAndShakeAnimation()
    {
        var bell = GetObject(Objects.Bell);
        var sequence = DOTween.Sequence().SetTarget(bell);
        sequence.Append(bell.transform.DOLocalRotate(new Vector3(0, 0, -_config.bellRotationAngle), 1f).SetEase(Ease.InOutQuad));
        sequence.Append(bell.transform.DOLocalRotate(new Vector3(0, 0, _config.bellRotationAngle), 1f).SetEase(Ease.InOutQuad));
        sequence.SetLoops(-1, LoopType.Yoyo).SetAutoKill(true);

        GetObject(Objects.MainGroup).transform
            .DOShakePosition(_config.mainGroupShakeDuration, _config.mainGroupShakeStrength, 23, 90, true, true)
            .SetEase(Ease.InQuad)
            .SetAutoKill(true);
    }

    #endregion

    #region Portal & After Portal

    public void GoPortal()
    {
        _step1.gameObject.SetActive(false);
        ShowObject(Objects.Portal);
        GetObject(Objects.Portal).transform
            .DOScale(1f, _config.portalScaleDuration)
            .SetEase(Ease.OutBack)
            .From(0)
            .SetAutoKill(true);

        DelayAction(PlayAfterPortalSequence, _config.afterPortalDelay);
    }

    /// <summary>
    /// 포탈 열기 애니메이션
    /// </summary>
    private void PlayAfterPortalSequence()
    {
        _afterPortalGroup.gameObject.SetActive(true);
        _afterPortalGroup.transform
            .DOScale(1f, _config.afterPortalScaleDuration)
            .SetEase(Ease.InOutQuart)
            .From(0)
            .SetAutoKill(true);

        var starsParticle = GetObject(Objects.AfterPortalEnergySpread).GetComponent<ParticleImage>();
        _config.ApplyAfterPortalParticleByTier(starsParticle, _tier);

        GetObject(Objects.MainGroup).transform.DOScale(_config.mainGroupScaleEnd, _config.portalRiseDuration).SetEase(Ease.Linear).SetAutoKill(true);
        GetObject(Objects.MainGroup).transform.DOMoveY(_config.mainGroupMoveY, _config.portalRiseDuration).SetRelative(true).SetEase(Ease.Linear).SetAutoKill(true);
        GetObject(Objects.Grass).transform.DOScale(_config.grassScaleEnd, _config.portalRiseDuration).SetEase(Ease.Linear).SetAutoKill(true);
        GetObject(Objects.Grass).transform.DOMoveY(_config.grassMoveY, _config.portalRiseDuration).SetRelative(true).SetEase(Ease.Linear).SetAutoKill(true);
        GetObject(Objects.LeftLeaf).transform.DOLocalMove(_config.leftLeafMoveOffset, _config.portalRiseDuration).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.RightLeaf).transform.DOLocalMove(_config.rightLeafMoveOffset, _config.portalRiseDuration).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);

        GetImage(Images.WhiteOverlay).gameObject.SetActive(true);
        GetImage(Images.WhiteOverlay)
            .DOFade(1f, _config.whiteOverlayFadeDuration)
            .SetDelay(_config.whiteOverlayFadeDelay)
            .SetEase(Ease.Linear)
            .SetAutoKill(true);
    }

    #endregion

    #region Button Handlers

    private void OnSkipButtonClicked()
    {
        HideButton(Buttons.SkipButton);
        UIManager.Instance.ClosePopupUI(this);
    }

    #endregion
}
