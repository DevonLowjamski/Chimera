using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core.Memory
{
    public class MemoryOptimizedQueue<T> : IEnumerable<T>, IDisposable
    {
        private T[] _array;
        private int _head;
        private int _tail;
        private int _size;
        private int _capacity;

        public int Count => _size;
        public int Capacity => _capacity;

        public MemoryOptimizedQueue() : this(4) { }

        public MemoryOptimizedQueue(int capacity)
        {
            _capacity = capacity;
            _array = new T[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void Enqueue(T item)
        {
            if (_size == _capacity)
            {
                Grow();
            }

            _array[_tail] = item;
            _tail = (_tail + 1) % _capacity;
            _size++;
        }

        public T Dequeue()
        {
            if (_size == 0) throw new InvalidOperationException("Queue is empty");

            T item = _array[_head];
            _array[_head] = default; // Clear reference
            _head = (_head + 1) % _capacity;
            _size--;

            return item;
        }

        public T Peek()
        {
            if (_size == 0) throw new InvalidOperationException("Queue is empty");
            return _array[_head];
        }

        public bool TryDequeue(out T result)
        {
            if (_size == 0)
            {
                result = default;
                return false;
            }

            result = Dequeue();
            return true;
        }

        public bool TryPeek(out T result)
        {
            if (_size == 0)
            {
                result = default;
                return false;
            }

            result = _array[_head];
            return true;
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_array, 0, _capacity);
                _head = 0;
                _tail = 0;
                _size = 0;
            }
        }

        private void Grow()
        {
            int newCapacity = _capacity * 2;
            T[] newArray = new T[newCapacity];

            if (_head < _tail)
            {
                Array.Copy(_array, _head, newArray, 0, _size);
            }
            else if (_size > 0)
            {
                Array.Copy(_array, _head, newArray, 0, _capacity - _head);
                Array.Copy(_array, 0, newArray, _capacity - _head, _tail);
            }

            _array = newArray;
            _head = 0;
            _tail = _size;
            _capacity = newCapacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; i++)
            {
                yield return _array[(_head + i) % _capacity];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            Clear();
            _array = null;
        }
    }
}