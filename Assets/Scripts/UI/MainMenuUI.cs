using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";

        public void PlayGame()
        {
            OpenGarage();
        }

        public void OpenGarage()
        {
            LoadSceneByNameOrBuildPath(garageSceneName);
        }

        public void OpenTrackSelection()
        {
            LoadSceneByNameOrBuildPath(trackSelectionSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private static void LoadSceneByNameOrBuildPath(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
                string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (nameWithoutExtension == sceneName)
                {
                    SceneManager.LoadScene(scenePath);
                    return;
                }
            }

            Debug.LogWarning($"Scene '{sceneName}' is not available in Build Settings.");
        }
    }
}
