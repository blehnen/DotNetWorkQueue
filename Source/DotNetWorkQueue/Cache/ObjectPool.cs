// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Cache
{
    /// <summary>
    /// A simple object pooling class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="DotNetWorkQueue.IObjectPool{T}" />
    public class ObjectPool<T> : IObjectPool<T> where T : IPooledObject
    {
        private readonly ConcurrentBag<T> _objects;

        /// <summary>
        /// Initializes a new pool with specified factory method and minimum and maximum size.
        /// </summary>
        /// <param name="maximumPoolSize">The maximum pool size.</param>
        /// <param name="factory">The factory that will be used to create new objects.</param>
        public ObjectPool(int maximumPoolSize, Func<T> factory)
        {
            Guard.IsValid(() => maximumPoolSize, maximumPoolSize, i => i > 0, "Maximum pool size must be greater than 0");

            Factory = factory;
            MaximumPoolSize = maximumPoolSize;

            _objects = new ConcurrentBag<T>();
        }

        /// <summary>
        /// A factory for creating new objects for the pool.
        /// </summary>
        public Func<T> Factory { get; }

        /// <summary>
        /// Defines the maximum pool size
        /// </summary>
        public int MaximumPoolSize { get; }

        /// <summary>
        /// Returns an object from the pool, or a new object if the pool is full.
        /// </summary>
        /// <returns>
        /// A monitored object from the pool.
        /// </returns>
        public T GetObject()
        {
            return _objects.TryTake(out var value) ? value : Factory();
        }

        /// <summary>
        /// Returns the object to the pool, if the pool is not over the <seealso cref="P:DotNetWorkQueue.IObjectPool`1.MaximumPoolSize" />
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Objects that inherit from <see cref="IDisposable"/> will be disposed.</remarks>
        public void ReturnObject(T value)
        {
            if (_objects.Count < MaximumPoolSize)
            {
                value.ResetState();
                _objects.Add(value);
            }
            else if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _objects.TryTake(out var value);
                    while (value != null)
                    {
                        var disposable = value as IDisposable;
                        disposable?.Dispose();
                        _objects.TryTake(out value);
                    }
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
