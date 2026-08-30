using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";

        public void OpenGarage()
        {
            SceneManager.LoadScene(garageSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
