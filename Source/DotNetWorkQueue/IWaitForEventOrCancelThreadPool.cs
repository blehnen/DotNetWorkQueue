// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows a scheduler to indicate that it's full and cannot accept more work
    /// </summary>
    public interface IWaitForEventOrCancelThreadPool: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Waits until notified to stop waiting.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        bool Wait(IWorkGroup group);
        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        /// <param name="group">The group.</param>
        void Reset(IWorkGroup group);
        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        /// <param name="group">The group.</param>
        void Set(IWorkGroup group);
        /// <summary>
        /// Cancels any current <see cref="Wait" /> calls
        /// </summary>
        void Cancel();
    }
}
