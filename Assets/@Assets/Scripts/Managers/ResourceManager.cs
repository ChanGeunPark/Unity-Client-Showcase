using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.U2D;

using Object = UnityEngine.Object;

/// <summary>
/// Unity Addressables 기반 리소스 관리 시스템
/// - 캐싱을 통한 중복 로드 방지
/// - 비동기 병렬 로딩 지원
/// - 메모리 관리 및 선택적 언로드
/// - Thread-Safe 구현
/// </summary>
public class ResourceManager : MonoBehaviour
{
    #region Singleton & Constants
    public static ResourceManager Instance { get; private set; }

    // 네이밍 규칙 상수
    private const string PREFIX_CLIENT_DATA = "ClientData_";
    private const string PRELOAD_LABEL = "PreLoad";

    // 재시도 설정
    private const int MAX_RETRIES = 3;
    private const int RETRY_DELAY_MS = 100;

    #endregion

    #region Cache & State Management

    // Thread-Safe 리소스 캐시
    private readonly ConcurrentDictionary<string, Object> _resources = new();
    private readonly ConcurrentDictionary<string, AsyncOperationHandle> _resourceHandles = new();
    private readonly ConcurrentDictionary<string, bool> _preloadAssetKeys = new();
    private readonly ConcurrentDictionary<string, bool> _loadedLabels = new();
    /// <summary>레이블별 로드된 에셋 키 (UnloadExceptLabel에서 유지할 리소스 판단용)</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _labelToAssetKeys = new();
    /// <summary>동일 레이블 중복 로드 방지: 레이블별 진행 중인 로드 태스크</summary>
    private readonly ConcurrentDictionary<string, UniTask> _inFlightLabelTasks = new();
    private readonly object _labelLoadLock = new object();

    private bool _isPreloadCached = false;
    private readonly object _preloadLock = new object();

    #endregion

    #region Lifecycle & Initialization

