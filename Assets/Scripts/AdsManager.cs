using UnityEngine;
using UnityEngine.Advertisements;
using UniRx;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID";
    [SerializeField] private bool testMode = true;
    private string gameId;
    private string interstitialAdUnitId = "Interstitial_Android"; // Default placement ID, change if needed

    private static AdsManager instance;
    public static AdsManager Instance => instance;
    private bool showWhenLoaded = false;
    private CompositeDisposable disposables = new CompositeDisposable();
    private bool adRequestedForCurrentLoss = false;
    private bool adReady = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_ANDROID
            gameId = androidGameId;
#elif UNITY_IOS
            gameId = iosGameId;
#endif
            InitializeAds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeToGameLost());
    }

    private System.Collections.IEnumerator SubscribeToGameLost()
    {
        yield return new WaitUntil(() => GameEvents.instance != null);

        // Subscribe to gameLost and request ad when it becomes true
        GameEvents.instance.gameLost.ObserveEveryValueChanged(x => x.Value)
            .Subscribe(value =>
            {
                Debug.Log($"[AdsManager] Observed gameLost -> {value}");
                Debug.Log($"[AdsManager] flags before handling -> adRequestedForCurrentLoss={adRequestedForCurrentLoss}, showWhenLoaded={showWhenLoaded}, adReady={adReady}");
                
                if (value && !adRequestedForCurrentLoss)
                {
                    adRequestedForCurrentLoss = true;
                    Debug.Log("[AdsManager] Requesting ad for current loss.");
                    ShowAd();
                }
                
                Debug.Log($"[AdsManager] flags after handling -> adRequestedForCurrentLoss={adRequestedForCurrentLoss}, showWhenLoaded={showWhenLoaded}, adReady={adReady}");
            })
            .AddTo(disposables);
    }

    private void OnDisable()
    {
        disposables.Clear();
    }

    public void InitializeAds()
    {
        Debug.Log("[AdsManager] Initializing Unity Ads...");
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(gameId, testMode, this);
        }
        else if (Advertisement.isInitialized)
        {
            Debug.Log("[AdsManager] Ads already initialized, loading first ad...");
            LoadAd();
        }
        else
        {
            Debug.LogWarning("[AdsManager] Advertisement not supported.");
        }
    }

    public void ShowAd()
    {
        Debug.Log("[AdsManager] Attempting to show ad...");
        Debug.Log($"[AdsManager] State -> isInitialized={Advertisement.isInitialized}, isSupported={Advertisement.isSupported}, adReady={adReady}, IsReady={Advertisement.IsReady(interstitialAdUnitId)}");

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[AdsManager] Ads not initialized. Initializing now...");
            InitializeAds();
            showWhenLoaded = true;
            return;
        }

        if (adReady || Advertisement.IsReady(interstitialAdUnitId))
        {
            Debug.Log("[AdsManager] Ad is ready. Showing ad now.");
            adReady = false;
            Advertisement.Show(interstitialAdUnitId, this);
        }
        else
        {
            Debug.LogWarning("[AdsManager] Ad not ready. Loading ad and will show when loaded...");
            showWhenLoaded = true;
            LoadAd();
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("[AdsManager] Unity Ads initialization complete.");
        LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[AdsManager] Unity Ads Initialization Failed: {error.ToString()} - {message}");
        // Reset flags so we can try again
        adRequestedForCurrentLoss = false;
        showWhenLoaded = false;
        adReady = false;
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"[AdsManager] Ad loaded and ready: {placementId}.");
        if (placementId == interstitialAdUnitId)
        {
            adReady = true;
            if (showWhenLoaded)
            {
                Debug.Log("[AdsManager] showWhenLoaded is true -> showing ad now.");
                Advertisement.Show(placementId, this);
                showWhenLoaded = false;
                adReady = false;
            }
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[AdsManager] Failed to load ad {placementId}: {error.ToString()} - {message}");
        // Reset flags if load fails so we can try again
        if (placementId == interstitialAdUnitId)
        {
            adRequestedForCurrentLoss = false;
            showWhenLoaded = false;
            adReady = false;
        }
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[AdsManager] Failed to show ad {placementId}: {error.ToString()} - {message}");
        // reset request flags so next loss can trigger a new request
        adRequestedForCurrentLoss = false;
        showWhenLoaded = false;
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"[AdsManager] Ad show started: {placementId}");
        // When ad starts showing, mark not ready until next load
        adReady = false;
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"[AdsManager] Ad clicked: {placementId}");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[AdsManager] Ad show complete: {placementId}, State: {showCompletionState}");
        
        if (placementId == interstitialAdUnitId)
        {
            // Reset flags first
            adRequestedForCurrentLoss = false;
            showWhenLoaded = false;
            adReady = false;

            // Then load the next ad
            Debug.Log("[AdsManager] Loading next ad for future use...");
            LoadAd();
        }
    }

    private void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[AdsManager] Cannot load ad - ads not initialized");
            return;
        }

        Debug.Log($"[AdsManager] Loading ad: {interstitialAdUnitId}");
        Advertisement.Load(interstitialAdUnitId, this);
    }

    /// <summary>
    /// Reset internal request flags and preload an ad. Call this when the player retries.
    /// </summary>
    public void ResetForRetry()
    {
        Debug.Log("[AdsManager] ResetForRetry called - clearing flags and preloading ad.");
        adRequestedForCurrentLoss = false;
        showWhenLoaded = false;
        adReady = false;

        // If we don't have an ad ready, load one now
        if (!Advertisement.IsReady(interstitialAdUnitId))
        {
            LoadAd();
        }
    }
}
