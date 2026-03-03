using AssetKits.ParticleImage;
using UnityEngine;

/// <summary>
/// 가챠 팝업 전체 플로우(인트로 → 카드 선택 → 별똥별 → 포탈)의 타이밍·애니메이션 수치를 정의합니다.
/// 인스펙터에서 값만 바꿔서 연출 톤을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "GachaPopupConfig", menuName = "Scriptable Objects/Gacha/GachaPopupConfig")]
public class GachaPopupConfig : ScriptableObject
{
    [Header("Timing - Intro & Step1")]
    public float introDelay = 1f;
    public float step1FadeDuration = 0.5f;
    public float step1BackgroundFadeDuration = 1f;

    [Header("Timing - Card Selection")]
    public float skipButtonShowDelay = 1.5f;
    public float cardFadeOutDuration = 0.4f;
    public float cardMoveY = 1000f;
    public float[] cardFadeOutDelays = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f };

    [Header("Timing - Smoke & Gacha Start")]
    public float smokeShowDelay = 3.4f;
    public float gachaAnimationStartDelay = 3.5f;

    [Header("Timing - Shooting Star")]
    public float shootingStarDelay = 1f;
    public float shootingStarMoveDuration = 1f;
    public float shootingStarLifetimeEndDelay = 2f;
    public float shootingStarTailScaleDuration = 2f;
    public float shootingStarTailScaleDelay = 0.5f;

    [Header("Timing - Portal")]
    public float portalShowDelay = 2.5f;
    public float portalScaleDuration = 0.3f;
    public float afterPortalDelay = 0.5f;
    public float afterPortalScaleDuration = 1f;

    [Header("Timing - White Overlay & Cat Face")]
    public float whiteOverlayFadeDelay = 0.7f;
    public float whiteOverlayFadeDuration = 1.5f;
    public float catFaceBlinkOn = 4f;
    public float catFaceBlinkOff = 1f;

    [Header("Animation - Intro")]
    public float mainGroupScaleFrom = 1.2f;
    public float grassScaleFrom = 0.8f;
    public float frontCatsScaleFrom = 0.8f;
    public float frontCatsScaleTo = 1.1f;
    public float leafScaleFrom = 1.2f;
    public float initDuration = 1f;

    [Header("Animation - Bell & Shake")]
    public float bellRotationAngle = 25f;
    public float mainGroupShakeDuration = 0.5f;
    public float mainGroupShakeStrength = 10f;

    [Header("Animation - Portal Rise (After Portal)")]
    public float portalRiseDuration = 5f;
    public float mainGroupScaleEnd = 2f;
    public float mainGroupMoveY = 600f;
    public float grassScaleEnd = 1.5f;
    public float grassMoveY = -200f;
    public Vector3 leftLeafMoveOffset = new Vector3(-200, 50, 0);
    public Vector3 rightLeafMoveOffset = new Vector3(200, 50, 0);

    [Header("Tier - Shooting Star Particle (SpeedRange min, max)")]
    public int shootingStarParticleTier1Min = 0;
    public int shootingStarParticleTier1Max = 5;
    public int shootingStarParticleTier2_3Speed = 4;
    public int shootingStarParticleDefaultSpeed = 2;

    [Header("Tier - After Portal Particle")]
    public int afterPortalParticleTier1FPS = 3;
    public int afterPortalParticleTier2_3StartFrame = 4;
    public int afterPortalParticleDefaultStartFrame = 2;

    /// <summary>티어에 맞게 별똥별 파티클 textureSheetFrameSpeedRange 적용.</summary>
    public void ApplyShootingStarParticleByTier(ParticleImage particle, int tier)
    {
        if (particle == null) return;
        if (tier == 1)
            particle.textureSheetFrameSpeedRange = new SpeedRange(shootingStarParticleTier1Min, shootingStarParticleTier1Max);
        else if (tier == 2 || tier == 3)
            particle.textureSheetFrameSpeedRange = new SpeedRange(shootingStarParticleTier2_3Speed, shootingStarParticleTier2_3Speed);
        else
            particle.textureSheetFrameSpeedRange = new SpeedRange(shootingStarParticleDefaultSpeed, shootingStarParticleDefaultSpeed);
    }

    /// <summary>티어에 맞게 포탈 후 스프레드 파티클 적용.</summary>
    public void ApplyAfterPortalParticleByTier(ParticleImage particle, int tier)
    {
        if (particle == null) return;
        if (tier == 1)
            particle.textureSheetFPS = afterPortalParticleTier1FPS;
        else if (tier == 2 || tier == 3)
            particle.textureSheetStartFrame = afterPortalParticleTier2_3StartFrame;
        else
            particle.textureSheetStartFrame = afterPortalParticleDefaultStartFrame;
    }
}
