using System.Collections;
using StellaGuild.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StellaGuild.Flow
{
    public class StartupFlowController : MonoBehaviour
    {
        [SerializeField] private UIPageRouter pageRouter;
        [SerializeField] private UIPageType startDestinationPage = UIPageType.Home;
        [SerializeField] private string gameplaySceneName = "Stage01";
        [SerializeField] private CanvasGroup startupLogoCanvasGroup;
        [SerializeField] private float startupLogoDurationSeconds = 1.6f;
        [SerializeField] private float startupLogoFadeSeconds = 0.35f;
        [SerializeField] private bool skipStartupLogo;

        private IEnumerator Start()
        {
            if (pageRouter == null)
            {
                Debug.LogError("StartupFlowController requires a UIPageRouter reference.", this);
                yield break;
            }

            yield return PlayStartupLogoIfConfigured();
            pageRouter.ShowInitialPage();
        }

        public void OnStartPressed()
        {
            if (pageRouter != null && pageRouter.Contains(startDestinationPage))
            {
                pageRouter.Navigate(startDestinationPage);
                return;
            }

            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogWarning("No destination page or scene is configured.", this);
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnSettingsPressed()
        {
            if (pageRouter != null && pageRouter.Contains(UIPageType.Settings))
            {
                pageRouter.Navigate(UIPageType.Settings);
            }
        }

        private IEnumerator PlayStartupLogoIfConfigured()
        {
            if (skipStartupLogo || startupLogoCanvasGroup == null)
            {
                yield break;
            }

            SetStartupLogoVisible(true);
            startupLogoCanvasGroup.alpha = 1f;

            if (startupLogoDurationSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(startupLogoDurationSeconds);
            }

            if (startupLogoFadeSeconds <= 0f)
            {
                SetStartupLogoVisible(false);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < startupLogoFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / startupLogoFadeSeconds);
                startupLogoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            SetStartupLogoVisible(false);
        }

        private void SetStartupLogoVisible(bool visible)
        {
            if (startupLogoCanvasGroup == null)
            {
                return;
            }

            startupLogoCanvasGroup.gameObject.SetActive(visible);
            startupLogoCanvasGroup.interactable = visible;
            startupLogoCanvasGroup.blocksRaycasts = visible;
        }
    }
}
