// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc cref="ITransportOptions" />
    public class PostgreSqlMessageQueueTransportOptions : ITransportOptions, IReadonly, ISetReadonly, IBaseTransportOptions
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
        private bool _additionalColumnsOnMetaData;
        private bool _enableHistory;
        private int _batchSize;

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
            AdditionalColumnsOnMetaData = false;
            EnableHistory = false;

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

        /// <summary>
        /// If true, <see cref="AdditionalColumns"/> and <see cref="AdditionalConstraints"/> will be created on the metadata table
        /// If false, they will be created on the status table
        /// </summary>
        public bool AdditionalColumnsOnMetaData
        {
            get => _additionalColumnsOnMetaData;
            set
            {
                FailIfReadOnly();
                _additionalColumnsOnMetaData = value;
            }
        }

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

        /// <summary>
        /// Gets or sets a value indicating whether message history tracking is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable history]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableHistory
        {
            get => _enableHistory;
            set
            {
                FailIfReadOnly();
                _enableHistory = value;
            }
        }

        /// <summary>
        /// Optional ceiling for the number of messages placed in a single batched insert when
        /// using the <c>Send(List&lt;...&gt;)</c> producer overloads. A value of 0 (the default)
        /// uses the transport-computed safe maximum. A configured value is treated as a ceiling
        /// only: it is clamped down to the safe maximum, but may be set smaller to bound the size
        /// and lock duration of the batch transaction. Values below 0 are ignored.
        /// </summary>
        /// <remarks>The batch body insert uses array parameters (<c>unnest</c>), so it is not
        /// constrained by the bound-parameter limit; the safe maximum is a transaction-size bound,
        /// not a parameter bound.</remarks>
        public int BatchSize
        {
            get => _batchSize;
            set
            {
                FailIfReadOnly();
                _batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the history tracking options (retention, body storage, tracking flags).
        /// </summary>
        public HistoryTransportOptions HistoryOptions { get; set; } = new HistoryTransportOptions();

        /// <inheritdoc />
        IHistoryTransportOptions IBaseTransportOptions.HistoryOptions => HistoryOptions;

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
        /// The option flags that decide the meta-insert SQL's shape, as a cache key.
        /// </summary>
        /// <remarks>
        /// Exactly the flags <see cref="AddBuiltInColumns"/> and
        /// <see cref="AddBuiltInColumnValues"/> branch on. If a flag is added to either of those,
        /// it belongs here too, or two different shapes will share one cached string.
        /// </remarks>
        internal string GetMetaSqlShape()
        {
            return string.Concat(
                EnableDelayedProcessing ? "d" : "-",
                EnablePriority ? "p" : "-",
                EnableRoute ? "r" : "-",
                EnableStatus ? "s" : "-",
                EnableMessageExpiration ? "e" : "-");
        }

        /// <summary>
        /// Adds the built in column values.
        /// </summary>
        /// <param name="command">The command.</param>
        internal void AddBuiltInColumnValues(StringBuilder command)
        {
            if (EnableDelayedProcessing)
            {
                command.Append(", @QueueProcessTime ");
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
                command.Append(", @ExpirationTime ");
            }

        }
        /// <summary>
        /// Binds the delay and expiration values for the meta insert.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="AddBuiltInColumnsParams"/> because only the meta insert has
        /// these two columns - the status insert shares that method but would be binding values
        /// its statement never names.
        /// <para>
        /// The values used to be written into the statement text, which made every send a
        /// distinct statement and put the send cache permanently out of reach for a queue with
        /// delayed processing on - see GitHub #255. Both columns are bigint .NET ticks, so there
        /// is no server-side expression to use instead the way SQL Server has one.
        /// </para>
        /// </remarks>
        /// <param name="command">The command.</param>
        /// <param name="delay">The delay, if the message carries one.</param>
        /// <param name="expiration">The expiration, or <see cref="TimeSpan.Zero"/> for a message that never expires.</param>
        /// <param name="currentDateTime">The current UTC time, which both values are relative to.</param>
        internal void AddBuiltInTimeParams(NpgsqlCommand command, TimeSpan? delay,
            TimeSpan expiration, DateTime currentDateTime)
        {
            if (EnableDelayedProcessing)
            {
                command.Parameters.Add("@QueueProcessTime", NpgsqlDbType.Bigint, 8).Value =
                    delay.HasValue && delay != TimeSpan.Zero
                        ? currentDateTime.Add(delay.Value).Ticks
                        : currentDateTime.Ticks;
            }

            if (EnableMessageExpiration)
            {
                //NULL for a message that never expires - what the inlined form wrote, and what
                //the de-queue reads
                command.Parameters.Add("@ExpirationTime", NpgsqlDbType.Bigint, 8).Value =
                    expiration != TimeSpan.Zero
                        ? currentDateTime.Add(expiration).Ticks
                        : (object)DBNull.Value;
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
