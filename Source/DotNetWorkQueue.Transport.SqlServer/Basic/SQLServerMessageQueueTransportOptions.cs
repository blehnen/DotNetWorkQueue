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
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Schema;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Transport options. Generally speaking, this controls the feature set of the transport.
    /// </summary>
    public class SqlServerMessageQueueTransportOptions : ITransportOptions, IReadonly, ISetReadonly, IBaseTransportOptions
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
        /// Initializes a new instance of the <see cref="SqlServerMessageQueueTransportOptions" /> class.
        /// </summary>
        public SqlServerMessageQueueTransportOptions()
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

        #endregion

        #region Options
        /// <summary>
        /// Gets or sets a value indicating whether [enable priority].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable priority]; otherwise, <c>false</c>.
        /// </value>
        public bool EnablePriority
        {
            get => _enablePriority;
            set
            {
                FailIfReadOnly();
                _enablePriority = value;
            }
        }

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

        /// <summary>
        /// If true, a transaction will be held until the message is finished processing.
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable hold transaction until message committed]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableHoldTransactionUntilMessageCommitted
        {
            get => _enableHoldTransactionUntilMessageCommitted;
            set
            {
                FailIfReadOnly();
                _enableHoldTransactionUntilMessageCommitted = value;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable status].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableStatus
        {
            get => _enableStatus;
            set
            {
                FailIfReadOnly();
                _enableStatus = value;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable heart beat].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable heart beat]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableHeartBeat
        {
            get => _enableHeartBeat;
            set
            {
                FailIfReadOnly();
                _enableHeartBeat = value;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable delayed processing].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable delayed processing]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableDelayedProcessing
        {
            get => _enableDelayedProcessing;
            set
            {
                FailIfReadOnly();
                _enableDelayedProcessing = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable status table].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status table]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableStatusTable
        {
            get => _enableStatusTable || AdditionalColumns.Count > 0;
            set
            {
                FailIfReadOnly();
                _enableStatusTable = value;
            }
        }

        /// <summary>
        /// Optional ceiling for the number of messages placed in a single batched multi-row
        /// insert when using the <c>Send(List&lt;...&gt;)</c> producer overloads. A value of 0
        /// (the default) uses the transport-computed safe maximum derived from the SQL Server
        /// command parameter limit. A configured value is treated as a ceiling only: it is
        /// clamped down to the safe maximum so it can never overflow the parameter budget, but
        /// may be set smaller to bound write-lock duration. Values below 0 are ignored.
        /// </summary>
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
        /// Gets or sets a value indicating whether routing is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable route]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableRoute
        {
            get => _enableRoute;
            set
            {
                FailIfReadOnly();
                _enableRoute = value;
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
        /// Gets or sets the history tracking options (retention, body storage, tracking flags).
        /// </summary>
        public HistoryTransportOptions HistoryOptions { get; set; } = new HistoryTransportOptions();

        /// <inheritdoc />
        IHistoryTransportOptions IBaseTransportOptions.HistoryOptions => HistoryOptions;

        /// <summary>
        /// Gets or sets the type of the queue.
        /// </summary>
        /// <value>
        /// The type of the queue.
        /// </value>
        public QueueTypes QueueType
        {
            get => _queueType;
            set
            {
                FailIfReadOnly();
                _queueType = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable message expiration].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable message expiration]; otherwise, <c>false</c>.
        /// </value>
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
                command.Append(", DATEADD(ms, @QueueProcessTime, GetUTCDate()) ");
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
                command.Append(", DATEADD(ms, @ExpirationTime, GetUTCDate()) ");
            }

        }

        /// <summary>
        /// Binds the delay and expiration offsets for the meta insert.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="AddBuiltInColumnsParams"/> because only the meta insert has
        /// these two columns - the status insert shares that method but has neither.
        /// <para>
        /// Only the <em>offset</em> is a parameter: the base time stays <c>GetUTCDate()</c>, so
        /// the server's clock still sets the value as it always has. Binding the computed time
        /// from the client instead would have moved the queue onto a different clock.
        /// </para>
        /// <para>
        /// The offsets used to be written into the statement as literals, so a queue with varying
        /// delays missed the send cache and made SQL Server compile a fresh plan per distinct
        /// value - see GitHub #255.
        /// </para>
        /// <para>
        /// Each parameter is named for the column it feeds rather than for the offset it carries,
        /// which is the convention the built-in columns already follow - <c>@Priority</c>,
        /// <c>@Route</c> and <c>@Status</c> are named the same way. It is what keeps them from
        /// colliding with a user's additional meta column: those bind as <c>"@" + column name</c>,
        /// and a column carrying a built-in name cannot exist alongside the built-in one.
        /// </para>
        /// </remarks>
        /// <param name="command">The command.</param>
        /// <param name="delay">The delay, if the message carries one.</param>
        /// <param name="expiration">The expiration, or <see cref="TimeSpan.Zero"/> for a message that never expires.</param>
        internal void AddBuiltInTimeParams(SqlCommand command, TimeSpan? delay, TimeSpan expiration)
        {
            if (EnableDelayedProcessing)
            {
                //no delay is an offset of zero, which is what a bare GetUTCDate() was
                command.Parameters.Add("@QueueProcessTime", SqlDbType.Int, 4).Value =
                    delay.HasValue && delay != TimeSpan.Zero
                        ? OffsetMilliseconds(delay.Value)
                        : 0;
            }

            if (EnableMessageExpiration)
            {
                //DATEADD returns NULL when its offset is NULL, which is the value the inlined
                //form wrote for a message that never expires
                command.Parameters.Add("@ExpirationTime", SqlDbType.Int, 4).Value =
                    expiration != TimeSpan.Zero
                        ? OffsetMilliseconds(expiration)
                        : (object)DBNull.Value;
            }
        }

        /// <summary>
        /// The offset in whole milliseconds, truncated rather than rounded.
        /// </summary>
        /// <remarks>
        /// The value used to reach SQL Server as a decimal literal - <c>DATEADD(ms,1.5,...)</c> -
        /// and SQL Server truncates when it converts that to the int the function takes, so 1.5 ms
        /// meant 1 ms. <c>Convert.ToInt32</c> would round it to 2 and quietly move the message.
        /// <para>
        /// Checked so that a delay too large for an int fails loudly. It failed before too, on the
        /// server, when <c>DATEADD</c> refused the out-of-range literal.
        /// </para>
        /// </remarks>
        /// <param name="value">The delay or expiration.</param>
        private static int OffsetMilliseconds(TimeSpan value) => checked((int)value.TotalMilliseconds);
        /// <summary>
        /// Adds the built in columns parameters.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        internal void AddBuiltInColumnsParams(SqlCommand command, IAdditionalMessageData data)
        {
            if (EnablePriority)
            {
                var priority = 0;
                if (data.GetPriority().HasValue)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    priority = data.GetPriority().Value;
                }
                command.Parameters.Add("@priority", SqlDbType.TinyInt, 1).Value = priority;
            }
            if (EnableRoute)
            {
                if (!string.IsNullOrEmpty(data.Route))
                {
                    command.Parameters.Add("@Route", SqlDbType.VarChar, 255).Value = data.Route;
                }
                else
                {
                    command.Parameters.Add("@Route", SqlDbType.VarChar, 255).Value = DBNull.Value;
                }
            }
            if (EnableStatus)
            {
                command.Parameters.Add("@Status", SqlDbType.Int, 4).Value = 0;
            }
        }
        #endregion
    }
}
