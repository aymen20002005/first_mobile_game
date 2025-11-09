using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.UI
{
    /// <summary>
    /// Simple main menu controller with Play and Quit actions.
    /// Attach this to a GameObject in the Main Menu scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Name of the scene to load when Play is pressed. Leave empty to use build index 1.")]
        public string gameSceneName = "GameScene";

        /// <summary>
        /// Called by the Play button.
        /// Loads the configured game scene.
        /// </summary>
        public void OnPlayButton()
        {
            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // Fallback: try load build index 1
                SceneManager.LoadScene(1);
            }
        }

        /// <summary>
        /// Called by the Quit button.
        /// Quits the application; in the editor it stops play mode.
        /// </summary>
        public void OnQuitButton()
        {
#if UNITY_EDITOR
            // Stop play mode when running inside the editor
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                Debug.Log("Quit requested (Editor: not playing). Use Application.Quit() in a build.");
            }
#else
            Application.Quit();
#endif
        }
    }
}
