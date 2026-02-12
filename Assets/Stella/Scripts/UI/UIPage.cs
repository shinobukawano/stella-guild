using UnityEngine;

namespace StellaGuild.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPage : UIComponent
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public bool IsVisible { get; private set; }

        protected override void OnInitialize()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public virtual void Show()
        {
            EnsureInitialized();
            SetVisible(true);
            OnShown();
        }

        public virtual void Hide()
        {
            EnsureInitialized();
            SetVisible(false);
            OnHidden();
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
