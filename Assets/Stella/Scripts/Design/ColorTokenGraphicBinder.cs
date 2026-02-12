using UnityEngine;
using UnityEngine.UI;

namespace StellaGuild.Design
{
    [ExecuteAlways]
    public sealed class ColorTokenGraphicBinder : MonoBehaviour
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private ColorToken token = ColorToken.TextShadow;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool applyInEditor = true;

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            if (!applyInEditor)
            {
                return;
            }

            Apply();
        }

        [ContextMenu("Apply Token")]
        public void Apply()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (targetGraphic == null)
            {
                Debug.LogWarning("ColorTokenGraphicBinder requires a Graphic target.", this);
                return;
            }

            targetGraphic.color = StellaColorTokens.Get(token);
        }
    }
}
