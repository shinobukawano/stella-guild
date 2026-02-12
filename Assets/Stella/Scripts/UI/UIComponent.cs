using UnityEngine;

namespace StellaGuild.UI
{
    public abstract class UIComponent : MonoBehaviour
    {
        private bool _initialized;

        protected virtual void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
    }
}
