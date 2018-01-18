using System;
using System.Collections.Concurrent;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// A scope that allows components to still exist after the container has been disposed.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ICreationScope" />
    public class CreationScope: ICreationScope
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
