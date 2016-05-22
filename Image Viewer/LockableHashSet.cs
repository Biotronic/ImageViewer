using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ImageViewer
{
    internal class LockableHashSet<T>
    {
        private readonly HashSet<T> _allTags= new HashSet<T>();
        private readonly Mutex mutex = new Mutex();

        public LockedList GetList()
        {
            return new LockedList(this);
        }

        public class LockedList : IDisposable, ISet<T>
        {
            private readonly LockableHashSet<T> _tags;
            private bool _disposed = false;

            public LockedList(LockableHashSet<T> tags)
            {
                _tags = tags;
                _tags.mutex.WaitOne();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _tags.mutex.ReleaseMutex();
                GC.SuppressFinalize(this);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _tags._allTags.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _tags._allTags.GetEnumerator();
            }

            public void Add(T item)
            {
                _tags._allTags.Add(item);
            }

            public void UnionWith(IEnumerable<T> other)
            {
                _tags._allTags.UnionWith(other);
            }

            public void IntersectWith(IEnumerable<T> other)
            {
                _tags._allTags.IntersectWith(other);
            }

            public void ExceptWith(IEnumerable<T> other)
            {
                _tags._allTags.ExceptWith(other);
            }

            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                _tags._allTags.SymmetricExceptWith(other);
            }

            public bool IsSubsetOf(IEnumerable<T> other)
            {
                return _tags._allTags.IsSubsetOf(other);
            }

            public bool IsSupersetOf(IEnumerable<T> other)
            {
                return _tags._allTags.IsSupersetOf(other);
            }

            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                return _tags._allTags.IsProperSupersetOf(other);
            }

            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                return _tags._allTags.IsProperSubsetOf(other);
            }

            public bool Overlaps(IEnumerable<T> other)
            {
                return _tags._allTags.Overlaps(other);
            }

            public bool SetEquals(IEnumerable<T> other)
            {
                return _tags._allTags.SetEquals(other);
            }

            bool ISet<T>.Add(T item)
            {
                return _tags._allTags.Add(item);
            }

            public void Clear()
            {
                _tags._allTags.Clear();
            }

            public bool Contains(T item)
            {
                return _tags._allTags.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _tags._allTags.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                return _tags._allTags.Remove(item);
            }

            public int Count => _tags._allTags.Count;
            public bool IsReadOnly => false;
        }
    }
}
