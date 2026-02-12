using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StellaGuild.UI.Buttons
{
    [RequireComponent(typeof(Button))]
    public sealed class StellaButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private StellaButtonVisualStyle style = StellaButtonVisualStyle.ActionRed;
        [SerializeField] private Graphic faceGraphic;
        [SerializeField] private Graphic shadowGraphic;
        [SerializeField] private Graphic labelGraphic;
        [SerializeField] private RectTransform faceTransform;
        [SerializeField] private Vector2 releasedLocalPosition = Vector2.zero;
        [SerializeField] private Vector2 pressedLocalPosition = new(4f, -4f);
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyInEditor = true;

        private bool _isPressed;

        private void Reset()
        {
            CacheReferences();
            ApplyStyle();
            RefreshPressState();
        }

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                ApplyStyle();
            }

            _isPressed = false;
            RefreshPressState();
        }

        private void OnValidate()
        {
            if (!applyInEditor)
            {
                return;
            }

            CacheReferences();
            ApplyStyle();
            RefreshPressState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            RefreshPressState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            RefreshPressState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPressed = false;
            RefreshPressState();
        }

        [ContextMenu("Apply Style")]
        public void ApplyStyle()
        {
            if (faceGraphic != null)
            {
                faceGraphic.color = StellaButtonStylePalette.GetFaceColor(style);
            }

            if (shadowGraphic != null)
            {
                shadowGraphic.color = StellaButtonStylePalette.GetShadowColor();
            }

            if (labelGraphic != null)
            {
                labelGraphic.color = StellaButtonStylePalette.GetLabelColor(style);
            }
        }

        private void CacheReferences()
        {
            if (faceGraphic == null)
            {
                faceGraphic = GetComponent<Graphic>();
            }

            if (faceTransform == null && faceGraphic != null)
            {
                faceTransform = faceGraphic.rectTransform;
            }
        }

        private void RefreshPressState()
        {
            if (faceTransform == null)
            {
                return;
            }

            faceTransform.anchoredPosition = _isPressed ? pressedLocalPosition : releasedLocalPosition;
        }
    }
}
