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
using System.Data;
using System.Data.SQLite;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Schema;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Transport options. Generally speaking, this controls the feature set of the transport.
    /// </summary>
    public class SqLiteMessageQueueTransportOptions: ITransportOptions, IReadonly, ISetReadonly
    {
        private bool _enableStatusTable;
        private bool _enablePriority;
        private bool _enableStatus;
        private bool _enableHeartBeat;
        private bool _enableDelayedProcessing;
        private QueueTypes _queueType;
        private bool _enableMessageExpiration;
        private bool _enableRoute;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueTransportOptions" /> class.
        /// </summary>
        public SqLiteMessageQueueTransportOptions()
        {
            EnableDelayedProcessing = false;
            EnableHeartBeat = true;
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

        /// <summary>
        /// If true, a transaction will be held until the message is finished processing.
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable hold transaction until message committed]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableHoldTransactionUntilMessageCommited
        {
            get => false;
            // ReSharper disable once ValueParameterNotUsed
            set => FailIfReadOnly();
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
        /// Throws an exception if the readonly flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as imutable
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

            if (EnableMessageExpiration || QueueType == QueueTypes.RpcReceive || QueueType == QueueTypes.RpcSend)
            {
                command.Append(", ExpirationTime ");
            }
        }
        /// <summary>
        /// Adds the built in column values.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="command">The command.</param>
        internal void AddBuiltInColumnValues(TimeSpan? delay, TimeSpan expiration, StringBuilder command)
        {
            if (EnableDelayedProcessing)
            {
                command.Append(", @DelayDate ");
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

            if (EnableMessageExpiration || QueueType == QueueTypes.RpcReceive || QueueType == QueueTypes.RpcSend)
            {
                command.Append(", @ExpirationDate ");
            }

        }
        /// <summary>
        /// Adds the built in columns parameters.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="currentDateTime">The current date time.</param>
        internal void AddBuiltInColumnsParams(SQLiteCommand command, IAdditionalMessageData data, TimeSpan? delay, TimeSpan expiration, DateTime currentDateTime)
        {
            if (EnableDelayedProcessing)
            {
                command.Parameters.Add("@DelayDate", DbType.Int64, 1).Value = delay.HasValue ? currentDateTime.Add(delay.Value).Ticks : currentDateTime.Ticks;
            }

            if (EnableMessageExpiration || QueueType == QueueTypes.RpcReceive || QueueType == QueueTypes.RpcSend)
            {
                command.Parameters.Add("@ExpirationDate", DbType.Int64, 1).Value = expiration != TimeSpan.Zero ? currentDateTime.Add(expiration).Ticks : DateTime.MaxValue.Ticks;
            }

            if (EnablePriority)
            {
                var priority = 0;
                if (data.GetPriority().HasValue)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    priority = data.GetPriority().Value;
                }
                command.Parameters.Add("@priority", DbType.Int16, 1).Value = priority;
            }
            if (EnableRoute)
            {
                if (!string.IsNullOrEmpty(data.Route))
                {
                    command.Parameters.Add("@Route", DbType.AnsiString, 255).Value = data.Route;
                }
                else
                {
                    command.Parameters.Add("@Route", DbType.AnsiString, 255).Value = DBNull.Value;
                }
            }
            if (EnableStatus)
            {
                command.Parameters.Add("@Status", DbType.Int32, 4).Value = 0;
            }
        }
        #endregion
    }
}
