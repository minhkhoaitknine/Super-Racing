using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";
        [SerializeField] private string playGameSceneName = "Test_Vehicle";

        public void PlayGame()
        {
            SceneManager.LoadScene(playGameSceneName);
        }

        public void OpenGarage()
        {
            SceneManager.LoadScene(garageSceneName);
        }

        public void OpenTrackSelection()
        {
            if (Application.CanStreamedLevelBeLoaded(trackSelectionSceneName))
            {
                SceneManager.LoadScene(trackSelectionSceneName);
                return;
            }

            Debug.LogWarning($"Track selection scene '{trackSelectionSceneName}' is not available yet.");
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
