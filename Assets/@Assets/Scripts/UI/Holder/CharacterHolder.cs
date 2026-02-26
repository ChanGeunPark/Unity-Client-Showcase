using System.Collections.Generic;
using ChocDino.UIFX;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CharacterHolder : BaseUI
{
    private const string StarHolderAddress = "GlobalStarHolder";
    private const string ShowAnimationIdPrefix = "CharacterHolderShowAnimation_";

    private int _index;
    private string _animationId;

    private static readonly IReadOnlyDictionary<CharacterGrade, int> GradeToStarCount = new Dictionary<CharacterGrade, int>
    {
        { CharacterGrade.Common, 1 },
        { CharacterGrade.Rare, 2 },
        { CharacterGrade.Epic, 3 },
        { CharacterGrade.Legendary, 4 },
    };

    enum Images { BgMask, CharacterImage, LockIcon, LevelTitleGroup }
    enum Texts { CharacterName, LevelText, LevelTitle }
    enum Objects { StarGroup }
    enum Sliders { LevelSlider }

    private void Awake()
    {
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindSlider(typeof(Sliders));
        BindObject(typeof(Objects));
        ResetUI();
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (!string.IsNullOrEmpty(_animationId))
            DOTween.Kill(_animationId);
    }
    public void ResetUI()
    {
        GetSlider(Sliders.LevelSlider).value = 0;
        GetSlider(Sliders.LevelSlider).maxValue = 10;
        SetText(Texts.LevelText, "1/10");
        Util.DestroyChilds(GetObject(Objects.StarGroup));
        HideImage(Images.LockIcon);
    }

    // 비동기 로드로 메인 스레드 블로킹 없이 초기화.
    public async UniTaskVoid Initialize(CharacterChart characterChart, CharacterData characterData = null)
    {
        bool isLock = characterData == null;
        CharacterDataSO so = ResourceManager.Instance.LoadSOData<CharacterDataSO>(characterChart.CharacterId);

        SetBasicInfo(characterChart.CharacterId, isLock ? 0 : characterData.Level);
        ApplyLockState(isLock);
        if (!isLock)
            await CreateStarsAsync(characterChart.Grade);
        await SetProfileImageAsync(so.ProfileImage);
    }

    private void SetBasicInfo(string characterName, int level)
    {
        SetText(Texts.CharacterName, characterName);
        SetText(Texts.LevelText, $"{level}/10");
    }

    private void ApplyLockState(bool isLock)
    {
        ColorAdjustFilter colorAdjustFilter = GetImage(Images.CharacterImage).GetComponent<ColorAdjustFilter>();
        if (isLock)
        {
            ShowImage(Images.LockIcon);
            HideObject(Objects.StarGroup);

            GetImage(Images.LevelTitleGroup).GetComponent<ImageController>().SetImageState(ImageController.ImageState.Disabled);
            colorAdjustFilter.Strength = 1;
            GetText(Texts.LevelText).color = new Color(1, 1, 1, 0.5f);
            GetText(Texts.LevelTitle).color = new Color(1, 1, 1, 0.5f);
            return;
        }

        GetImage(Images.LevelTitleGroup).GetComponent<ImageController>().SetImageState(ImageController.ImageState.Normal);
        colorAdjustFilter.Strength = 0;
        HideImage(Images.LockIcon);
        ShowObject(Objects.StarGroup);
        GetText(Texts.LevelText).color = Color.white;
        GetText(Texts.LevelTitle).color = Color.white;
    }

    private async UniTask CreateStarsAsync(CharacterGrade grade)
    {
        if (!GradeToStarCount.TryGetValue(grade, out int count)) return;

        GameObject starGroup = GetObject(Objects.StarGroup);
        Util.DestroyChilds(starGroup);
        Transform parent = starGroup.transform;

        GameObject starPrefab = await ResourceManager.Instance.LoadAsync<GameObject>(StarHolderAddress);
        if (starPrefab == null) return;
        for (int i = 0; i < count; i++)
            Instantiate(starPrefab, parent);
    }

    private async UniTask SetProfileImageAsync(AssetReferenceSprite profileImage)
    {
        // ResourceManager 경유: 캐시·핸들 관리, 같은 키 중복 로드 방지.
        Sprite sprite = await ResourceManager.Instance.LoadSpriteByKeyAsync(profileImage.RuntimeKey);
        if (sprite != null)
            SetImage(Images.CharacterImage, sprite);
    }

    public void ShowAnimation(float delay = 0, int index = 0)
    {
        _index = index;
        _animationId = ShowAnimationIdPrefix + GetInstanceID();
        DOTween.Kill(_animationId);

        Vector3 ShowAnimationRotateFrom = new Vector3(60f, -90f, 0f);
        Vector3 ShowAnimationRotateTo = Vector3.zero;
        float ShowAnimationRotateDuration = 1f;
        Vector3 ShowAnimationScaleFrom = new Vector3(0f, 0f, 1f);
        float ShowAnimationScaleTo = 1f;
        float ShowAnimationScaleDuration = 0.3f;

        var rotateTween = transform.DORotate(ShowAnimationRotateTo, ShowAnimationRotateDuration)
            .SetEase(Ease.OutBack)
            .From(ShowAnimationRotateFrom)
            .SetTarget(transform);
        var scaleTween = transform.DOScale(ShowAnimationScaleTo, ShowAnimationScaleDuration)
            .SetEase(Ease.OutBack)
            .From(ShowAnimationScaleFrom)
            .SetTarget(transform);

        var sequence = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(rotateTween)
            .Join(scaleTween)
            .SetId(_animationId)
            .SetTarget(transform);
    }
}
