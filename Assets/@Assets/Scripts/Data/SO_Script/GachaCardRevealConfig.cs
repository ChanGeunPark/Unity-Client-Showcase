using AssetKits.ParticleImage;
using UnityEngine;

/// <summary>
/// 가챠 카드 오픈 연출에 쓰이는 리소스 경로, 타이밍, 애니메이션 수치를 정의합니다.
/// 인스펙터에서 값만 바꿔서 연출 톤을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "GachaCardRevealConfig", menuName = "Scriptable Objects/Gacha/GachaCardRevealConfig")]
public class GachaCardRevealConfig : ScriptableObject
{
    [Header("Resources - Prefab Name")]
    [Tooltip("레인보우 하이라이트 프리팹 이름 (ResourceManager.Load)")]
    public string rainbowHighlightName = "RectangleRainbowHighlight";
    [Tooltip("종에 맞는 버스트 이펙트 프리팹 이름")]
    public string powerBurstName = "GachaPowerBurst";
    [Tooltip("스프레드 이펙트 프리팹 이름")]
    public string spreadEffectName = "GachaSpreadEffect";
    [Tooltip("라이트레이 이펙트 프리팹 이름")]
    public string lightRaysName = "GachaLightRays";

    [Header("Resources - Animator State Names (by Tier)")]
    [Tooltip("티어 1 (레전드) 애니메이터 스테이트 이름")]
    public string rainbowAnimStateLegend = "RectangleRainbowAniLegend";
    [Tooltip("티어 2~3 (에픽/레어) 애니메이터 스테이트 이름")]
    public string rainbowAnimStateGood = "RectangleRainbowAniGood";
    [Tooltip("그 외 등급 애니메이터 스테이트 이름")]
    public string rainbowAnimStateNormal = "RectangleRainbowAniNormal";

    [Header("Timing")]
    public float cardScaleDelay = 0.4f;
    public float cardScaleDuration = 0.6f;
    public float cardRotateDuration = 0.3f;
    public float shineAndSpreadDelay = 1f;
    public float glowAnimationDelay = 2.5f;
    public float spreadScaleDuration = 1.5f;
    public float shake1Duration = 1f;
    public float shake2Duration = 1.5f;
    public float rainbowShowDelay = 0.4f;
    public float cardScaleBackDuration = 0.5f;
    public float cardSquashDuration = 0.1f;
    public float cardSquashDelay = 0.4f;
    public float cardExitY = 1000f;
    public float cardExitDuration = 0.2f;
    public float starsLoopOffDelay = 0.2f;
    public float hideCardDelay = 0.6f;

    [Header("Animation - Card Reveal")]
    public float cardRevealScale = 1.3f;
    public float cardTiltAngle = 10f;
    public float shineScaleEnd = 10f;
    public float shineScaleDuration = 0.5f;
    public float popScale = 1.2f;
    public float popDuration = 0.1f;
    public float cardSquashX = 0.73f;
    public float cardSquashY = 1.09f;

    [Header("Animation - Material (Glow, Shine, ChromAberr, RoundWave)")]
    public float glowFrom = 1f;
    public float glowTo = 5.3f;
    public float glowDuration = 3f;
    public float roundWavePeak = 0.1f;
    public float roundWaveDuration = 0.2f;
    public float chromAberrDuration = 0.2f;
    public float shineLocationDuration = 0.3f;
    public float rainbowFadeDuration = 0.1f;

    [Header("Animation - Tier Material (GradBlend / HitEffect)")]
    [Tooltip("티어 1 레전드 그라데이션 블렌드 최종값")]
    public float gradBlendLegend = 0.333f;
    [Tooltip("일반 등급 HitEffect 블렌드 값")]
    public float gradBlendNormal = 0.1f;
    public float gradBlendDuration = 0.5f;

    [Header("Animation - Fade Out (Unselected Cards)")]
    public float fadeOutAlpha = 0.4f;
    public float fadeOutDuration = 0.3f;

    [Header("Tier - Spread Particle (ParticleImage)")]
    [Tooltip("티어 1일 때 textureSheetFPS")]
    public int spreadParticleFPSLegend = 3;
    [Tooltip("티어 2~3일 때 textureSheetStartFrame")]
    public int spreadParticleStartFrameGood = 4;
    [Tooltip("그 외 textureSheetStartFrame")]
    public int spreadParticleStartFrameNormal = 2;

    /// <summary>
    /// 티어(1=레전드, 2=에픽, 3=레어 등)에 맞는 레인보우 애니메이터 스테이트 이름 반환.
    /// </summary>
    public string GetRainbowAnimStateForTier(int tier)
    {
        return tier == 1 ? rainbowAnimStateLegend
             : tier == 2 || tier == 3 ? rainbowAnimStateGood
             : rainbowAnimStateNormal;
    }

    /// <summary>
    /// 티어에 맞게 ParticleImage 스프레드 파티클 설정 적용.
    /// </summary>
    public void ApplySpreadParticleByTier(ParticleImage particle, int tier)
    {
        if (particle == null) return;
        if (tier == 1)
            particle.textureSheetFPS = spreadParticleFPSLegend;
        else if (tier == 2 || tier == 3)
            particle.textureSheetStartFrame = spreadParticleStartFrameGood;
        else
            particle.textureSheetStartFrame = spreadParticleStartFrameNormal;
    }
}
