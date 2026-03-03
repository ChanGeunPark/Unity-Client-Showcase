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
    public float DelayTime = 0;
    [SerializeField] private Image _smokeAnimation;
    [SerializeField] private Image _cardImage;
    [SerializeField] private UIParticle _shineEffect;
    [SerializeField] private Image _lineImage;
    [SerializeField] private TextMeshProUGUI _CardName;

    private Material _cardImageMaterialInstance;
    private bool _isClickable = true;

    public int Tier;

    public Action<GachaIntroHolder> OnClick { get; set; }

    void Start()
    {
        if (_smokeAnimation != null)
        {
            _smokeAnimation.gameObject.SetActive(false);
        }


        if (_cardImage != null)
        {
            // 공용 머티리얼과 분리하기 위해 이 카드 전용 인스턴스 생성
            _cardImageMaterialInstance = new Material(_cardImage.material);
            _cardImage.material = _cardImageMaterialInstance;
            _cardImage.material.SetFloat("_GlowGlobal", 1f);
            _cardImage.material.SetFloat("_RoundWaveStrength", 0f);
            _cardImage.material.SetFloat("_ChromAberrAmount", 0f);
            _cardImage.material.SetFloat("_ShineLocation", 0f);
            _cardImage.material.SetFloat("_ShineGlow", 0f);
            _cardImage.material.SetFloat("_GradBlend", 0f);
            _cardImage.material.SetFloat("_HitEffectBlend", 0f);
        }

        if (DelayTime > 0f && _smokeAnimation != null)
            DOTween.Sequence().AppendInterval(DelayTime).AppendCallback(() => _smokeAnimation.gameObject.SetActive(true)).SetTarget(this);
        else if (_smokeAnimation != null)
            _smokeAnimation.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_cardImageMaterialInstance != null && Application.isPlaying)
            Destroy(_cardImageMaterialInstance);
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if (!_isClickable)
        {
            return;
        }

        _isClickable = false;

        if (_cardImage != null)
        {
            OnClick?.Invoke(this);






            GameObject ractangleRainbowHighlight = ResourceManager.Instance.Load<GameObject>("RactangleRainbowHighlight");
            GameObject rainbowHighlight = Instantiate(ractangleRainbowHighlight, _cardImage.transform);



            rainbowHighlight.transform.localPosition = Vector3.zero;
            rainbowHighlight.transform.localScale = Vector3.one;





            GameObject powerBurstV8 = ResourceManager.Instance.Load<GameObject>("GachaPowerBurst");
            GameObject powerBurst = Instantiate(powerBurstV8, this.transform);
            powerBurst.transform.localPosition = Vector3.zero;
            powerBurst.transform.localScale = Vector3.one;

            rainbowHighlight.SetActive(false);
            powerBurst.SetActive(false);


            transform.DOScale(1.3f, 0.6f).SetEase(Ease.OutQuad).SetAutoKill(true).SetDelay(0.4f);
            transform.DOLocalMove(new Vector3(0, 0, 0), 0.6f).SetEase(Ease.OutQuad).SetAutoKill(true).SetDelay(0.4f);
            transform.DOLocalRotate(new Vector3(0, 180, 10), 0.3f).SetEase(Ease.Linear).SetLoops(2, LoopType.Yoyo).SetAutoKill(true).SetDelay(0.4f);




            AnimateShineEffectWithDOTween();
            ChromAberrAmountDOTween();
            AnimateRoundWaveStrengthDOTween();

            void ShineAndSpreadCallback()
            {
                GameObject energySpread = ResourceManager.Instance.Load<GameObject>("GachaSpreadEffect");
                GameObject spread = Instantiate(energySpread, this.transform);
                spread.transform.localPosition = Vector3.zero;


                GameObject seowonLightRays = ResourceManager.Instance.Load<GameObject>("GachaLightRays");
                GameObject lightRays = Instantiate(seowonLightRays, _cardImage.transform);
                lightRays.transform.localPosition = Vector3.zero;
                lightRays.transform.localScale = Vector3.one;


                float shineScale = 0f;
                DOTween.To(() => shineScale, x => { shineScale = x; _shineEffect.scale = x; }, 10f, 0.5f).SetTarget(this);



                spread.transform.DOScale(Vector3.one, 1.5f).From(Vector3.zero).SetAutoKill(true).OnComplete(() =>
                 {
                     ParticleImage starsParticle = spread.transform.Find("Stars").GetComponent<ParticleImage>();
                     if (Tier == 1)
                     {
                         starsParticle.textureSheetFPS = 3;
                     }
                     else if (Tier == 2 || Tier == 3)
                     {
                         starsParticle.textureSheetStartFrame = 4;
                     }
                     else
                     {
                         starsParticle.textureSheetStartFrame = 2;
                     }

                     starsParticle.gravityEnabled = true;
                 });

                //1차
                this.transform.DOShakePosition(1f, 5, 12, 90, true, false).SetEase(Ease.InQuad).SetAutoKill(true).OnComplete(() =>
                {

                    /// === 레전드 등급 효과 추가 ===
                    rainbowHighlight.SetActive(true);
                    powerBurst.SetActive(true);
                    if (Tier == 1)
                    {
                        rainbowHighlight.GetComponent<Animator>().Play("RactangleRainbowAniLegend");
                    }
                    else if (Tier == 2 || Tier == 3)
                    {
                        rainbowHighlight.GetComponent<Animator>().Play("RactangleRainbowAniGood");
                    }
                    else
                    {
                        rainbowHighlight.GetComponent<Animator>().Play("RactangleRainbowAniNormal");
                    }
                    rainbowHighlight.GetComponent<Image>().DOFade(1f, 0.1f).From(0f).SetAutoKill(true);
                    /// === 레전드 등급 효과 추가 ===

                    _cardImage.transform.DOScale(1.2f, 0.1f).SetAutoKill(true).SetLoops(2, LoopType.Yoyo);
                    _lineImage.transform.DOScale(1.2f, 0.1f).SetAutoKill(true).SetLoops(2, LoopType.Yoyo);
                    // spread.transform.DOScale(1.2f, 0.2f).SetAutoKill(true).SetLoops(2, LoopType.Yoyo);
                    _shineEffect.transform.DOScale(1.2f, 0.1f).SetAutoKill(true).SetLoops(2, LoopType.Yoyo);

                    //2차 (여기서 카드 등급에 따라 추가 효과 적용)
                    this.transform.DOShakePosition(1.5f, 5, 30, 90, true, false).SetEase(Ease.InQuad).SetAutoKill(true);

                    DOTween.Sequence().AppendInterval(0.4f).AppendCallback(() =>
                    {
                        OnCardRainbowDOTween();
                        // _cardImage.transform.DOLocalMoveY(-100, 0.4f).SetEase(Ease.Linear).SetAutoKill(true);
                        // _lineImage.transform.DOLocalMoveY(-100, 0.4f).SetEase(Ease.Linear).SetAutoKill(true);
                        // spread.transform.DOLocalMoveY(-100, 0.4f).SetEase(Ease.Linear).SetAutoKill(true);
                        // _shineEffect.transform.DOLocalMoveY(-100, 0.4f).SetEase(Ease.Linear).SetAutoKill(true);
                        // _lineImage.transform.DOScale(new Vector3(1.8f, 0.4f, 1), 0.8f).SetAutoKill(true);




                        _cardImage.transform.DOScale(1, 0.5f).SetAutoKill(true).OnComplete(() =>
                        {
                            _cardImage.transform.DOScale(new Vector3(0.73f, 1.09f, 1), 0.1f).SetEase(Ease.InOutQuad).SetAutoKill(true).SetDelay(0.4f);
                            _lineImage.transform.DOScale(new Vector3(0.73f, 1.09f, 1), 0.1f).SetEase(Ease.InOutQuad).SetAutoKill(true).SetDelay(0.4f);

                            // 위로 올라가기 
                            _cardImage.transform.DOLocalMoveY(1000, 0.2f).SetEase(Ease.InBack).SetAutoKill(true).SetDelay(0.4f);
                            _lineImage.transform.DOLocalMoveY(1000, 0.2f).SetEase(Ease.InBack).SetAutoKill(true).SetDelay(0.4f);
                            spread.transform.DOLocalMoveY(1000, 0.2f).SetEase(Ease.InBack).SetAutoKill(true).SetDelay(0.4f);
                            _shineEffect.transform.DOLocalMoveY(1000, 0.2f).SetEase(Ease.InBack).SetAutoKill(true).SetDelay(0.4f);

                            DOTween.Sequence().AppendInterval(0.2f).AppendCallback(() =>
                            {
                                spread.transform.Find("Stars").GetComponent<ParticleImage>().loop = false;

                            });

                            DOTween.Sequence().AppendInterval(0.6f).AppendCallback(() =>
                            {
                                _cardImage.material.SetFloat("_GlowGlobal", 1f);
                                _cardImage.material.SetFloat("_GradBlend", 0f);

                                _cardImage.gameObject.SetActive(false);
                                _lineImage.gameObject.SetActive(false);
                            }).SetTarget(this);
                        });
                    }).SetTarget(this);





                });


                // Change the pivot to center bottom
                // RectTransform rectTransform = this.GetComponent<RectTransform>();
                // if (rectTransform != null)
                // {
                //     Vector2 currentPivot = rectTransform.pivot;
                //     Vector2 newPivot = new Vector2(0.5f, 0f); // Center bottom pivot
                //     Vector2 size = rectTransform.rect.size;
                //     Vector2 deltaPosition = (newPivot - currentPivot) * size;
                //     rectTransform.pivot = newPivot;
                //     rectTransform.anchoredPosition += deltaPosition;
                // }

            }
            DOTween.Sequence().AppendInterval(1f).AppendCallback(ShineAndSpreadCallback).SetTarget(this);
            DOTween.Sequence().AppendInterval(2.5f).AppendCallback(() => AnimateGlowDOTween()).SetTarget(this);

            //             DOTween.Sequence().AppendInterval(5f).AppendCallback(() => { }).SetTarget(this);
        }

    }

    private void AnimateGlowDOTween()
    {
        _cardImage.material.SetFloat("_GlowGlobal", 1f);
        DOVirtual.Float(1f, 5.3f, 3f, v => _cardImage.material.SetFloat("_GlowGlobal", v)).SetTarget(this);
    }

    private void AnimateRoundWaveStrengthDOTween()
    {
        _cardImage.material.SetFloat("_RoundWaveStrength", 0f);
        var seq = DOTween.Sequence().SetTarget(this);
        seq.Append(DOVirtual.Float(0f, 0.1f, 0.2f, v => _cardImage.material.SetFloat("_RoundWaveStrength", v)));
        seq.Append(DOVirtual.Float(0.1f, 0f, 0.2f, v => _cardImage.material.SetFloat("_RoundWaveStrength", v)));
        seq.OnComplete(() => _cardImage.material.SetFloat("_RoundWaveStrength", 0f));
    }

    private void AnimateShineEffectWithDOTween()
    {
        _cardImage.material.SetFloat("_ShineLocation", 1f);
        _cardImage.material.SetFloat("_ShineGlow", 1f);
        DOVirtual.Float(0f, 1f, 0.3f, v => _cardImage.material.SetFloat("_ShineLocation", v))
            .SetTarget(this)
            .OnComplete(() =>
            {
                _cardImage.material.SetFloat("_ShineLocation", 0f);
                _cardImage.material.SetFloat("_ShineGlow", 0f);
            });
    }

    private void ChromAberrAmountDOTween()
    {
        _cardImage.material.SetFloat("_ChromAberrAmount", 0f);
        var seq = DOTween.Sequence().SetTarget(this);
        seq.Append(DOVirtual.Float(0f, 1f, 0.2f, v => _cardImage.material.SetFloat("_ChromAberrAmount", v)));
        seq.Append(DOVirtual.Float(1f, 0f, 0.2f, v => _cardImage.material.SetFloat("_ChromAberrAmount", v)));
        seq.OnComplete(() => _cardImage.material.SetFloat("_ChromAberrAmount", 0f));
    }


    public void FadeOutImage()
    {
        _cardImage.DOFade(0.4f, 0.3f).SetAutoKill(true);
        _lineImage.DOFade(0f, 0.3f).SetAutoKill(true);
    }

    public void OnCardRainbowDOTween()
    {
        _cardImage.material.SetFloat("_GradBlend", 0f);
        _cardImage.material.SetFloat("_HitEffectBlend", 0f);

        if (Tier == 1)
            DOVirtual.Float(0f, 0.333f, 0.5f, v => _cardImage.material.SetFloat("_GradBlend", v)).SetTarget(this);
        else
            DOVirtual.Float(0f, 0.1f, 0.5f, v => _cardImage.material.SetFloat("_HitEffectBlend", v)).SetTarget(this);
    }


    public void HideCardName()
    {
        _CardName.DOFade(0f, 0.3f).SetAutoKill(true);
    }


}
