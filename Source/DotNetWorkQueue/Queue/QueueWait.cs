// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Delays querying the transport for a length of time. 
    /// </summary>
    internal class QueueWait : IQueueWait
    {
        private readonly TimeSpan[] _backoffTimes;
        private readonly ICancelWork _tokenWorkerCanceled;
        private long _currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueWait"/> class.
        /// </summary>
        /// <param name="backoffTimes">The back off times.</param>
        /// <param name="tokenWorkerCanceled">The cancel token.</param>
        /// <exception cref="System.ArgumentException">
        /// Back off helper must be initialized with at least one time span!;backoffTimes
        /// or
        /// backoffTimes
        /// </exception>
        public QueueWait(IEnumerable<TimeSpan> backoffTimes, ICancelWork tokenWorkerCanceled)
        {
            Guard.NotNull(() => tokenWorkerCanceled, tokenWorkerCanceled);
            var timeSpans = backoffTimes.ToArray();
            Guard.NotNull(() => timeSpans, timeSpans);
            if(timeSpans.Length == 0)
               throw new ArgumentException("Back off helper must be initialized with at least one time span");

            _backoffTimes = timeSpans;
            _tokenWorkerCanceled = tokenWorkerCanceled;
        }

        /// <summary>
        /// Resets the wait time back to the start.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _currentIndex, 0);
        }

        /// <summary>
        /// Waits until the next wait time has expired
        /// </summary>
        public void Wait()
        {
            Wait(_ => { });
        }

        /// <summary>
        /// Waits until the specified time span has been reached.
        /// </summary>
        /// <param name="waitTime">The how long the wait will last.</param>
        public void Wait(Action<TimeSpan> waitTime)
        {
            var effectiveIndex = Math.Min(Interlocked.Read(ref _currentIndex), _backoffTimes.Length - 1);
            var timeToWait = _backoffTimes[effectiveIndex];

            waitTime(timeToWait);
            WaitInternal(timeToWait);

            Interlocked.Increment(ref _currentIndex);
        }
        /// <summary>
        /// Waits for the specified amount of time
        /// </summary>
        /// <param name="timeToWait">The time to wait.</param>
        private void WaitInternal(TimeSpan timeToWait)
        {
            //NOTE - we are only using the stop token, not the cancel token.
            //The stop token should always fire before the cancel token
            //however, that is enforced by implementation, not contract...
            _tokenWorkerCanceled.StopWorkToken.WaitHandle.WaitOne(timeToWait);
        }
    }
}
