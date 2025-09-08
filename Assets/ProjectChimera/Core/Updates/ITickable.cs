using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core.Updates
{
    /// <summary>
    /// Interface for objects that need to be updated each frame
    /// Replaces MonoBehaviour.Update() with centralized tick management
    /// Part of Phase 0.5 Central Update Bus implementation
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// Called every frame by the UpdateOrchestrator
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        void Tick(float deltaTime);
        
        /// <summary>
        /// Priority for update order (lower numbers execute first)
        /// System priorities: -100 to -1
        /// Gameplay priorities: 0 to 99
        /// UI priorities: 100 to 199
        /// Effects priorities: 200+
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this tickable should be updated this frame
        /// </summary>
        bool Enabled { get; }
        
        /// <summary>
        /// Optional: Called when tickable is registered with UpdateOrchestrator
        /// </summary>
        void OnRegistered() { }
        
        /// <summary>
        /// Optional: Called when tickable is unregistered from UpdateOrchestrator
        /// </summary>
        void OnUnregistered() { }
    }
    
    /// <summary>
    /// Standard priority levels for different system types
    /// </summary>
    public static class TickPriority
    {
        // System Core (Critical foundation systems)
        public const int TimeManager = -100;
        public const int EventManager = -90;
        public const int InputSystem = -80;
        
        // Data & Persistence
        public const int DataManager = -70;
        public const int SaveSystem = -60;
        
        // Game Systems (Order matters for simulation accuracy)
        public const int EnvironmentalManager = -50;
        public const int PlantLifecycle = -40;
        public const int GeneticsManager = -30;
        public const int CultivationManager = -20;
        public const int EconomyManager = -10;
        
        // Gameplay Systems
        public const int FacilityManager = -5;
        public const int ConstructionSystem = 0;
        public const int GridPlacement = 10;
        public const int InteractionSystem = 20;
        public const int ProgressionManager = 30;
        public const int ResearchManager = 40;
        
        // UI Systems
        public const int UIManager = 100;
        public const int MenuSystems = 110;
        public const int HUD = 120;
        public const int Notifications = 130;
        
        // Effects & Animation
        public const int ParticleEffects = 200;
        public const int AudioManager = 210;
        public const int CameraEffects = 220;
        
        // Specialized Services
        public const int SpeedTreeServices = 250;
        
        // Analytics & Monitoring
        public const int AnalyticsManager = 290;
        
        // Debug & Development
        public const int DebugSystems = 1000;
    }
    
    /// <summary>
    /// Base implementation of ITickable for common scenarios
    /// Provides default implementations and utility methods
    /// </summary>
    public abstract class TickableBase : ITickable
    {
        protected bool _enabled = true;
        protected int _priority = 0;
        
        public virtual int Priority => _priority;
        public virtual bool Enabled => _enabled;
        
        public abstract void Tick(float deltaTime);
        
        public virtual void OnRegistered() { }
        public virtual void OnUnregistered() { }
        
        /// <summary>
        /// Enable/disable ticking
        /// </summary>
        public virtual void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
        
        /// <summary>
        /// Change tick priority (requires re-registration with UpdateOrchestrator)
        /// </summary>
        protected void SetPriority(int priority)
        {
            _priority = priority;
        }
    }
    
    /// <summary>
    /// ITickable implementation for MonoBehaviour components
    /// Allows existing MonoBehaviours to opt into centralized update system
    /// </summary>
    public abstract class TickableMonoBehaviour : MonoBehaviour, ITickable
    {
        [Header("Tickable Settings")]
        [SerializeField] protected int _priority = 0;
        [SerializeField] protected bool _enabled = true;
        
        public virtual int Priority => _priority;
        public virtual bool Enabled => _enabled && gameObject.activeInHierarchy;
        
        public abstract void Tick(float deltaTime);
        
        public virtual void OnRegistered() { }
        public virtual void OnUnregistered() { }
        
        protected virtual void OnEnable()
        {
            // Register with UpdateOrchestrator when enabled
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.RegisterTickable(this);
        }
        
        protected virtual void OnDisable()
        {
            // Unregister from UpdateOrchestrator when disabled
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.UnregisterTickable(this);
        }
        
        /// <summary>
        /// Override Update to prevent accidental usage
        /// Forces developers to use Tick() instead
        /// </summary>
        private void Update()
        {
            // This should never be called since we're using centralized updates
            // If you see this warning, remove Update() method and use Tick() instead
            ChimeraLogger.LogWarning($"[{GetType().Name}] Update() called on TickableMonoBehaviour - use Tick() instead", this);
        }
    }
    
    /// <summary>
    /// Interface for systems that need fixed timestep updates (physics, simulation)
    /// </summary>
    public interface IFixedTickable
    {
        /// <summary>
        /// Called at fixed intervals by the UpdateOrchestrator
        /// </summary>
        /// <param name="fixedDeltaTime">Fixed time step interval</param>
        void FixedTick(float fixedDeltaTime);
        
        /// <summary>
        /// Priority for fixed update order
        /// </summary>
        int FixedPriority { get; }
        
        /// <summary>
        /// Whether this fixed tickable should be updated
        /// </summary>
        bool FixedEnabled { get; }
    }
    
    /// <summary>
    /// Interface for systems that need late update (after all normal updates)
    /// </summary>
    public interface ILateTickable
    {
        /// <summary>
        /// Called after all normal Tick() methods have completed
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        void LateTick(float deltaTime);
        
        /// <summary>
        /// Priority for late update order
        /// </summary>
        int LatePriority { get; }
        
        /// <summary>
        /// Whether this late tickable should be updated
        /// </summary>
        bool LateEnabled { get; }
    }
}