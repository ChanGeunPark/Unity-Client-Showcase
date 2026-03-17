using System;

using AirFishLab.ScrollingList;
using AssetKits.ParticleImage;
using Coffee.UIExtensions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GachaIntroHolder : ListBox, IPointerClickHandler
{
    /// <summary>셰이더 프로퍼티 이름 (설정과 무관한 고정값)</summary>
    private static class MaterialProps
    {
        public const string GlowGlobal = "_GlowGlobal";
        public const string RoundWaveStrength = "_RoundWaveStrength";
        public const string ChromAberrAmount = "_ChromAberrAmount";
        public const string ShineLocation = "_ShineLocation";
        public const string ShineGlow = "_ShineGlow";
        public const string GradBlend = "_GradBlend";
        public const string HitEffectBlend = "_HitEffectBlend";
    }

    #region Serialized & Public Fields

    public float DelayTime;
    public int Tier;
    public Action<GachaIntroHolder> OnClick { get; set; }

    [Tooltip("연출 설정 (리소스 경로, 타이밍, 티어별 값). 미할당 시 카드 클릭 연출이 동작하지 않음. Data/SO/GachaCardRevealConfig_Default 사용 가능")]
    [SerializeField] private GachaCardRevealConfig _config;
    [SerializeField] private Image _smokeAnimation;
    [SerializeField] private Image _cardImage;
    [SerializeField] private UIParticle _shineEffect;
    [SerializeField] private Image _lineImage;
    [SerializeField] private TextMeshProUGUI _CardName;

    #endregion

    #region Private State

    private Material _cardImageMaterialInstance;
    private bool _isClickable = true;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        if (_smokeAnimation != null)
            _smokeAnimation.gameObject.SetActive(false);

        if (_cardImage != null)
            SetupCardMaterial();

        if (_smokeAnimation != null)
        {
            if (DelayTime > 0f)
                DOTween.Sequence()
                    .AppendInterval(DelayTime)
                    .AppendCallback(() => _smokeAnimation.gameObject.SetActive(true))
                    .SetTarget(this)
                    .SetLink(gameObject);
            else
                _smokeAnimation.gameObject.SetActive(true);
        }

        if (_shineEffect != null)
            _shineEffect.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_cardImageMaterialInstance != null && Application.isPlaying)
            Destroy(_cardImageMaterialInstance);
    }

    #endregion

    #region Setup

    private void SetupCardMaterial()
    {
        _cardImageMaterialInstance = new Material(_cardImage.material);
        _cardImage.material = _cardImageMaterialInstance;
        _cardImage.material.SetFloat(MaterialProps.GlowGlobal, 1f);
        _cardImage.material.SetFloat(MaterialProps.RoundWaveStrength, 0f);
        _cardImage.material.SetFloat(MaterialProps.ChromAberrAmount, 0f);
        _cardImage.material.SetFloat(MaterialProps.ShineLocation, 0f);
        _cardImage.material.SetFloat(MaterialProps.ShineGlow, 0f);
        _cardImage.material.SetFloat(MaterialProps.GradBlend, 0f);
        _cardImage.material.SetFloat(MaterialProps.HitEffectBlend, 0f);
    }

    #endregion

    #region Click Handler

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isClickable) return;
        _isClickable = false;

        if (_cardImage == null || _config == null) return;

        OnClick?.Invoke(this);

        var rainbowHighlight = CreateAndParent(_config.rainbowHighlightName, _cardImage.transform);
        var powerBurst = CreateAndParent(_config.powerBurstName, transform);
        SetLocalIdentity(rainbowHighlight.transform);
        SetLocalIdentity(powerBurst.transform);
        rainbowHighlight.SetActive(false);
        powerBurst.SetActive(false);

        PlayCardRevealTweens();
        AnimateShineEffectWithDOTween();
        ChromAberrAmountDOTween();
        AnimateRoundWaveStrengthDOTween();

        DOTween.Sequence()
            .AppendInterval(_config.shineAndSpreadDelay)
            .AppendCallback(() => PlayShineAndSpread(rainbowHighlight, powerBurst))
            .SetTarget(this)
            .SetLink(gameObject);
        DOTween.Sequence()
            .AppendInterval(_config.glowAnimationDelay)
            .AppendCallback(AnimateGlowDOTween)
            .SetTarget(this)
            .SetLink(gameObject);
    }

    private static GameObject CreateAndParent(string path, Transform parent)
    {
        var prefab = ResourceManager.Instance.Load<GameObject>(path);
        return Instantiate(prefab, parent);
    }

    private static void SetLocalIdentity(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localScale = Vector3.one;
    }

    private void PlayCardRevealTweens()
    {
        transform.DOScale(_config.cardRevealScale, _config.cardScaleDuration)
            .SetEase(Ease.OutQuad)
            .SetAutoKill(true)
            .SetDelay(_config.cardScaleDelay)
            .SetLink(gameObject);
        transform.DOLocalMove(Vector3.zero, _config.cardScaleDuration)
            .SetEase(Ease.OutQuad)
            .SetAutoKill(true)
            .SetDelay(_config.cardScaleDelay)
            .SetLink(gameObject);
        transform.DOLocalRotate(new Vector3(0, 180, _config.cardTiltAngle), _config.cardRotateDuration)
            .SetEase(Ease.Linear)
            .SetLoops(2, LoopType.Yoyo)
            .SetAutoKill(true)
            .SetDelay(_config.cardScaleDelay)
            .SetLink(gameObject);
    }

    /// <summary>
    /// 카드 레인보우 및 버스트 애니메이션 재생
    /// </summary>
    private void PlayShineAndSpread(GameObject rainbowHighlight, GameObject powerBurst)
    {
        if (_shineEffect != null)
            _shineEffect.gameObject.SetActive(true);

        var spread = CreateAndParent(_config.spreadEffectName, transform);
        spread.transform.localPosition = Vector3.zero;

        var lightRays = CreateAndParent(_config.lightRaysName, _cardImage.transform);
        SetLocalIdentity(lightRays.transform);

        float shineScale = 0f;
        DOTween.To(() => shineScale, x => { shineScale = x; _shineEffect.scale = x; }, _config.shineScaleEnd, _config.shineScaleDuration)
            .SetTarget(this)
            .SetLink(gameObject);

        spread.transform.DOScale(Vector3.one, _config.spreadScaleDuration)
            .From(Vector3.zero)
            .SetAutoKill(true)
            .SetLink(spread)
            .OnComplete(() =>
        {
            var starsParticle = spread.transform.Find("Stars").GetComponent<ParticleImage>();
            _config.ApplySpreadParticleByTier(starsParticle, Tier);
            starsParticle.gravityEnabled = true;
        });

        transform.DOShakePosition(_config.shake1Duration, 5, 12, 90, true, false)
            .SetEase(Ease.InQuad)
            .SetAutoKill(true)
            .SetLink(gameObject)
            .OnComplete(() =>
        {
            ShowRainbowAndPowerBurst(rainbowHighlight, powerBurst);
            _cardImage.transform.DOScale(_config.popScale, _config.popDuration)
                .SetAutoKill(true)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(_cardImage.gameObject);
            _lineImage.transform.DOScale(_config.popScale, _config.popDuration)
                .SetAutoKill(true)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(_lineImage.gameObject);
            _shineEffect.transform.DOScale(_config.popScale, _config.popDuration)
                .SetAutoKill(true)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(_shineEffect.gameObject);

            transform.DOShakePosition(_config.shake2Duration, 5, 30, 90, true, false)
                .SetEase(Ease.InQuad)
                .SetAutoKill(true)
                .SetLink(gameObject);

            DOTween.Sequence()
                .AppendInterval(_config.rainbowShowDelay)
                .AppendCallback(() => PlayCardRainbowAndExit(spread))
                .SetTarget(this)
                .SetLink(gameObject);
        });
    }

    private void ShowRainbowAndPowerBurst(GameObject rainbowHighlight, GameObject powerBurst)
    {
        rainbowHighlight.SetActive(true);
        powerBurst.SetActive(true);
        string stateName = _config.GetRainbowAnimStateForTier(Tier);
        rainbowHighlight.GetComponent<Animator>().Play(stateName);
        rainbowHighlight.GetComponent<Image>().DOFade(1f, _config.rainbowFadeDuration)
            .From(0f)
            .SetAutoKill(true)
            .SetLink(rainbowHighlight);
    }

    private void PlayCardRainbowAndExit(GameObject spread)
    {
        OnCardRainbowDOTween();

        _cardImage.transform.DOScale(1f, _config.cardScaleBackDuration)
            .SetAutoKill(true)
            .SetLink(_cardImage.gameObject)
            .OnComplete(() =>
        {
            _cardImage.transform.DOScale(new Vector3(_config.cardSquashX, _config.cardSquashY, 1f), _config.cardSquashDuration)
                .SetEase(Ease.InOutQuad)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(_cardImage.gameObject);
            _lineImage.transform.DOScale(new Vector3(_config.cardSquashX, _config.cardSquashY, 1f), _config.cardSquashDuration)
                .SetEase(Ease.InOutQuad)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(_lineImage.gameObject);

            _cardImage.transform.DOLocalMoveY(_config.cardExitY, _config.cardExitDuration)
                .SetEase(Ease.InBack)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(_cardImage.gameObject);
            _lineImage.transform.DOLocalMoveY(_config.cardExitY, _config.cardExitDuration)
                .SetEase(Ease.InBack)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(_lineImage.gameObject);
            spread.transform.DOLocalMoveY(_config.cardExitY, _config.cardExitDuration)
                .SetEase(Ease.InBack)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(spread);
            _shineEffect.transform.DOLocalMoveY(_config.cardExitY, _config.cardExitDuration)
                .SetEase(Ease.InBack)
                .SetAutoKill(true)
                .SetDelay(_config.cardSquashDelay)
                .SetLink(_shineEffect.gameObject);

            DOTween.Sequence()
                .AppendInterval(_config.starsLoopOffDelay)
                .AppendCallback(() =>
            {
                spread.transform.Find("Stars").GetComponent<ParticleImage>().loop = false;
            });

            DOTween.Sequence()
                .AppendInterval(_config.hideCardDelay)
                .AppendCallback(() =>
            {
                _cardImage.material.SetFloat(MaterialProps.GlowGlobal, 1f);
                _cardImage.material.SetFloat(MaterialProps.GradBlend, 0f);
                _cardImage.gameObject.SetActive(false);
                _lineImage.gameObject.SetActive(false);
            }).SetTarget(this);
        });
    }

    #endregion

    #region Material / DOTween Animation Methods

    private void AnimateGlowDOTween()
    {
        _cardImage.material.SetFloat(MaterialProps.GlowGlobal, _config.glowFrom);
        DOVirtual.Float(_config.glowFrom, _config.glowTo, _config.glowDuration, v => _cardImage.material.SetFloat(MaterialProps.GlowGlobal, v))
            .SetTarget(this)
            .SetLink(gameObject);
    }

    private void AnimateRoundWaveStrengthDOTween()
    {
        _cardImage.material.SetFloat(MaterialProps.RoundWaveStrength, 0f);
        var seq = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
        seq.Append(DOVirtual.Float(0f, _config.roundWavePeak, _config.roundWaveDuration, v => _cardImage.material.SetFloat(MaterialProps.RoundWaveStrength, v)));
        seq.Append(DOVirtual.Float(_config.roundWavePeak, 0f, _config.roundWaveDuration, v => _cardImage.material.SetFloat(MaterialProps.RoundWaveStrength, v)));
        seq.OnComplete(() => _cardImage.material.SetFloat(MaterialProps.RoundWaveStrength, 0f));
    }

    private void AnimateShineEffectWithDOTween()
    {
        _cardImage.material.SetFloat(MaterialProps.ShineLocation, 1f);
        _cardImage.material.SetFloat(MaterialProps.ShineGlow, 1f);
        DOVirtual.Float(0f, 1f, _config.shineLocationDuration, v => _cardImage.material.SetFloat(MaterialProps.ShineLocation, v))
            .SetTarget(this)
            .OnComplete(() =>
            {
                _cardImage.material.SetFloat(MaterialProps.ShineLocation, 0f);
                _cardImage.material.SetFloat(MaterialProps.ShineGlow, 0f);
            });
    }

    private void ChromAberrAmountDOTween()
    {
        _cardImage.material.SetFloat(MaterialProps.ChromAberrAmount, 0f);
        var seq = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
        seq.Append(DOVirtual.Float(0f, 1f, _config.chromAberrDuration, v => _cardImage.material.SetFloat(MaterialProps.ChromAberrAmount, v)));
        seq.Append(DOVirtual.Float(1f, 0f, _config.chromAberrDuration, v => _cardImage.material.SetFloat(MaterialProps.ChromAberrAmount, v)));
        seq.OnComplete(() => _cardImage.material.SetFloat(MaterialProps.ChromAberrAmount, 0f));
    }

    private void OnCardRainbowDOTween()
    {
        _cardImage.material.SetFloat(MaterialProps.GradBlend, 0f);
        _cardImage.material.SetFloat(MaterialProps.HitEffectBlend, 0f);
        float targetGrad = Tier == 1 ? _config.gradBlendLegend : _config.gradBlendNormal;
        if (Tier == 1)
            DOVirtual.Float(0f, targetGrad, _config.gradBlendDuration, v => _cardImage.material.SetFloat(MaterialProps.GradBlend, v))
                .SetTarget(this)
                .SetLink(gameObject);
        else
            DOVirtual.Float(0f, _config.gradBlendNormal, _config.gradBlendDuration, v => _cardImage.material.SetFloat(MaterialProps.HitEffectBlend, v))
                .SetTarget(this)
                .SetLink(gameObject);
    }

    #endregion

    #region Public API

    public void FadeOutImage()
    {
        if (_config == null) return;
        _cardImage.DOFade(_config.fadeOutAlpha, _config.fadeOutDuration)
            .SetAutoKill(true)
            .SetLink(_cardImage.gameObject);
        _lineImage.DOFade(0f, _config.fadeOutDuration)
            .SetAutoKill(true)
            .SetLink(_lineImage.gameObject);
    }

    public void HideCardName()
    {
        if (_config == null) return;
        _CardName.DOFade(0f, _config.fadeOutDuration)
            .SetAutoKill(true)
            .SetLink(_CardName.gameObject);
    }

    #endregion
}
