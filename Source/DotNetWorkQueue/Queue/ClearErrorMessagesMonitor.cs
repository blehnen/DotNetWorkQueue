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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Monitor process for removing messages in error
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Queue.BaseMonitor" />
    /// <seealso cref="DotNetWorkQueue.IClearErrorMessagesMonitor" />
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "not needed")]
    public class ClearErrorMessagesMonitor : BaseMonitor, IClearErrorMessagesMonitor
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesMonitor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="clearErrorMessages">The clear messages implementation.</param>
        /// <param name="log">The log.</param>
        public ClearErrorMessagesMonitor(IMessageErrorConfiguration configuration,
            IClearErrorMessages clearErrorMessages, ILogger log)
            : base(Guard.NotNull(() => clearErrorMessages, clearErrorMessages).ClearMessages, configuration, log)
        {

        }
        #endregion
    }
}
