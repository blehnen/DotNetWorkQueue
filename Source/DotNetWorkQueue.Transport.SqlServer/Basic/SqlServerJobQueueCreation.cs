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
using DotNetWorkQueue.Transport.SqlServer.Schema;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Creates a job queue in a SQL server DB
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobQueueCreation" />
    public class SqlServerJobQueueCreation : IJobQueueCreation
    {
        private readonly SqlServerMessageQueueCreation _queueCreation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerJobQueueCreation"/> class.
        /// </summary>
        /// <param name="queueCreation">The queue creation.</param>
        public SqlServerJobQueueCreation(SqlServerMessageQueueCreation queueCreation)
        {
            _queueCreation = queueCreation;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _queueCreation.IsDisposed;

        /// <summary>
        /// Gets a disposable creation scope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>
        /// This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.
        /// </remarks>
        public ICreationScope Scope => _queueCreation.Scope;

        /// <summary>
        /// Gets or sets the options for the queue transport.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public SqlServerMessageQueueTransportOptions Options => _queueCreation.Options;

        /// <summary>
        /// Tells the transport to setup and create a queue for handling re-occurring jobs.
        /// </summary>
        /// <param name="registerService">The additional registrations.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="enableRoute">if set to <c>true</c> route support will be enabled.</param>
        /// <returns></returns>
        public QueueCreationResult CreateJobSchedulerQueue(Action<IContainer> registerService, string queue, string connection, bool enableRoute = false)
        {
            if (_queueCreation.Options.AdditionalColumns.Count == 0)
            {
                _queueCreation.Options.AdditionalColumns.Add(new Column("JobName", ColumnTypes.Varchar, 255, false, null));
                var constraint = new Constraint($"IX_{queue}JobName", ConstraintType.Constraint,
                    "JobName") {Unique = true};
                _queueCreation.Options.AdditionalConstraints.Add(constraint);
            }
            if(enableRoute)
            {
                _queueCreation.Options.EnableRoute = true;
            }
            return _queueCreation.CreateQueue();
        }

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// May not be supported by all transports. Any data in the queue will be lost.
        /// </remarks>
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
