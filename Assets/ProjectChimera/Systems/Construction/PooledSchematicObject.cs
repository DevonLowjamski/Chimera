using UnityEngine;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Component for objects that are managed by the schematic pooling system.
    /// Handles pool lifecycle events and maintains pool identification.
    /// </summary>
    public class PooledSchematicObject : MonoBehaviour
    {
        [Header("Pool Information")]
        [SerializeField] private string _poolKey;
        [SerializeField] private SchematicItem _sourceItem;
        [SerializeField] private bool _isActiveFromPool;
        
        [Header("Pool Events")]
        [SerializeField] private bool _resetTransformOnReturn = true;
        [SerializeField] private bool _resetComponentsOnReturn = true;
        
        // Properties
        public string PoolKey => _poolKey;
        public SchematicItem SourceItem => _sourceItem;
        public bool IsActiveFromPool => _isActiveFromPool;
        
        // Events
        public System.Action<PooledSchematicObject> OnReturnedToPoolEvent;
        public System.Action<PooledSchematicObject> OnActivatedFromPoolEvent;
        
        /// <summary>
        /// Initialize the pooled object with pool information
        /// </summary>
        public void Initialize(string poolKey, SchematicItem sourceItem)
        {
            _poolKey = poolKey;
            _sourceItem = sourceItem;
            _isActiveFromPool = false;
        }
        
        /// <summary>
        /// Called when object is activated from pool
        /// </summary>
        public void OnActivatedFromPool(SchematicItem item)
        {
            _isActiveFromPool = true;
            _sourceItem = item;
            
            // Reset any cached components
            RefreshComponents();
            
            OnActivatedFromPoolEvent?.Invoke(this);
        }
        
        /// <summary>
        /// Called when object is returned to pool
        /// </summary>
        public void OnReturnedToPool()
        {
            _isActiveFromPool = false;
            
            // Reset transform if needed
            if (_resetTransformOnReturn)
            {
                ResetTransform();
            }
            
            // Reset components if needed
            if (_resetComponentsOnReturn)
            {
                ResetComponents();
            }
            
            OnReturnedToPoolEvent?.Invoke(this);
        }
        
        /// <summary>
        /// Reset transform to default state
        /// </summary>
        private void ResetTransform()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// Reset components to default state
        /// </summary>
        private void ResetComponents()
        {
            // Reset animator if present
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            
            // Reset rigidbody if present
            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true; // Default to kinematic for construction objects
            }
            
            // Reset particle systems if present
            var particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Clear();
                ps.Stop();
            }
            
            // Reset audio sources if present
            var audioSources = GetComponentsInChildren<AudioSource>();
            foreach (var audio in audioSources)
            {
                audio.Stop();
            }
        }
        
        /// <summary>
        /// Refresh components after activation
        /// </summary>
        private void RefreshComponents()
        {
            // Enable all renderers
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
            
            // Enable all colliders
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }
        }
        
        /// <summary>
        /// Get pool statistics for this object
        /// </summary>
        public PoolObjectStats GetStats()
        {
            return new PoolObjectStats
            {
                PoolKey = _poolKey,
                ObjectName = name,
                IsActive = _isActiveFromPool,
                SourceItemName = _sourceItem?.ItemName ?? "Unknown",
                ActivationCount = GetActivationCount()
            };
        }
        
        /// <summary>
        /// Get activation count from component or estimate
        /// </summary>
        private int GetActivationCount()
        {
            // This could be tracked over time, for now return a placeholder
            return _isActiveFromPool ? 1 : 0;
        }
    }
    
    /// <summary>
    /// Statistics for pooled objects
    /// </summary>
    [System.Serializable]
    public struct PoolObjectStats
    {
        public string PoolKey;
        public string ObjectName;
        public bool IsActive;
        public string SourceItemName;
        public int ActivationCount;
    }
}