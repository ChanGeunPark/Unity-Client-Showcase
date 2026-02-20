using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Image))]
public class ImageController : MonoBehaviour
{
    public enum ImageState
    {
        Normal,
        Active,
        Disabled,
        Selected
    }

    [Header("Mode")]
    [Tooltip("Use custom sprite list (key/sprite) instead of Normal/Active/Disabled/Selected.")]
    [SerializeField] private bool isCustom = false;

    [System.Serializable]
    private class CustomSpriteEntry
    {
        public string key;
        public Sprite sprite;
        public List<LocaleSpriteEntry> localeSprites = new List<LocaleSpriteEntry>();
    }

    [SerializeField]
    [Tooltip("Custom sprites (shown when Use Custom Sprite is on).")]
    private List<CustomSpriteEntry> customSprites = new List<CustomSpriteEntry>();

    private Dictionary<string, Sprite> customSpriteMap;
    private Dictionary<string, CustomSpriteEntry> customSpriteEntryMap;

    [System.Serializable]
    private class LocaleSpriteEntry
    {
        public LocaleType locale = LocaleType.None;
        public Sprite sprite;
    }

    [Header("Image Sprites (when not using custom)")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite disabledSprite;
    [SerializeField] private Sprite selectedSprite;

    [Header("Locale Overrides (when Locale Dependent is on and not custom)")]
    [SerializeField] private List<LocaleSpriteEntry> normalLocaleSprites = new List<LocaleSpriteEntry>();
    [SerializeField] private List<LocaleSpriteEntry> activeLocaleSprites = new List<LocaleSpriteEntry>();
    [SerializeField] private List<LocaleSpriteEntry> disabledLocaleSprites = new List<LocaleSpriteEntry>();
    [SerializeField] private List<LocaleSpriteEntry> selectedLocaleSprites = new List<LocaleSpriteEntry>();

    [Header("Current State")]
    [SerializeField] private ImageState currentState = ImageState.Normal;
    [SerializeField]
    [Tooltip("In custom mode: key to display. Use the dropdown in inspector or SetImageState(string).")]
    private string currentCustomKey;

    private Image targetImage;

    public enum LocaleType
    {
        None,
        KR,
        EN,
        JP,
        CN,
    }

    public LocaleType localeType = LocaleType.None;
    [SerializeField] private bool isLocaleDependent = false;

    private void Awake()
    {
        localeType = LocaleType.KR;
        Initialize();
        if (!IsValid())
        {
            enabled = false;
            return;
        }

        if (isCustom)
            ApplyCustomBySelection();
        else
            SetImageState(currentState);
    }

    private void Initialize()
    {
        targetImage = GetComponent<Image>();
        BuildCustomSpriteMap();

        if (!isCustom && normalSprite == null && targetImage != null && targetImage.sprite != null)
            normalSprite = targetImage.sprite;
    }

    private bool IsValid()
    {
        if (targetImage == null)
        {
            Debug.LogError($"ImageController on {gameObject.name}: Image component is missing!");
            return false;
        }

        if (isCustom)
        {
            if (customSpriteMap == null || customSpriteMap.Count == 0)
            {
                Debug.LogError($"ImageController on {gameObject.name}: At least one custom sprite is required in custom mode!");
                return false;
            }
        }
        else
        {
            if (normalSprite == null && !(isLocaleDependent && HasLocaleSprite(normalLocaleSprites)))
            {
                Debug.LogError($"ImageController on {gameObject.name}: Normal sprite (or locale-specific override) is required!");
                return false;
            }
        }

        return true;
    }

    public void SetImageState(ImageState state)
    {
        if (!IsValid()) return;

        currentState = state;

        if (isCustom)
        {
            Debug.LogWarning($"ImageController on {gameObject.name}: SetImageState(ImageState) ignored in custom mode. Use SetImageState(string key) instead.");
            return;
        }

        Sprite spriteToUse = state switch
        {
            ImageState.Normal => normalSprite,
            ImageState.Active => activeSprite ?? normalSprite,
            ImageState.Disabled => disabledSprite ?? normalSprite,
            ImageState.Selected => selectedSprite ?? normalSprite,
            _ => normalSprite
        };

        spriteToUse = ApplyLocaleOverride(spriteToUse, GetLocaleOverridesForState(state));
        if (spriteToUse == null)
        {
            Debug.LogError($"ImageController on {gameObject.name}: No sprite available for state '{state}' with locale '{localeType}'.");
            return;
        }

        targetImage.sprite = spriteToUse;
    }

    public bool SetImageState(string customKey)
    {
        if (!IsValid()) return false;
        if (!isCustom)
        {
            Debug.LogWarning($"ImageController on {gameObject.name}: SetImageState(string) used while not in custom mode.");
            return false;
        }

        if (string.IsNullOrEmpty(customKey))
        {
            Debug.LogError($"ImageController on {gameObject.name}: customKey is null or empty.");
            return false;
        }

        if (TrySetCustomSprite(customKey))
        {
            currentCustomKey = customKey;
            return true;
        }

        Debug.LogError($"ImageController on {gameObject.name}: No custom sprite found for key '{customKey}' (locale '{localeType}').");
        return false;
    }

    public ImageState GetCurrentState() => currentState;

    public void SetNormalSprite(Sprite sprite)
    {
        normalSprite = sprite;
        if (currentState == ImageState.Normal)
            SetImageState(ImageState.Normal);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        BuildCustomSpriteMap();
        if (!isCustom && normalSprite == null && targetImage != null && targetImage.sprite != null)
            normalSprite = targetImage.sprite;

        if (isCustom && customSpriteMap != null && customSpriteMap.Count > 0)
            ApplyCustomBySelection();
        else if (!isCustom)
            SetImageState(currentState);

        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
    }
#endif

    private void BuildCustomSpriteMap()
    {
        if (!isCustom)
        {
            customSpriteMap = null;
            customSpriteEntryMap = null;
            return;
        }

        customSpriteMap ??= new Dictionary<string, Sprite>();
        customSpriteMap.Clear();
        customSpriteEntryMap ??= new Dictionary<string, CustomSpriteEntry>();
        customSpriteEntryMap.Clear();

        if (customSprites == null) return;

        for (int i = 0; i < customSprites.Count; i++)
        {
            var entry = customSprites[i];
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            if (entry.sprite == null && !HasLocaleSprite(entry.localeSprites)) continue;
            customSpriteMap[entry.key] = entry.sprite;
            customSpriteEntryMap[entry.key] = entry;
        }
    }

    private void ApplyCustomBySelection()
    {
        if (customSpriteMap == null || customSpriteMap.Count == 0) return;

        if (!string.IsNullOrEmpty(currentCustomKey) && TrySetCustomSprite(currentCustomKey))
            return;

        for (int i = 0; i < (customSprites?.Count ?? 0); i++)
        {
            var entry = customSprites[i];
            if (entry != null && !string.IsNullOrEmpty(entry.key) && TrySetCustomSprite(entry.key))
            {
                currentCustomKey = entry.key;
                return;
            }
        }
    }

    private bool TrySetCustomSprite(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        Sprite sprite = null;
        if (customSpriteEntryMap != null && customSpriteEntryMap.TryGetValue(key, out var entry))
            sprite = ResolveCustomSprite(entry);
        else if (customSpriteMap != null && customSpriteMap.TryGetValue(key, out var fallback))
            sprite = ApplyLocaleOverride(fallback, null);

        if (sprite == null) return false;

        targetImage.sprite = sprite;
        return true;
    }

    private Sprite ResolveCustomSprite(CustomSpriteEntry entry)
    {
        return entry == null ? null : ApplyLocaleOverride(entry.sprite, entry.localeSprites);
    }

    private Sprite ApplyLocaleOverride(Sprite baseSprite, List<LocaleSpriteEntry> overrides)
    {
        if (!isLocaleDependent || overrides == null || overrides.Count == 0)
            return baseSprite;

        if (TryGetLocaleSprite(overrides, localeType, out var localizedSprite))
            return localizedSprite;
        if (localeType != LocaleType.None && TryGetLocaleSprite(overrides, LocaleType.None, out localizedSprite))
            return localizedSprite;

        return baseSprite;
    }

    private bool TryGetLocaleSprite(List<LocaleSpriteEntry> overrides, LocaleType locale, out Sprite sprite)
    {
        sprite = null;
        if (overrides == null) return false;

        for (int i = 0; i < overrides.Count; i++)
        {
            var entry = overrides[i];
            if (entry == null || entry.sprite == null || entry.locale != locale) continue;
            sprite = entry.sprite;
            return true;
        }
        return false;
    }

    private List<LocaleSpriteEntry> GetLocaleOverridesForState(ImageState state)
    {
        return state switch
        {
            ImageState.Active => activeLocaleSprites,
            ImageState.Disabled => disabledLocaleSprites,
            ImageState.Selected => selectedLocaleSprites,
            _ => normalLocaleSprites
        };
    }

    private bool HasLocaleSprite(List<LocaleSpriteEntry> overrides)
    {
        if (overrides == null) return false;
        for (int i = 0; i < overrides.Count; i++)
        {
            var entry = overrides[i];
            if (entry != null && entry.sprite != null) return true;
        }
        return false;
    }
}
