using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core.Memory
{
    public class MemoryPool<T> : IDisposable where T : class, new()
    {
        private readonly MemoryOptimizedQueue<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;

        public int Count => _pool.Count;
        public int MaxSize => _maxSize;

        public MemoryPool(int maxSize = 100, Func<T> factory = null, Action<T> resetAction = null)
        {
            _maxSize = maxSize;
            _pool = new MemoryOptimizedQueue<T>(Math.Min(maxSize, 16));
            _factory = factory ?? (() => new T());
            _resetAction = resetAction;
        }

        public T Get()
        {
            if (_pool.TryDequeue(out T item))
            {
                return item;
            }
            return _factory();
        }

        public void Return(T item)
        {
            if (item == null || _pool.Count >= _maxSize)
                return;

            _resetAction?.Invoke(item);
            _pool.Enqueue(item);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Dispose()
        {
            _pool?.Dispose();
        }
    }
}