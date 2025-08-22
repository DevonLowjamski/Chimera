using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Generic object pool for UI elements in Project Chimera.
    /// Provides efficient reuse of UI elements to reduce allocation overhead.
    /// </summary>
    public abstract class UIElementPool
    {
        protected Queue<VisualElement> _pool;
        protected HashSet<VisualElement> _activeElements;
        protected int _initialSize;
        protected int _maxSize;
        protected int _growthRate;
        protected float _lastCleanupTime;
        
        public int PooledCount => _pool?.Count ?? 0;
        public int ActiveCount => _activeElements?.Count ?? 0;
        public int TotalCount => PooledCount + ActiveCount;
        public bool IsAtMaxCapacity => TotalCount >= _maxSize;
        
        public abstract VisualElement GetElement();
        public abstract void ReturnElement(VisualElement element);
        public abstract void Cleanup();
    }
    
    /// <summary>
    /// Typed object pool for specific UI element types
    /// </summary>
    public class UIElementPool<T> : UIElementPool where T : VisualElement, new()
    {
        private System.Func<T> _createFunction;
        private System.Action<T> _resetFunction;
        private System.Action<T> _destroyFunction;
        
        public UIElementPool(int initialSize = 10, int maxSize = 100, int growthRate = 5)
        {
            _initialSize = initialSize;
            _maxSize = maxSize;
            _growthRate = growthRate;
            _lastCleanupTime = Time.time;
            
            _pool = new Queue<VisualElement>();
            _activeElements = new HashSet<VisualElement>();
            
            _createFunction = () => new T();
            _resetFunction = ResetElement;
            _destroyFunction = DestroyElement;
            
            // Pre-populate pool
            for (int i = 0; i < _initialSize; i++)
            {
                var element = CreateElement();
                _pool.Enqueue(element);
            }
        }
        
        /// <summary>
        /// Get element from pool
        /// </summary>
        public T Get()
        {
            T element;
            
            if (_pool.Count > 0)
            {
                element = (T)_pool.Dequeue();
            }
            // else if (!IsAtMaxCapacity)
            // {
                element = CreateElement();
            // }
            // else
            // {
                // Pool is at max capacity, reuse oldest active element
                element = (T)_activeElements.First();
                _activeElements.Remove(element);
                _resetFunction(element);
            // }
            
            _activeElements.Add(element);
            PrepareElement(element);
            
            return element;
        }
        
        /// <summary>
        /// Return element to pool
        /// </summary>
        public void Return(T element)
        {
            if (element == null || !_activeElements.Contains(element))
                return;
            
            _activeElements.Remove(element);
            _resetFunction(element);
            
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(element);
            }
            // else
            // {
                _destroyFunction(element);
            // }
        }
        
        /// <summary>
        /// Create new element
        /// </summary>
        private T CreateElement()
        {
            var element = _createFunction();
            ConfigureNewElement(element);
            return element;
        }
        
        /// <summary>
        /// Configure newly created element
        /// </summary>
        private void ConfigureNewElement(T element)
        {
            element.name = $"Pooled_{typeof(T).Name}";
            element.userData = this;
            
            // Set initial state
            element.style.display = DisplayStyle.None;
            element.SetEnabled(true);
        }
        
        /// <summary>
        /// Prepare element for use
        /// </summary>
        private void PrepareElement(T element)
        {
            element.style.display = DisplayStyle.Flex;
            element.SetEnabled(true);
            
            // Clear any event handlers that might have been attached
            // Note: In a real implementation, you'd need to track and clear specific events
        }
        
        /// <summary>
        /// Reset element to default state
        /// </summary>
        private void ResetElement(T element)
        {
            if (element == null)
                return;
            
            // Reset visual properties
            element.style.display = DisplayStyle.None;
            element.style.visibility = Visibility.Visible;
            element.style.opacity = 1f;
            
            // Reset transform properties
            element.style.translate = new Translate(new Length(0), new Length(0));
            element.style.rotate = new Rotate(new Angle(0));
            element.style.scale = new Scale(Vector2.one);
            
            // Reset layout properties
            element.style.position = Position.Relative;
            element.style.left = StyleKeyword.Auto;
            element.style.top = StyleKeyword.Auto;
            element.style.right = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;
            element.style.width = StyleKeyword.Auto;
            element.style.height = StyleKeyword.Auto;
            
            // Reset content
            if (element is Label label)
            {
                label.text = string.Empty;
            }
            if (element is Button button)
            {
                button.text = string.Empty;
            }
            else if (element is TextField textField)
            {
                textField.value = string.Empty;
            }
            
            // Clear children
            element.Clear();
            
            // Remove from hierarchy
            element.RemoveFromHierarchy();
            
            // Reset classes
            element.ClearClassList();
        }
        
        /// <summary>
        /// Destroy element (cleanup resources)
        /// </summary>
        private void DestroyElement(T element)
        {
            if (element == null)
                return;
            
            // Remove from hierarchy
            element.RemoveFromHierarchy();
            
            // Clear references
            element.userData = null;
            
            // Note: VisualElement doesn't have explicit disposal in Unity UI Toolkit
            // The garbage collector will handle cleanup when references are removed
        }
        
        /// <summary>
        /// Cleanup unused pooled elements
        /// </summary>
        public override void Cleanup()
        {
            var currentTime = Time.time;
            var timeSinceLastCleanup = currentTime - _lastCleanupTime;
            
            // Only cleanup if enough time has passed
            if (timeSinceLastCleanup < 30f) // 30 seconds
                return;
            
            // Calculate how many elements to remove (keep at least minimum)
            var targetSize = Mathf.Max(_initialSize, _pool.Count / 2);
            var elementsToRemove = _pool.Count - targetSize;
            
            for (int i = 0; i < elementsToRemove && _pool.Count > _initialSize; i++)
            {
                var element = (T)_pool.Dequeue();
                _destroyFunction(element);
            }
            
            _lastCleanupTime = currentTime;
        }
        
        /// <summary>
        /// Grow pool by specified amount
        /// </summary>
        public void Grow(int count = -1)
        {
            if (count <= 0)
                count = _growthRate;
            
            for (int i = 0; i < count && TotalCount < _maxSize; i++)
            {
                var element = CreateElement();
                _pool.Enqueue(element);
            }
        }
        
        /// <summary>
        /// Shrink pool to specified size
        /// </summary>
        public void Shrink(int targetSize)
        {
            targetSize = Mathf.Max(_initialSize, targetSize);
            
            while (_pool.Count > targetSize)
            {
                var element = (T)_pool.Dequeue();
                _destroyFunction(element);
            }
        }
        
        /// <summary>
        /// Clear all pooled elements
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var element = (T)_pool.Dequeue();
                _destroyFunction(element);
            }
            
            // Note: Active elements are left as-is since they're in use
        }
        
        /// <summary>
        /// Force return all active elements
        /// </summary>
        public void ForceReturnAll()
        {
            var activeElementsCopy = new List<VisualElement>(_activeElements);
            
            foreach (T element in activeElementsCopy)
            {
                Return(element);
            }
        }
        
        /// <summary>
        /// Get pool statistics
        /// </summary>
        public UIPoolStatistics GetStatistics()
        {
            return new UIPoolStatistics
            {
                ElementType = typeof(T).Name,
                PooledCount = PooledCount,
                ActiveCount = ActiveCount,
                TotalCount = TotalCount,
                MaxSize = _maxSize,
                UtilizationRate = ActiveCount / (float)TotalCount,
                EfficiencyRate = (TotalCount - PooledCount) / (float)TotalCount,
                LastCleanupTime = _lastCleanupTime
            };
        }
        
        // Base class overrides
        public override VisualElement GetElement()
        {
            return Get();
        }
        
        public override void ReturnElement(VisualElement element)
        {
            if (element is T typedElement)
            {
                Return(typedElement);
            }
        }
    }
    
    /// <summary>
    /// Specialized pool for complex UI components
    /// </summary>
    public class UIComponentPool<T> : UIElementPool<T> where T : VisualElement, new()
    {
        private Dictionary<T, UIComponentData> _componentData;
        private System.Action<T> _componentInitializer;
        private System.Action<T> _componentResetter;
        
        public UIComponentPool(int initialSize = 10, int maxSize = 100, int growthRate = 5,
                              System.Action<T> initializer = null, System.Action<T> resetter = null)
            : base(initialSize, maxSize, growthRate)
        {
            _componentData = new Dictionary<T, UIComponentData>();
            _componentInitializer = initializer;
            _componentResetter = resetter;
        }
        
        /// <summary>
        /// Get component with initialization
        /// </summary>
        public T GetComponent()
        {
            var component = Get();
            
            if (!_componentData.ContainsKey(component))
            {
                _componentData[component] = new UIComponentData
                {
                    CreationTime = Time.time,
                    UsageCount = 0,
                    LastUsedTime = Time.time
                };
                
                _componentInitializer?.Invoke(component);
            }
            
            var data = _componentData[component];
            data.UsageCount++;
            data.LastUsedTime = Time.time;
            
            return component;
        }
        
        /// <summary>
        /// Return component with reset
        /// </summary>
        public void ReturnComponent(T component)
        {
            _componentResetter?.Invoke(component);
            Return(component);
        }
        
        /// <summary>
        /// Get component usage statistics
        /// </summary>
        public UIComponentStats GetComponentStats(T component)
        {
            if (!_componentData.TryGetValue(component, out var data))
            {
                return new UIComponentStats();
            }
            
            return new UIComponentStats
            {
                CreationTime = data.CreationTime,
                UsageCount = data.UsageCount,
                LastUsedTime = data.LastUsedTime,
                Age = Time.time - data.CreationTime,
                IdleTime = Time.time - data.LastUsedTime
            };
        }
        
        /// <summary>
        /// Clean up unused components based on usage patterns
        /// </summary>
        public void CleanupByUsage(float maxIdleTime = 60f)
        {
            var currentTime = Time.time;
            var elementsToRemove = new List<T>();
            
            // Find unused pooled elements
            var pooledElements = new List<T>();
            var tempQueue = new Queue<VisualElement>();
            
            while (_pool.Count > 0)
            {
                var element = _pool.Dequeue();
                pooledElements.Add((T)element);
                tempQueue.Enqueue(element);
            }
            
            // Restore pool
            while (tempQueue.Count > 0)
            {
                _pool.Enqueue(tempQueue.Dequeue());
            }
            
            // Check usage patterns
            foreach (var element in pooledElements)
            {
                if (_componentData.TryGetValue(element, out var data))
                {
                    var idleTime = currentTime - data.LastUsedTime;
                    if (idleTime > maxIdleTime && PooledCount > _initialSize)
                    {
                        elementsToRemove.Add(element);
                    }
                }
            }
            
            // Remove unused elements
            foreach (var element in elementsToRemove)
            {
                var tempQueue2 = new Queue<VisualElement>();
                
                while (_pool.Count > 0)
                {
                    var pooledElement = _pool.Dequeue();
                    if (pooledElement != element)
                    {
                        tempQueue2.Enqueue(pooledElement);
                    }
                }
                
                while (tempQueue2.Count > 0)
                {
                    _pool.Enqueue(tempQueue2.Dequeue());
                }
                
                _componentData.Remove(element);
            }
        }
    }
    
    /// <summary>
    /// UI update batch for batching element updates
    /// </summary>
    public class UIUpdateBatch
    {
        private Queue<IUIUpdatable> _updateQueue;
        private UIUpdatePriority _priority;
        private int _maxBatchSize;
        private float _lastProcessTime;
        
        public UIUpdatePriority Priority => _priority;
        public int QueuedCount => _updateQueue.Count;
        public bool HasElements => _updateQueue.Count > 0;
        
        public UIUpdateBatch(UIUpdatePriority priority, int maxBatchSize = 50)
        {
            _priority = priority;
            _maxBatchSize = maxBatchSize;
            _updateQueue = new Queue<IUIUpdatable>();
            _lastProcessTime = Time.time;
        }
        
        /// <summary>
        /// Add element to batch
        /// </summary>
        public void AddElement(IUIUpdatable element)
        {
            if (_updateQueue.Count < _maxBatchSize)
            {
                _updateQueue.Enqueue(element);
            }
        }
        
        /// <summary>
        /// Process batch with time limit
        /// </summary>
        public void ProcessBatch(float maxTimeSeconds)
        {
            var startTime = Time.realtimeSinceStartup;
            var processedCount = 0;
            
            while (_updateQueue.Count > 0 && (Time.realtimeSinceStartup - startTime) < maxTimeSeconds)
            {
                var element = _updateQueue.Dequeue();
                try
                {
                    element.UpdateElement();
                    processedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error updating UI element: {ex.Message}");
                }
            }
            
            _lastProcessTime = Time.time;
        }
        
        /// <summary>
        /// Clear all queued elements
        /// </summary>
        public void Clear()
        {
            _updateQueue.Clear();
        }
    }
    
    /// <summary>
    /// Component data tracking
    /// </summary>
    public class UIComponentData
    {
        public float CreationTime;
        public int UsageCount;
        public float LastUsedTime;
    }
    
    /// <summary>
    /// Pool statistics
    /// </summary>
    public struct UIPoolStatistics
    {
        public string ElementType;
        public int PooledCount;
        public int ActiveCount;
        public int TotalCount;
        public int MaxSize;
        public float UtilizationRate;
        public float EfficiencyRate;
        public float LastCleanupTime;
    }
    
    /// <summary>
    /// Component statistics
    /// </summary>
    public struct UIComponentStats
    {
        public float CreationTime;
        public int UsageCount;
        public float LastUsedTime;
        public float Age;
        public float IdleTime;
    }
}