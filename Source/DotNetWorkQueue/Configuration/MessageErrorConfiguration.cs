// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    /// Configures removal of messages in error
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageErrorConfiguration" />
    public class MessageErrorConfiguration: IMessageErrorConfiguration
    {
        private TimeSpan _monitorTime;
        private TimeSpan _messageAge;
        private bool _enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageErrorConfiguration"/> class.
        /// </summary>
        public MessageErrorConfiguration()
        {
            Enabled = true; //enable by default, as queues filled with error messages will slow down
            MessageAge = TimeSpan.FromDays(30); //default to 30 days
            MonitorTime = TimeSpan.FromDays(1); //check every day
        }

        #region Configuration
        /// <inheritdoc />
        public TimeSpan MonitorTime
        {
            get => _monitorTime;
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
            }
        }

        /// <inheritdoc />
        public bool Enabled
        {
            get => _enabled;
            set
            {
                FailIfReadOnly();
                _enabled = value;
            }
        }

        /// <inheritdoc />
        public TimeSpan MessageAge
        {
            get => _messageAge;
            set
            {
                FailIfReadOnly();
                _messageAge = value;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
        #endregion
    }
}
