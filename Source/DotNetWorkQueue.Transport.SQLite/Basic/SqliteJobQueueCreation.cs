﻿// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Schema;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Creates a job queue in a SQL server DB
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobQueueCreation" />
    public class SqliteJobQueueCreation : IJobQueueCreation
    {
        private readonly SqLiteMessageQueueCreation _queueCreation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteJobQueueCreation"/> class.
        /// </summary>
        /// <param name="queueCreation">The queue creation.</param>
        public SqliteJobQueueCreation(SqLiteMessageQueueCreation queueCreation)
        {
            _queueCreation = queueCreation;
        }

        /// <inheritdoc />
        public bool IsDisposed => _queueCreation.IsDisposed;

        /// <inheritdoc />
        public ICreationScope Scope => _queueCreation.Scope;

        /// <summary>
        /// Gets or sets the options for the queue transport.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public SqLiteMessageQueueTransportOptions Options => _queueCreation.Options;

        /// <inheritdoc />
        public QueueCreationResult CreateJobSchedulerQueue(Action<IContainer> registerService, QueueConnection queueConnection, Action<IContainer> setOptions = null, bool enableRoute = false)
        {
            if (_queueCreation.Options.AdditionalColumns.Count == 0)
            {
                _queueCreation.Options.EnableStatusTable = true;
                _queueCreation.Options.AdditionalColumns.Add(new Column("JobName", ColumnTypes.Text, 255, false, null));
                var constraint = new Constraint($"IX_{queueConnection.Queue}JobName", ConstraintType.Constraint,
                    "JobName")
                { Unique = true };
                _queueCreation.Options.AdditionalConstraints.Add(constraint);
            }
            if (enableRoute)
            {
                _queueCreation.Options.EnableRoute = true;
            }
            return _queueCreation.CreateQueue();
        }

        /// <inheritdoc />
        public QueueRemoveResult RemoveQueue()
        {
            return _queueCreation.RemoveQueue();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _queueCreation.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}