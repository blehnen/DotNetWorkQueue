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
using System.Linq;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// A scope that allows components to still exist after the container has been disposed.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ICreationScope" />
    public class CreationScope : ICreationScope
    {
        private ConcurrentBag<IDisposable> _disposables;
        private ConcurrentBag<IClear> _clears;

        /// <summary>
        /// Adds the scoped Disposable object to the scope.
        /// </summary>
        /// <remarks>All objects added here will be disposed of when the scope is disposed</remarks>
        /// <param name="disposable">The disposable.</param>
        public void AddScopedObject(IDisposable disposable)
        {
            if (_disposables == null)
                _disposables = new ConcurrentBag<IDisposable>();

            _disposables.Add(disposable);
        }

        /// <summary>
        /// Adds the scoped object.
        /// </summary>
        /// <param name="input">The input.</param>
        public void AddScopedObject(IClear input)
        {
            if (_clears == null)
                _clears = new ConcurrentBag<IClear>();

            _clears.Add(input);
        }

        ///<inheritdoc/>
        public T GetDisposable<T>()
            where T : class, IDisposable
        {
            if (_disposables == null)
                return null;

            return _disposables.Where(item => item.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the contained objects.
        /// </summary>
        /// <value>
        /// The contained objects.
        /// </value>
        public ConcurrentBag<IDisposable> ContainedDisposables => _disposables;

        /// <summary>
        /// Gets the contained objects.
        /// </summary>
        /// <value>
        /// The contained objects.
        /// </value>
        public ConcurrentBag<IClear> ContainedClears => _clears;

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                if (_disposables != null)
                {
                    foreach (var obj in _disposables)
                    {
                        obj.Dispose();
                    }
                    _disposables = null;
                }

                if (_clears != null)
                {
                    foreach (var obj in _clears)
                    {
                        obj.Clear();
                    }
                    _clears = null;
                }
            }
            _disposedValue = true;
        }
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
