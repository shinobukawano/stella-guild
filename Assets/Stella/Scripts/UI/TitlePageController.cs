using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StellaGuild.UI
{
    public class TitlePageController : UIPage
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private UnityEvent onStartPressed;
        [SerializeField] private UnityEvent onSettingsPressed;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (startButton == null)
            {
                Debug.LogError("TitlePageController requires a startButton reference.", this);
                return;
            }

            startButton.onClick.AddListener(HandleStartPressed);

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsPressed);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartPressed);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsPressed);
            }
        }

        private void HandleStartPressed()
        {
            onStartPressed?.Invoke();
        }

        private void HandleSettingsPressed()
        {
            onSettingsPressed?.Invoke();
        }
    }
}
