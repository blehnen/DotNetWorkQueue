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

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Base configuration for a time client
    /// </summary>
    public class BaseTimeConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTimeConfiguration"/> class.
        /// </summary>
        public BaseTimeConfiguration()
        {
            RefreshTime = TimeSpan.FromSeconds(900);
        }
        /// <summary>
        /// How often to obtain the time from the time source
        /// </summary>
        /// <value>
        /// The refresh time.
        /// </value>
        /// <remarks>Defaults to 900 seconds</remarks>
        public TimeSpan RefreshTime { get; set; }
    }
}
