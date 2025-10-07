using UnityEngine;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// Base interface for optimized UI panels
    /// </summary>
    public interface IOptimizedUIPanel
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        void UpdatePanel(float deltaTime);
    }

    /// <summary>
    /// Base class for optimized UI panels
    /// </summary>
    public abstract class OptimizedUIPanel : MonoBehaviour, IOptimizedUIPanel
    {
        [SerializeField] protected bool _isVisible = false;

        public virtual bool IsVisible => _isVisible && gameObject.activeInHierarchy;

        public virtual void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            OnShow();
        }

        public virtual void Hide()
        {
            _isVisible = false;
            gameObject.SetActive(false);
            OnHide();
        }

        public abstract void UpdatePanel(float deltaTime);

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}
