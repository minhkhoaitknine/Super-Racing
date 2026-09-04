using SuperRacing.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class CurrencyDisplay : MonoBehaviour
    {
        private Text label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToKnownLabels();
        }

        private static void AttachToKnownLabels()
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.name == "Currency Text" || text.name == "Money Label")
                {
                    if (text.GetComponent<CurrencyDisplay>() == null)
                    {
                        text.gameObject.AddComponent<CurrencyDisplay>();
                    }
                }
            }
        }

        private void Awake()
        {
            label = GetComponent<Text>();
            Refresh(CurrencyWallet.Balance);
        }

        private void OnEnable() => CurrencyWallet.BalanceChanged += Refresh;
        private void OnDisable() => CurrencyWallet.BalanceChanged -= Refresh;

        private void Refresh(int balance)
        {
            if (label != null) label.text = $"◆  {balance:N0}";
        }
    }
}
