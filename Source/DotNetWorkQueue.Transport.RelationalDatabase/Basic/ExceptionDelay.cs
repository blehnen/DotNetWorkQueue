// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public static class ExceptionDelay
    {
        /// <summary>
        /// Gets the default fatal exception delay time spans
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TimeSpan> GetExceptionDelay()
        {
            var rc = new List<TimeSpan>(10)
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(13),
                TimeSpan.FromSeconds(21),
                TimeSpan.FromSeconds(34),
                TimeSpan.FromSeconds(55)
            };
            return rc;
        }
    }
}
