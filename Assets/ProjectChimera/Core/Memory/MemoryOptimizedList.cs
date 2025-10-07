using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core.Memory
{
    public class MemoryOptimizedList<T> : IList<T>, IDisposable
    {
        private T[] _items;
        private int _size;
        private int _capacity;
        private const int DefaultCapacity = 4;

        public int Count => _size;
        public int Capacity => _capacity;
        public bool IsReadOnly => false;

        public MemoryOptimizedList()
        {
            _items = Array.Empty<T>();
            _capacity = 0;
            _size = 0;
        }

        public MemoryOptimizedList(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            
            if (capacity == 0)
            {
                _items = Array.Empty<T>();
            }
            else
            {
                _items = new T[capacity];
            }
            _capacity = capacity;
            _size = 0;
        }

        public T this[int index]
        {
            get
            {
                if (index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index];
            }
            set
            {
                if (index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
                _items[index] = value;
            }
        }

        public void Add(T item)
        {
            if (_size == _capacity)
            {
                EnsureCapacity(_size + 1);
            }
            _items[_size++] = item;
        }

        public void Insert(int index, T item)
        {
            if (index > _size) throw new ArgumentOutOfRangeException(nameof(index));
            
            if (_size == _capacity)
            {
                EnsureCapacity(_size + 1);
            }

            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
            
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T); // Clear reference
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        /// <summary>
        /// Trim excess capacity to reduce memory usage
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(_capacity * 0.9);
            if (_size < threshold)
            {
                SetCapacity(_size);
            }
        }

        /// <summary>
        /// Reserve capacity without allocating if already sufficient
        /// </summary>
        public void Reserve(int capacity)
        {
            if (capacity > _capacity)
            {
                SetCapacity(capacity);
            }
        }

        private void EnsureCapacity(int min)
        {
            if (_capacity < min)
            {
                int newCapacity = _capacity == 0 ? DefaultCapacity : _capacity * 2;
                if (newCapacity < min) newCapacity = min;
                SetCapacity(newCapacity);
            }
        }

        private void SetCapacity(int value)
        {
            if (value != _capacity)
            {
                if (value > 0)
                {
                    T[] newItems = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(_items, 0, newItems, 0, _size);
                    }
                    _items = newItems;
                }
                else
                {
                    _items = Array.Empty<T>();
                }
                _capacity = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; i++)
            {
                yield return _items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            Clear();
            _items = null;
        }
    }
}