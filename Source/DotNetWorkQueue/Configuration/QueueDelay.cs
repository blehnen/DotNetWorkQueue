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
using System.Collections;
using System.Collections.Generic;
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Defines the worker thread delay behavior when no records are found to process, or if a serious transport exception has occurred.
    /// </summary>
    public class QueueDelay : IQueueDelay
    {
        private readonly List<TimeSpan> _delayTimes = new List<TimeSpan>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDelay"/> class.
        /// </summary>
        public QueueDelay()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDelay"/> class.
        /// </summary>
        /// <param name="delays">The delays.</param>
        public QueueDelay(IEnumerable<TimeSpan> delays)
        {
            _delayTimes.AddRange(delays);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TimeSpan> GetEnumerator()
        {
            return _delayTimes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds the specified delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public void Add(TimeSpan delay)
        {
            FailIfReadOnly();
            _delayTimes.Add(delay);
        }

        /// <summary>
        /// Adds the specified delays.
        /// </summary>
        /// <param name="delays">The delays.</param>
        public void Add(IEnumerable<TimeSpan> delays)
        {
            FailIfReadOnly();
            _delayTimes.AddRange(delays);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _delayTimes.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }
}
