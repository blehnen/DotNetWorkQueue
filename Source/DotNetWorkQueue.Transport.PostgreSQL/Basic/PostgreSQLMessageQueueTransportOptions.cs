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
using System.Text;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc cref="ITransportOptions" />
    public class PostgreSqlMessageQueueTransportOptions: ITransportOptions, IReadonly, ISetReadonly, IBaseTransportOptions
    {
        private bool _enableStatusTable;
        private bool _enablePriority;
        private bool _enableHoldTransactionUntilMessageCommitted;
        private bool _enableStatus;
        private bool _enableHeartBeat;
        private bool _enableDelayedProcessing;
        private QueueTypes _queueType;
        private bool _enableMessageExpiration;
        private bool _enableRoute;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueTransportOptions" /> class.
        /// </summary>
        public PostgreSqlMessageQueueTransportOptions()
        {
            EnableDelayedProcessing = false;
            EnableHeartBeat = true;
            EnableHoldTransactionUntilMessageCommitted = false;
            EnablePriority = false;
            EnableStatus = true;
            EnableMessageExpiration = false;
            QueueType = QueueTypes.Normal;
            EnableStatusTable = false;
            EnableRoute = false;

            AdditionalColumns = new ColumnList();
            AdditionalConstraints = new ConstraintList();
        }
        #endregion

        #region User Settings

        /// <summary>
        /// Additional columns that can be attached to the queue.
        /// </summary>
        /// <value>
        /// The additional columns.
        /// </value>
        /// <remarks>See <see cref="IAdditionalMessageData"/> for how to pass in data when saving messages </remarks>
        public ColumnList AdditionalColumns { get; }

        /// <summary>
        /// Additional constraints or indexes that can be attached to the queue.
        /// </summary>
        /// <value>
        /// The additional constraints.
        /// </value>
        public ConstraintList AdditionalConstraints { get; }

        #endregion

        #region Options
        /// <inheritdoc />
        public bool EnablePriority
        {
            get => _enablePriority;
            set
            {
                FailIfReadOnly();
                _enablePriority = value;
            }
        }
        /// <inheritdoc />
        public bool EnableHoldTransactionUntilMessageCommitted
        {
            get => _enableHoldTransactionUntilMessageCommitted;
            set
            {
                FailIfReadOnly();
                _enableHoldTransactionUntilMessageCommitted = value;
            }
        }
        /// <inheritdoc />
        public bool EnableStatus
        {
            get => _enableStatus;
            set
            {
                FailIfReadOnly();
                _enableStatus = value;
            }
        }
        /// <inheritdoc />
        public bool EnableHeartBeat
        {
            get => _enableHeartBeat;
            set
            {
                FailIfReadOnly();
                _enableHeartBeat = value;
            }
        }
        /// <inheritdoc />
        public bool EnableDelayedProcessing
        {
            get => _enableDelayedProcessing;
            set
            {
                FailIfReadOnly();
                _enableDelayedProcessing = value;
            }
        }

        /// <inheritdoc />
        public bool EnableRoute
        {
            get => _enableRoute;
            set
            {
                FailIfReadOnly();
                _enableRoute = value;
            }
        }

        /// <inheritdoc />
        public bool EnableStatusTable
        {
            get => _enableStatusTable || AdditionalColumns.Count > 0;
            set
            {
                FailIfReadOnly();
                _enableStatusTable = value;
            }
        }

        /// <inheritdoc />
        public QueueTypes QueueType
        {
            get => _queueType;
            set
            {
                FailIfReadOnly();
                _queueType = value;
            }
        }

        /// <inheritdoc />
        public bool EnableMessageExpiration
        {
            get => _enableMessageExpiration;
            set
            {
                FailIfReadOnly();
                _enableMessageExpiration = value;
            }
        }

        #endregion

        #region Validation
        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        /// <returns></returns>
        public Validation ValidConfiguration()
        {
            var v = new Validation();
            var sbErrors = new StringBuilder();
            v.Valid = true;

            if (EnableHoldTransactionUntilMessageCommitted)
            {
                if (EnableHeartBeat)
                {
                    sbErrors.AppendLine("[EnableHeartBeat] must be false when using transactions");
                }
                if (EnableStatus)
                {
                    sbErrors.AppendLine("[EnableStatus] must be false when using transactions. The status table may still be used.");
                }
            }

            v.ErrorMessage = sbErrors.ToString();
            if (!string.IsNullOrWhiteSpace(v.ErrorMessage))
                v.Valid = false;

            return v;
        }
        #endregion

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

        /// <summary>
        /// Configuration validation status
        /// </summary>
        public class Validation
        {
            /// <summary>
            /// Gets or sets a value indicating whether the configuration is valid.
            /// </summary>
            /// <value>
            ///   <c>true</c> if valid; otherwise, <c>false</c>.
            /// </value>
            public bool Valid { get; set; }
            /// <summary>
            /// Gets or sets the error message.
            /// </summary>
            /// <value>
            /// The error message.
            /// </value>
            public string ErrorMessage { get; set; }
        }

        #region Internal Methods
        /// <summary>
        /// Adds the built in columns.
        /// </summary>
        /// <param name="command">The command.</param>
        internal void AddBuiltInColumns(StringBuilder command)
        {
            if (EnableDelayedProcessing)
            {
                command.Append(", QueueProcessTime ");
            }

            if (EnablePriority)
            {
                command.Append(", Priority ");
            }

            if (EnableRoute)
            {
                command.Append(", Route ");
            }

            if (EnableStatus)
            {
                command.Append(", Status ");
            }

            if (EnableMessageExpiration)
            {
                command.Append(", ExpirationTime ");
            }
        }
        /// <summary>
        /// Adds the built in column values.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="currentDateTime">The current date time.</param>
        /// <param name="command">The command.</param>
        internal void AddBuiltInColumnValues(TimeSpan? delay, TimeSpan expiration, DateTime currentDateTime, StringBuilder command)
        {
            if (EnableDelayedProcessing)
            {
                if (delay.HasValue && delay != TimeSpan.Zero)
                {
                    command.Append($", {currentDateTime.Add(delay.Value).Ticks} ");
                }
                else
                {
                    command.Append($", {currentDateTime.Ticks}");
                }
            }

            if (EnablePriority)
            {
                command.Append(", @Priority ");
            }

            if (EnableRoute)
            {
                command.Append(", @Route ");
            }

            if (EnableStatus)
            {
                command.Append(", @Status ");
            }

            if (EnableMessageExpiration)
            {
                command.Append(expiration != TimeSpan.Zero ? $", {currentDateTime.Add(expiration).Ticks} " : ", NULL ");
            }

        }
        /// <summary>
        /// Adds the built in columns parameters.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        internal void AddBuiltInColumnsParams(NpgsqlCommand command, IAdditionalMessageData data)
        {
            if (EnablePriority)
            {
                var priority = 0;
                if (data.GetPriority().HasValue)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    priority = data.GetPriority().Value;
                }
                command.Parameters.Add("@priority", NpgsqlDbType.Integer, 1).Value = priority;
            }
            if (EnableRoute)
            {
                if (!string.IsNullOrEmpty(data.Route))
                {
                    command.Parameters.Add("@Route", NpgsqlDbType.Varchar, 255).Value = data.Route;
                }
                else
                {
                    command.Parameters.Add("@Route", NpgsqlDbType.Varchar, 255).Value = DBNull.Value;
                }
            }
            if (EnableStatus)
            {
                command.Parameters.Add("@Status", NpgsqlDbType.Integer, 4).Value = 0;
            }
        }
        #endregion
    }
}