    /// <summary>
    /// Singleton 초기화 및 씬 간 유지 설정
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            ReleaseAllHandles();
        }
    }

    #endregion

    #region Handle Management

    /// <summary>
    /// Addressable 핸들 해제 (메모리 누수 방지)
    /// </summary>
    private void ReleaseHandles(Func<string, bool> predicate = null)
    {
        if (_resourceHandles.IsEmpty) return;

        foreach (var kvp in _resourceHandles.ToArray())
        {
            if (predicate != null && !predicate(kvp.Key)) continue;

            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
            _resourceHandles.TryRemove(kvp.Key, out _);
        }
    }

    /// <summary>
    /// 모든 핸들 해제
    /// </summary>
    private void ReleaseAllHandles()
    {
        foreach (var handle in _resourceHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _resourceHandles.Clear();
    }

    #endregion


    #region Resource Loading

    /// <summary>
    /// 캐시에서 리소스 조회
    /// </summary>
    private T LoadFromCache<T>(string key) where T : Object
    {
        if (_resources.TryGetValue(key, out Object resource))
        {
            return resource as T;
        }
        return null;
    }

    /// <summary>
    /// 동기 리소스 로드 (가능하면 비동기 사용 권장)
    /// </summary>
    [Obsolete("Use LoadAsync instead to avoid blocking the main thread")]
    public T Load<T>(string key) where T : Object
    {
        T cachedResource = LoadFromCache<T>(key);
        if (cachedResource)
        {
            return cachedResource;
        }

        T loadedResource = LoadByName<T>(key);
        if (loadedResource)
        {
            return loadedResource;
        }

        return null;
    }

    /// <summary>
    /// 비동기 리소스 로드 (캐시 우선)
    /// </summary>
    public async UniTask<T> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : Object
    {
        T cachedResource = LoadFromCache<T>(key);
        if (cachedResource)
        {
            return cachedResource;
        }

        T loadedResource = await LoadByNameAsync<T>(key, cancellationToken);
        return loadedResource;
    }

    /// <summary>
    /// GameObject 인스턴스화 (프리팹 로드 후 생성)
    /// </summary>
    public GameObject Instantiate(string key, Transform parent = null)
    {
        try
        {
            GameObject prefab = Load<GameObject>(key);

            if (!prefab)
            {
                Debug.LogError($"[ResourceManager] Failed to load prefab: {key}");
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            return go;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Instantiate ERROR: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// GameObject 파괴
    /// </summary>
    public void Destroy(GameObject go)
    {
        if (!go) return;
        Object.Destroy(go);
    }

    #endregion

    #region ClientData & QuestData Loading

    /// <summary>
    /// ClientData 로드 (네이밍 규칙 자동 적용)
    /// </summary>
    public T LoadClientData<T>(string mainKey, string subKey = null) where T : ScriptableObject
    {
        string loadKey = BuildClientDataKey(mainKey, subKey);
        T clientData = Load<T>(loadKey);

        if (clientData) return clientData;

        Debug.LogError($"[ResourceManager] Failed to Load ClientData<{typeof(T)}> : {mainKey}");
        return null;
    }

    /// <summary>
    /// ClientData 키 생성 헬퍼
    /// </summary>
    private string BuildClientDataKey(string mainKey, string subKey)
    {
        return string.IsNullOrEmpty(subKey)
            ? $"{PREFIX_CLIENT_DATA}{mainKey}"
            : $"{PREFIX_CLIENT_DATA}{subKey}_{mainKey}";
    }


    /// <summary>
    /// 여러 ClientData를 병렬로 로드 (Thread-Safe, 캐시 우선)
    /// </summary>
    public async UniTask<Dictionary<(string mainKey, string subKey), T>> BulkLoadClientDataAsync<T>(
        HashSet<(string mainKey, string subKey)> keys,
        CancellationToken cancellationToken = default
    ) where T : ScriptableObject
    {
        var result = new Dictionary<(string mainKey, string subKey), T>();
        if (keys == null || keys.Count == 0)
            return result;

        // 캐시 조회 및 로드 대상 분리
        var toLoad = new List<(string loadKey, (string mainKey, string subKey) keyTuple)>();
        foreach (var keyTuple in keys)
        {
            string loadKey = BuildClientDataKey(keyTuple.mainKey, keyTuple.subKey);

            if (_resources.TryGetValue(loadKey, out var cached) && cached is T cachedAsset)
            {
                result[keyTuple] = cachedAsset;
            }
            else
            {
                toLoad.Add((loadKey, keyTuple));
            }
        }

        // 병렬 로드
        if (toLoad.Count > 0)
        {
            var tasks = toLoad.Select(async item =>
            {
                var handle = Addressables.LoadAssetAsync<T>(item.loadKey);
                try
                {
                    var asset = await handle.ToUniTask(cancellationToken: cancellationToken);

                    if (asset != null)
                    {
                        _resources.TryAdd(item.loadKey, asset);
                        _resourceHandles.TryAdd(item.loadKey, handle);
                        return (item.keyTuple, asset);
                    }
                    else
                    {
                        Debug.LogError($"[ResourceManager] Failed to load ClientData<{typeof(T)}> : {item.loadKey}");
                        if (handle.IsValid())
                        {
                            Addressables.Release(handle);
                        }
                        return (item.keyTuple, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ResourceManager] Exception loading {item.loadKey}: {e.Message}");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                    return (item.keyTuple, null);
                }
            }).ToArray();

            var loaded = await UniTask.WhenAll(tasks);
            foreach (var (keyTuple, asset) in loaded)
            {
                if (asset != null)
                {
                    result[keyTuple] = asset;
                }
            }
        }

        return result;
    }

    #endregion

    #region Addressables Core Loading

    /// <summary>
    /// Addressables 동기 로딩 (Handle 누수 방지 개선)
    /// </summary>
    [Obsolete("Use LoadByNameAsync to avoid blocking")]
    private T LoadByName<T>(string name) where T : Object
    {
        AsyncOperationHandle<IList<IResourceLocation>> locHandle = default;

        try
        {
            locHandle = Addressables.LoadResourceLocationsAsync(name, typeof(T));
            locHandle.WaitForCompletion();

            if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null)
            {
                Debug.LogError($"[ResourceManager] Failed to load resource locations for type: {typeof(T)}, key: {name}");
                return null;
            }

            var loc = locHandle.Result.FirstOrDefault(l => l.PrimaryKey.Contains(name));
            if (loc == null)
            {
                Debug.LogError($"[ResourceManager] No matching location found for: {name}");
                return null;
            }

            var assetHandle = Addressables.LoadAssetAsync<T>(loc);
            assetHandle.WaitForCompletion();

            if (assetHandle.Status == AsyncOperationStatus.Succeeded && assetHandle.Result != null)
            {
                _resources.TryAdd(name, assetHandle.Result);
                _resourceHandles.TryAdd(name, assetHandle);
                return assetHandle.Result;
            }
            else
            {
                Debug.LogError($"[ResourceManager] Failed to load asset: {name}");
                if (assetHandle.IsValid())
                {
                    Addressables.Release(assetHandle);
                }
                return null;
            }
        }
        finally
        {
            // Location 핸들 해제 (메모리 누수 방지)
            if (locHandle.IsValid())
            {
                Addressables.Release(locHandle);
            }
        }
    }

    /// <summary>
    /// Addressables 비동기 로딩 (재시도 로직 포함, Handle 누수 방지)
    /// </summary>
    private async UniTask<T> LoadByNameAsync<T>(
        string name,
        CancellationToken cancellationToken = default
    ) where T : Object
    {
        int retryCount = 0;

        while (retryCount < MAX_RETRIES)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locHandle = default;
            AsyncOperationHandle<T> loadHandle = default;

            try
            {
                // 1. Location 로드
                locHandle = Addressables.LoadResourceLocationsAsync(name, typeof(T));
                var locations = await locHandle.ToUniTask(cancellationToken: cancellationToken);

                if (locHandle.Status != AsyncOperationStatus.Succeeded || locations == null || locations.Count == 0)
                {
                    throw new Exception($"Failed to load resource locations for: {name}");
                }

                var loc = locations.FirstOrDefault(l => l.PrimaryKey.Contains(name));
                if (loc == null)
                {
                    throw new Exception($"No matching location found for: {name}");
                }

                // 2. Asset 로드
                loadHandle = Addressables.LoadAssetAsync<T>(loc);
                var asset = await loadHandle.ToUniTask(cancellationToken: cancellationToken);

                if (loadHandle.Status == AsyncOperationStatus.Succeeded && asset != null)
                {
                    _resources.TryAdd(name, asset);
                    _resourceHandles.TryAdd(name, loadHandle);

                    // Location 핸들만 해제 (asset 핸들은 캐시에 보관)
                    if (locHandle.IsValid())
                    {
                        Addressables.Release(locHandle);
                    }

                    return asset;
                }
                else
                {
                    throw new Exception($"Failed to load asset: {name}, Status: {loadHandle.Status}");
                }
            }
            catch (OperationCanceledException)
            {
                // 취소는 재시도 없이 즉시 반환
                CleanupHandles(locHandle, loadHandle);
                throw;
            }
            catch (Exception e)
            {
                retryCount++;
                Debug.LogWarning($"[ResourceManager] Attempt {retryCount}/{MAX_RETRIES} failed for {name}: {e.Message}");

                CleanupHandles(locHandle, loadHandle);

                if (retryCount >= MAX_RETRIES)
                {
                    Debug.LogError($"[ResourceManager] Failed to load {name} after {MAX_RETRIES} attempts");
                    return null;
                }

                // 재시도 전 대기
                await UniTask.Delay(RETRY_DELAY_MS * retryCount, cancellationToken: cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// 실패한 핸들 정리 헬퍼
    /// </summary>
    private void CleanupHandles(params AsyncOperationHandle[] handles)
    {
        foreach (var handle in handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }


    #endregion

    #region Label-Based Loading

    /// <summary>
    /// 레이블 기반 대량 리소스 로드 (진행률 콜백 지원).
    /// 동일 레이블 동시 호출 시 한 번만 로드하고 나머지는 동일 태스크를 await.
    /// </summary>
    public async UniTask LoadAllAsyncByLabel<T>(
        string label,
        Action<string, int, int> progressCallback,
        CancellationToken cancellationToken = default
    ) where T : Object
    {
        if (_loadedLabels.ContainsKey(label))
        {
            progressCallback?.Invoke(string.Empty, 0, 0);
            return;
        }

        bool weCreated = false;
        UniTask task;

        lock (_labelLoadLock)
        {
            if (_loadedLabels.ContainsKey(label))
            {
                progressCallback?.Invoke(string.Empty, 0, 0);
                return;
            }

            if (_inFlightLabelTasks.TryGetValue(label, out var existing))
            {
                task = existing;
            }
            else
            {
                weCreated = true;
                task = LoadAllAsyncByLabelInternal<T>(label, progressCallback, cancellationToken);
                _inFlightLabelTasks[label] = task;
            }
        }

        await task;

        if (weCreated)
        {
            lock (_labelLoadLock)
            {
                _inFlightLabelTasks.TryRemove(label, out _);
            }
        }
    }

    /// <summary>
    /// 레이블 로드 실제 수행 (내부용). 레이블별 중복 호출 방지는 호출측에서 처리.
    /// </summary>
    private async UniTask LoadAllAsyncByLabelInternal<T>(
        string label,
        Action<string, int, int> progressCallback,
        CancellationToken cancellationToken
    ) where T : Object
    {
        AsyncOperationHandle<IList<IResourceLocation>> locHandle = default;

        try
        {
            locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            var locations = await locHandle.ToUniTask(cancellationToken: cancellationToken);

            if (locHandle.Status != AsyncOperationStatus.Succeeded || locations == null || locations.Count == 0)
            {
                Debug.LogError($"[ResourceManager] Failed to load resource locations for label: {label}");
                progressCallback?.Invoke(string.Empty, 0, 0);
                return;
            }

            int totalCount = locations.Count;
            int loadedCount = 0;
            var labelKeys = _labelToAssetKeys.GetOrAdd(label, _ => new ConcurrentDictionary<string, bool>());
            var toLoad = new List<IResourceLocation>();

            foreach (var loc in locations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_resources.ContainsKey(loc.PrimaryKey))
                {
                    labelKeys.TryAdd(loc.PrimaryKey, true);
                    loadedCount++;
                    progressCallback?.Invoke(loc.PrimaryKey, loadedCount, totalCount);
                }
                else
                {
                    toLoad.Add(loc);
                }
            }

            if (toLoad.Count > 0)
            {
                var tasks = toLoad.Select(async loc =>
                {
                    var handle = Addressables.LoadAssetAsync<T>(loc);
                    try
                    {
                        var asset = await handle.ToUniTask(cancellationToken: cancellationToken);

                        if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
                        {
                            _resources.TryAdd(asset.name, asset);
                            _resourceHandles.TryAdd(asset.name, handle);
                            labelKeys.TryAdd(asset.name, true);

                            if (label == PRELOAD_LABEL)
                            {
                                _preloadAssetKeys.TryAdd(asset.name, true);
                            }

                            return (success: true, name: asset.name);
                        }
                        else
                        {
                            Debug.LogError($"[ResourceManager] Failed to load asset for label {label}. Status: {handle.Status}");
                            if (handle.IsValid())
                            {
                                Addressables.Release(handle);
                            }
                            return (success: false, name: string.Empty);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ResourceManager] Exception loading asset for label {label}: {e.Message}");
                        if (handle.IsValid())
                        {
                            Addressables.Release(handle);
                        }
                        return (success: false, name: string.Empty);
                    }
                }).ToArray();

                var results = await UniTask.WhenAll(tasks);

                foreach (var result in results)
                {
                    if (result.success)
                    {
                        loadedCount++;
                        progressCallback?.Invoke(result.name, loadedCount, totalCount);
                    }
                }
            }

            _loadedLabels.TryAdd(label, true);
        }
        finally
        {
            if (locHandle.IsValid())
            {
                Addressables.Release(locHandle);
            }
        }
    }


    /// <summary>
    /// PreLoad 레이블 리소스를 최초 1회만 로드 (Thread-Safe)
    /// </summary>
    public async UniTask EnsurePreloadCached(
        Action<string, int, int> progressCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        // 이미 완료된 경우 즉시 반환
        if (_isPreloadCached || _loadedLabels.ContainsKey(PRELOAD_LABEL))
        {
            progressCallback?.Invoke(string.Empty, 0, 0);
            return;
        }

        lock (_preloadLock)
        {
            if (_isPreloadCached)
            {
                progressCallback?.Invoke(string.Empty, 0, 0);
                return;
            }
        }

        await LoadAllAsyncByLabel<Object>(PRELOAD_LABEL, progressCallback, cancellationToken);

        // 로드 완료 후에만 플래그 설정 (다른 호출자가 "완료됨"으로 인식)
        lock (_preloadLock)
        {
            _isPreloadCached = true;
        }
    }

    /// <summary>
    /// 레이블 로드 완료 여부 확인
    /// </summary>
    public bool IsLabelLoaded(string label)
    {
        return _loadedLabels.ContainsKey(label);
    }

    #endregion

    #region Sprite Loading


    /// <summary>
    /// Sprite 비동기 로드 (SpriteAtlas 인덱스 지원)
    /// index = -1: 단일 Sprite, index >= 0: Atlas에서 인덱스로 추출
    /// </summary>
    public async UniTask<Sprite> LoadSpriteAsync(
        string atlasAddress,
        int index = -1,
        CancellationToken cancellationToken = default
    )
    {
        string spriteKey = index >= 0 ? $"{atlasAddress}_{index}" : atlasAddress;

        // 캐시 확인
        if (_resources.TryGetValue(spriteKey, out Object cached) && cached is Sprite cachedSprite)
        {
            return cachedSprite;
        }

        try
        {
            Sprite sprite = null;

            if (index >= 0)
            {
                // SpriteAtlas에서 로드
                var atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                var atlas = await atlasHandle.ToUniTask(cancellationToken: cancellationToken);

                if (atlas == null)
                {
                    Debug.LogError($"[ResourceManager] Failed to load sprite atlas: {atlasAddress}");
                    if (atlasHandle.IsValid())
                    {
                        Addressables.Release(atlasHandle);
                    }
                    return null;
                }

                sprite = atlas.GetSprite(spriteKey);
                _resourceHandles.TryAdd(spriteKey, atlasHandle);
            }
            else
            {
                // 단일 Sprite 로드
                var spriteHandle = Addressables.LoadAssetAsync<UnityEngine.Sprite>(atlasAddress);
                sprite = await spriteHandle.ToUniTask(cancellationToken: cancellationToken);

                if (sprite == null && spriteHandle.IsValid())
                {
                    Addressables.Release(spriteHandle);
                    return null;
                }

                _resourceHandles.TryAdd(spriteKey, spriteHandle);
            }

            if (sprite == null)
            {
                Debug.LogError($"[ResourceManager] Failed to load sprite: {spriteKey}");
                return null;
            }

            _resources.TryAdd(spriteKey, sprite);
            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception loading sprite {spriteKey}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sprite 동기 로드 (가능하면 비동기 사용 권장)
    /// </summary>
    [Obsolete("Use LoadSpriteAsync to avoid blocking")]
    public Sprite LoadSprite(string atlasAddress, int index = -1)
    {
        string spriteKey = index >= 0 ? $"{atlasAddress}_{index}" : atlasAddress;

        if (_resources.TryGetValue(spriteKey, out Object resource))
        {
            return resource as Sprite;
        }

        Sprite sprite = null;

        if (index >= 0)
        {
            SpriteAtlas atlas = Load<SpriteAtlas>(atlasAddress);
            if (!atlas)
            {
                Debug.LogError($"[ResourceManager] Failed to load sprite atlas: {atlasAddress}");
                return null;
            }
            sprite = atlas.GetSprite(spriteKey);
        }
        else
        {
            sprite = Load<Sprite>(atlasAddress);
        }

        if (!sprite)
        {
            Debug.LogError($"[ResourceManager] Failed to load sprite: {spriteKey}");
            return null;
        }

        _resources.TryAdd(spriteKey, sprite);
        return sprite;
    }

    /// <summary>
    /// Texture2D에서 모든 Sprite 로드 (재시도 로직 포함)
    /// </summary>
    public async UniTask<List<Sprite>> LoadAllSpritesFromTexture2DAsync(
        string atlasAddress,
        CancellationToken cancellationToken = default
    )
    {
        int retryCount = 0;

        while (retryCount < MAX_RETRIES)
        {
            AsyncOperationHandle<IList<Sprite>> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<IList<Sprite>>(atlasAddress);
                var spriteList = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (handle.Status == AsyncOperationStatus.Succeeded && spriteList != null && spriteList.Count > 0)
                {
                    var result = new List<Sprite>();

                    foreach (var sprite in spriteList)
                    {
                        if (sprite != null)
                        {
                            result.Add(sprite);
                            _resources.TryAdd(sprite.name, sprite);
                        }
                    }

                    _resourceHandles.TryAdd(atlasAddress, handle);
                    return result;
                }
                else if (spriteList != null && spriteList.Count == 0)
                {
                    Debug.LogWarning($"[ResourceManager] Loaded sprite list is empty for atlas: {atlasAddress}");
                    retryCount++;

                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    await UniTask.Delay(RETRY_DELAY_MS * retryCount, cancellationToken: cancellationToken);
                    continue;
                }
                else
                {
                    throw new Exception($"Failed to load sprites, Status: {handle.Status}");
                }
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
            catch (Exception e)
            {
                retryCount++;
                Debug.LogWarning($"[ResourceManager] Attempt {retryCount}/{MAX_RETRIES}: Error loading sprites from atlas {atlasAddress}: {e.Message}");

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                if (retryCount >= MAX_RETRIES)
                {
                    Debug.LogError($"[ResourceManager] Failed to load sprites after {MAX_RETRIES} attempts from atlas: {atlasAddress}");
                    return new List<Sprite>();
                }

                await UniTask.Delay(RETRY_DELAY_MS * retryCount, cancellationToken: cancellationToken);
            }
        }

        return new List<Sprite>();
    }

    /// <summary>
    /// Texture2D에서 모든 Sprite 동기 로드 (레거시, 비동기 사용 권장)
    /// </summary>
    [Obsolete("Use LoadAllSpritesFromTexture2DAsync to avoid blocking")]
    public List<Sprite> LoadAllSpritesFromTexture2D(string atlasAddress)
    {
        List<Sprite> sprites = new List<Sprite>();
        int retryCount = 0;
        AsyncOperationHandle<IList<Sprite>> handle = default;

        while (retryCount < MAX_RETRIES)
        {
            try
            {
                handle = Addressables.LoadAssetAsync<IList<Sprite>>(atlasAddress);
                handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    if (handle.Result.Count == 0)
                    {
                        Debug.LogWarning($"[ResourceManager] Loaded sprite list is empty for atlas: {atlasAddress}");
                        retryCount++;
                        if (handle.IsValid())
                        {
                            Addressables.Release(handle);
                        }
                        continue;
                    }

                    foreach (var sprite in handle.Result)
                    {
                        if (sprite != null)
                        {
                            sprites.Add(sprite);
                            _resources.TryAdd(sprite.name, sprite);
                        }
                    }

                    _resourceHandles.TryAdd(atlasAddress, handle);
                    return sprites;
                }

                Debug.LogWarning($"[ResourceManager] Attempt {retryCount + 1}/{MAX_RETRIES}: Failed to load sprites from texture: {atlasAddress}");
                retryCount++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResourceManager] Attempt {retryCount + 1}/{MAX_RETRIES}: Error loading sprites from atlas {atlasAddress}: {e.Message}");
                retryCount++;
            }
            finally
            {
                if (retryCount < MAX_RETRIES && handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        Debug.LogError($"[ResourceManager] Failed to load sprites after {MAX_RETRIES} attempts from atlas: {atlasAddress}");
        return new List<Sprite>();
    }

    #endregion

    #region Label Utilities

    /// <summary>
    /// 레이블의 리소스 개수 조회 (Coroutine 방식, 레거시)
    /// </summary>
    [Obsolete("Use GetLabelCountAsync instead")]
    public IEnumerator GetLabelCount(string label, Action<int> callback)
    {
        AsyncOperationHandle<IList<IResourceLocation>> handle = default;

        try
        {
            handle = Addressables.LoadResourceLocationsAsync(label, typeof(Object));
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(handle.Result.Count);
            }
            else
            {
                Debug.LogError($"[ResourceManager] Failed to load resource locations for label: {label}");
                callback?.Invoke(0);
            }
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    /// <summary>
    /// 레이블의 리소스 개수 비동기 조회
    /// </summary>
    public async UniTask<int> GetLabelCountAsync(string label, CancellationToken cancellationToken = default)
    {
        AsyncOperationHandle<IList<IResourceLocation>> handle = default;

        try
        {
            handle = Addressables.LoadResourceLocationsAsync(label, typeof(Object));
            var locations = await handle.ToUniTask(cancellationToken: cancellationToken);
            return locations?.Count ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Failed to load locations for label '{label}': {e}");
            return 0;
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    #endregion

    #region Memory Management

    /// <summary>
    /// 모든 Addressable 리소스 언로드
    /// </summary>
    public async UniTask<bool> UnloadAllAddressableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var labels = _loadedLabels.Keys.ToList();

            if (labels.Count > 0)
            {
                var clearOp = Addressables.ClearDependencyCacheAsync(labels, true);
                await clearOp.ToUniTask(cancellationToken: cancellationToken);

                if (clearOp.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[ResourceManager] Unload failed. Status: {clearOp.Status}, Exception: {clearOp.OperationException}");
                    return false;
                }
            }

            ReleaseAllHandles();
            _resources.Clear();
            _loadedLabels.Clear();
            _preloadAssetKeys.Clear();
            _labelToAssetKeys.Clear();

            lock (_preloadLock)
            {
                _isPreloadCached = false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception during unload: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 특정 레이블(labelToKeep)만 남기고 나머지 레이블 리소스 언로드
    /// </summary>
    public async UniTask<bool> UnloadAllAddressableExceptLabelAsync(
        string labelToKeep,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var labelsToUnload = _loadedLabels.Keys.Where(l => l != labelToKeep).ToList();

            if (labelsToUnload.Count == 0)
            {
                return true;
            }

            var clearOp = Addressables.ClearDependencyCacheAsync(labelsToUnload, false);
            await clearOp.ToUniTask(cancellationToken: cancellationToken);

            if (clearOp.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] Unload operation failed. Status: {clearOp.Status}, Exception: {clearOp.OperationException}");
                return false;
            }

            foreach (var label in labelsToUnload)
            {
                _loadedLabels.TryRemove(label, out _);
                _labelToAssetKeys.TryRemove(label, out _);
            }

            // 유지할 레이블(labelToKeep)에 속한 에셋 키만 보존
            var keysToKeep = _labelToAssetKeys.TryGetValue(labelToKeep, out var keepSet)
                ? new HashSet<string>(keepSet.Keys)
                : new HashSet<string>();

            var keysToRemove = _resources.Keys.Where(k => !keysToKeep.Contains(k)).ToList();

            ReleaseHandles(key => !keysToKeep.Contains(key));

            foreach (var key in keysToRemove)
            {
                _resources.TryRemove(key, out _);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception during selective unload: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 모든 Addressable 리소스 언로드 (레거시 콜백 방식)
    /// </summary>
    [Obsolete("Use UnloadAllAddressableAsync instead")]
    public void UnloadAllAddressable(Action<bool> callback = null)
    {
        UnloadAllAddressableAsync().ContinueWith(success => callback?.Invoke(success)).Forget();
    }

    /// <summary>
    /// 특정 레이블 제외 나머지 언로드 (레거시 콜백 방식)
    /// </summary>
    [Obsolete("Use UnloadAllAddressableExceptLabelAsync instead")]
    public void UnloadAllAddressableExceptLabel(string labelToKeep, Action<bool> callback = null)
    {
        UnloadAllAddressableExceptLabelAsync(labelToKeep)
            .ContinueWith(success => callback?.Invoke(success))
            .Forget();
    }

    #endregion


    #region Catalog Management

    /// <summary>
    /// 원격 카탈로그 로드 및 캐시 정리
    /// </summary>
    public async UniTask<bool> LoadCatalogAsync(
        string catalogVersion,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            string buildTarget = GetBuildTarget();

            if (string.IsNullOrEmpty(buildTarget))
            {
                Debug.LogError("[ResourceManager] Build target is not specified.");
                return false;
            }

            string catalogUrl =
                $"https://storage.googleapis.com/hoosik-game-data/ServerData/{Application.version}/{buildTarget}/catalog_{Application.version}.bin";

            Debug.Log($"[ResourceManager] Loading catalog from: {catalogUrl}");

            var catalogHandle = Addressables.LoadContentCatalogAsync(catalogUrl);
            var catalog = await catalogHandle.ToUniTask(cancellationToken: cancellationToken);

            if (catalogHandle.Status == AsyncOperationStatus.Succeeded && catalog != null)
            {
                Debug.Log($"[ResourceManager] Catalog {catalogVersion} loaded successfully.");

                // 캐시 정리
                bool cacheCleared = await ClearCacheAsync(cancellationToken);

                if (cacheCleared)
                {
                    Debug.Log("[ResourceManager] Cache cleared successfully.");
                }

                return cacheCleared;
            }
            else
            {
                Debug.LogError($"[ResourceManager] Failed to load catalog {catalogVersion}. Status: {catalogHandle.Status}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception loading catalog: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 플랫폼별 빌드 타겟 문자열 반환
    /// </summary>
    private string GetBuildTarget()
    {
#if UNITY_IOS
        return "iOS";
#elif UNITY_ANDROID
        return "Android";
#elif UNITY_STANDALONE_WIN
        return "Windows";
#elif UNITY_STANDALONE_OSX
        return "macOS";
#else
        return null;
#endif
    }

    /// <summary>
    /// Addressables 캐시 완전 정리
    /// </summary>
    public async UniTask<bool> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var labels = _loadedLabels.Keys.ToList();

            if (labels.Count > 0)
            {
                var clearOp = Addressables.ClearDependencyCacheAsync(labels, true);
                await clearOp.ToUniTask(cancellationToken: cancellationToken);

                if (clearOp.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("[ResourceManager] Failed to clear Addressables cache.");
                    return false;
                }
            }

            ReleaseAllHandles();
            _resources.Clear();
            _loadedLabels.Clear();
            _preloadAssetKeys.Clear();
            _labelToAssetKeys.Clear();

            lock (_preloadLock)
            {
                _isPreloadCached = false;
            }

            Debug.Log("[ResourceManager] Successfully cleared Addressables cache.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceManager] Exception clearing cache: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 카탈로그 로드 (레거시 콜백 방식)
    /// </summary>
    [Obsolete("Use LoadCatalogAsync instead")]
    public void LoadCatalogAsync(string catalogVersion, Action<bool> callback = null)
    {
        LoadCatalogAsync(catalogVersion, CancellationToken.None)
            .ContinueWith(success => callback?.Invoke(success))
            .Forget();
    }

    /// <summary>
    /// 캐시 정리 (레거시 콜백 방식)
    /// </summary>
    [Obsolete("Use ClearCacheAsync instead")]
    public void ClearCache(Action callback)
    {
        ClearCacheAsync(CancellationToken.None)
            .ContinueWith(_ => callback?.Invoke())
            .Forget();
    }

    #endregion
}