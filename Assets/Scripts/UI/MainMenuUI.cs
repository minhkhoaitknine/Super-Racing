using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";

        private void Start()
        {
            CreateGameLogo();
        }

        private static void CreateGameLogo()
        {
            if (GameObject.Find("Super Racing Logo") != null) return;
            Texture2D logoTexture = Resources.Load<Texture2D>("UI/SuperRacingLogo");
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (logoTexture == null || canvas == null) return;

            GameObject logoObject = new("Super Racing Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            logoObject.layer = canvas.gameObject.layer;
            logoObject.transform.SetParent(canvas.transform, false);
            RawImage logo = logoObject.GetComponent<RawImage>();
            logo.texture = logoTexture;
            logo.raycastTarget = false;
            // Crop the small outer export margin and keep only the finished navy badge.
            logo.uvRect = new Rect(0.009f, 0.069f, 0.982f, 0.872f);

            RectTransform rect = logo.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -82f);
            rect.sizeDelta = new Vector2(430f, 190f);
        }

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
