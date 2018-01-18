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
using System.Collections.Generic;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contains time spans, used to delay query of the transport when no messages are found to process
    /// </summary>
    public interface IQueueDelay : IEnumerable<TimeSpan>, IReadonly, ISetReadonly
    {
        /// <summary>
        /// Adds the specified delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        void Add(TimeSpan delay);
        /// <summary>
        /// Adds the specified delays.
        /// </summary>
        /// <param name="delays">The delays.</param>
        void Add(IEnumerable<TimeSpan> delays);
        /// <summary>
        /// Clears this instance.
        /// </summary>
        void Clear();
    }
}
