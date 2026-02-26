using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CharacterHolder : BaseUI
{
    private const string StarHolderAddress = "GlobalStarHolder";

    private static readonly IReadOnlyDictionary<CharacterGrade, int> GradeToStarCount = new Dictionary<CharacterGrade, int>
    {
        { CharacterGrade.Common, 1 },
        { CharacterGrade.Rare, 2 },
        { CharacterGrade.Epic, 3 },
        { CharacterGrade.Legendary, 4 },
    };

    enum Images { BgMask, CharacterImage, LockIcon }
    enum Texts { CharacterName, LevelText }
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

        SetBasicInfo(so.CharacterName, isLock ? 0 : characterData.Level);
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
        if (isLock)
        {
            ShowImage(Images.LockIcon);
            HideObject(Objects.StarGroup);
            return;
        }
        HideImage(Images.LockIcon);
        ShowObject(Objects.StarGroup);
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
}
