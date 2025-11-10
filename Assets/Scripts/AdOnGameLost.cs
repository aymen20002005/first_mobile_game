using UniRx;
using UnityEngine;

public class AdOnGameLost : MonoBehaviour
{
    private CompositeDisposable disposables = new CompositeDisposable();

    private void OnEnable()
    {
        Debug.Log("[AdOnGameLost] OnEnable - starting subscription coroutine.");
        StartCoroutine(Subscribe());
    }

    private System.Collections.IEnumerator Subscribe()
    {
        yield return new WaitUntil(() => GameEvents.instance != null);
        Debug.Log("[AdOnGameLost] Subscribing to GameEvents.gameLost.");
        GameEvents.instance.gameLost.ObserveEveryValueChanged(x => x.Value)
            .Subscribe(value =>
            {
                Debug.Log($"[AdOnGameLost] gameLost value changed -> {value}");
                if (value)
                {
                    Debug.Log("[AdOnGameLost] Game lost detected. (ads handled by AdsManager)");
                    // Ads are now handled centrally by AdsManager; do not call ShowAd() here to avoid duplicates.
                }
            })
            .AddTo(disposables);
    }

    private void OnDisable()
    {
        disposables.Clear();
    }
}
