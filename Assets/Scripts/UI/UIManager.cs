using UniRx;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    private CompositeDisposable subscriptions = new CompositeDisposable();
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject loseUI;

    private void OnEnable()
    {
        StartCoroutine(Subscribe());
        gameUI.SetActive(true);
        startUI.SetActive(true);
    }
    private IEnumerator Subscribe()
    {
        yield return new WaitUntil(() => GameEvents.instance != null);

        GameEvents.instance.gameStarted.ObserveEveryValueChanged(x => x.Value)
            .Subscribe(value =>
            {
                if (value)
                    ActivateMenu(gameUI);
            })
            .AddTo(subscriptions);

        GameEvents.instance.gameWon.ObserveEveryValueChanged(x => x.Value)
            .Subscribe(value =>
            {
                if (value)
                    ActivateMenu(winUI);
            })
            .AddTo(subscriptions);

        GameEvents.instance.gameLost.ObserveEveryValueChanged(x => x.Value)
            .Subscribe(value =>
            {
                if (value)
                    ActivateMenu(loseUI);
            })
            .AddTo(subscriptions);
    }
    private void OnDisable()
    {
        subscriptions.Clear();
    }

    private void ActivateMenu(GameObject _menu)
    {
        gameUI.SetActive(false);
        startUI.SetActive(false);
        winUI.SetActive(false);
        loseUI.SetActive(false);

        _menu.SetActive(true);
    }

    //Level functions
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private IEnumerator DelayedGameStart()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure all resets are processed
        GameEvents.instance.gameStarted.SetValueAndForceNotify(true);
    }

    public void NextLevel()
    {
        int newCurrentLevel = PlayerPrefs.GetInt("currentLevel", 1) + 1;
        int newLoadingLevel = PlayerPrefs.GetInt("loadingLevel", 1) + 1;

        if (newLoadingLevel >= SceneManager.sceneCountInBuildSettings)
            newLoadingLevel = 1;

        PlayerPrefs.SetInt("currentLevel", newCurrentLevel);
        PlayerPrefs.SetInt("loadingLevel", newLoadingLevel);

        SceneManager.LoadScene(newLoadingLevel);
    }

    // Called by a "Try Again" button that resets the current run without reloading the scene
    public void TryAgain()
    {
        if (GameEvents.instance == null) return;

        Debug.Log("[UIManager] TryAgain called: resetting game state...");
        
        StartCoroutine(ResetGameState());
    }

    private IEnumerator ResetGameState()
    {
        // First reset game flags to ensure ad system recognizes the game is no longer in lost state
        GameEvents.instance.gameWon.SetValueAndForceNotify(false);
        GameEvents.instance.gameLost.SetValueAndForceNotify(false);
        
        yield return new WaitForSeconds(0.1f);

        // Then reset AdsManager to ensure it's ready for the next loss
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ResetForRetry();
            Debug.Log("[UIManager] AdsManager state reset for retry.");
        }

        yield return new WaitForSeconds(0.1f);

        // Reset UI to gameplay
        ActivateMenu(gameUI);

        // Reset player size
        GameEvents.instance.playerSize.Value = 1;

        yield return new WaitForSeconds(0.1f);

        // Finally start the game
        GameEvents.instance.gameStarted.SetValueAndForceNotify(true);

        Debug.Log("[UIManager] Game state reset complete.");
    }
}