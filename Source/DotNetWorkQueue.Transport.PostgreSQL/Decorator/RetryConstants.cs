﻿// ---------------------------------------------------------------------
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
namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <summary>
    /// Shared constants for the retry decorators
    /// </summary>
    internal class RetryConstants
    {
        /// <summary>
        /// The retry count
        /// </summary>
        public const int RetryCount = 3;
        /// <summary>
        /// The minimum wait in Milliseconds
        /// </summary>
        public const int MinWait = 100;
        /// <summary>
        /// The maximum wait in Milliseconds
        /// </summary>
        public const int MaxWait = 1000;
    }
}
