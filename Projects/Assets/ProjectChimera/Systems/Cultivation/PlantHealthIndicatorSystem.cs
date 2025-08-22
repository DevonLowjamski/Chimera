using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// System for displaying plant health indicators and status
    /// </summary>
    public class PlantHealthIndicatorSystem : MonoBehaviour
    {
        [Header("Health Indicator UI")]
        [SerializeField] private Canvas _healthCanvas;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _hydrationSlider;
        [SerializeField] private Slider _nutritionSlider;
        [SerializeField] private Image _stressIndicator;
        
        private bool _isInitialized = false;
        private InteractivePlant _targetPlant;
        
        public void Initialize(InteractivePlant plant)
        {
            _targetPlant = plant;
            _isInitialized = true;
            
            SetupUI();
        }
        
        public void UpdateIndicators()
        {
            if (!_isInitialized || _targetPlant == null) return;
            
            // Update health indicators
            if (_healthSlider != null)
                _healthSlider.value = _targetPlant.CurrentHealth / _targetPlant.MaxHealth;
                
            if (_hydrationSlider != null)
                _hydrationSlider.value = _targetPlant.CurrentHydration / 100f;
                
            if (_nutritionSlider != null)
                _nutritionSlider.value = _targetPlant.CurrentNutrition / 100f;
                
            if (_stressIndicator != null)
            {
                float stress = _targetPlant.CurrentStressLevel / 100f;
                _stressIndicator.color = Color.Lerp(Color.green, Color.red, stress);
            }
        }
        
        public void UpdateHealthDisplay(InteractivePlant plant, float quality)
        {
            if (plant == null) return;
            
            _targetPlant = plant;
            UpdateIndicators();
            
            // Apply quality-based visual feedback
            if (quality > 0.8f)
            {
                // High quality care - positive feedback
                ShowPositiveFeedback();
            }
            else if (quality < 0.4f)
            {
                // Low quality care - negative feedback
                ShowNegativeFeedback();
            }
        }
        
        private void ShowPositiveFeedback()
        {
            // Visual feedback for good care
            if (_stressIndicator != null)
            {
                StartCoroutine(FlashIndicator(_stressIndicator, Color.green, 0.5f));
            }
        }
        
        private void ShowNegativeFeedback()
        {
            // Visual feedback for poor care
            if (_stressIndicator != null)
            {
                StartCoroutine(FlashIndicator(_stressIndicator, Color.red, 0.5f));
            }
        }
        
        private System.Collections.IEnumerator FlashIndicator(Image indicator, Color color, float duration)
        {
            Color originalColor = indicator.color;
            indicator.color = color;
            yield return new WaitForSeconds(duration);
            indicator.color = originalColor;
        }
        
        public void ShowHealthStatus(bool show)
        {
            if (_healthCanvas != null)
                _healthCanvas.gameObject.SetActive(show);
        }
        
        private void SetupUI()
        {
            if (_healthCanvas == null)
            {
                // Create health canvas if not assigned
                GameObject canvasGO = new GameObject("PlantHealthCanvas");
                canvasGO.transform.SetParent(transform);
                _healthCanvas = canvasGO.AddComponent<Canvas>();
                _healthCanvas.renderMode = RenderMode.WorldSpace;
            }
        }
        
        private void Update()
        {
            if (_isInitialized)
            {
                UpdateIndicators();
            }
        }
    }
}