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
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Allows a scheduler to indicate that it's full and cannot accept more work
    /// </summary>
    internal class WaitForEventOrCancelThreadPool : IWaitForEventOrCancelThreadPool
    {
        private readonly ConcurrentDictionary<IWorkGroup, IWaitForEventOrCancel> _waitForEventForGroups;
        private readonly Lazy<IWaitForEventOrCancel> _waitForEvent;
        private readonly IWaitForEventOrCancelFactory _waitForEventOrCancelFactory;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForEventOrCancelThreadPool"/> class.
        /// </summary>
        /// <param name="waitForEventOrCancelFactory">The wait for event or cancel factory.</param>
        public WaitForEventOrCancelThreadPool(IWaitForEventOrCancelFactory waitForEventOrCancelFactory)
        {
            Guard.NotNull(() => waitForEventOrCancelFactory, waitForEventOrCancelFactory);
            _waitForEventForGroups = new ConcurrentDictionary<IWorkGroup, IWaitForEventOrCancel>();
            _waitForEventOrCancelFactory = waitForEventOrCancelFactory;
            _waitForEvent = new Lazy<IWaitForEventOrCancel>(_waitForEventOrCancelFactory.Create);
        }

        /// <summary>
        /// Waits until notified to stop waiting.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public bool Wait(IWorkGroup group)
        {
            ThrowIfDisposed();

            if (group == null)
            {
                return _waitForEvent.Value.Wait();
            }

            return GetOrAddGroup(group).Wait();
        }

        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        /// <param name="group">The group.</param>
        public void Reset(IWorkGroup group)
        {
            ThrowIfDisposed();

            if (group == null)
            {
                _waitForEvent.Value.Reset();
            }
            else
            {
                GetOrAddGroup(group).Reset();
            }
        }

        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        /// <param name="group">The group.</param>
        public void Set(IWorkGroup group)
        {
            ThrowIfDisposed();

            if (group == null)
            {
                _waitForEvent.Value.Set();
            }
            else
            {
                Guard.NotNull(() => group, group);
                GetOrAddGroup(group).Set();
            }
        }

        /// <summary>
        /// Cancels any current <see cref="Wait" /> calls
        /// </summary>
        public void Cancel()
        {
            ThrowIfDisposed();

            _waitForEvent.Value.Cancel();
            _waitForEventForGroups.AsParallel().ForAll(w => w.Value.Cancel());
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            GC.SuppressFinalize(this);
            if (_waitForEvent.IsValueCreated)
            {
                _waitForEvent.Value.Dispose();
            }
            _waitForEventForGroups.AsParallel().ForAll(w => w.Value.Dispose());
            _waitForEventForGroups.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion

        /// <summary>
        /// Gets an existing wait event for a group, or creates and adds one atomically.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns>The wait event for this group.</returns>
        private IWaitForEventOrCancel GetOrAddGroup(IWorkGroup group)
        {
            if (_waitForEventForGroups.TryGetValue(group, out var existing))
            {
                return existing;
            }

            var newWaitEvent = _waitForEventOrCancelFactory.Create();
            if (_waitForEventForGroups.TryAdd(group, newWaitEvent))
            {
                return newWaitEvent;
            }

            //already added by another thread, nuke the one we just created
            newWaitEvent.Dispose();
            return _waitForEventForGroups[group];
        }
    }
}
