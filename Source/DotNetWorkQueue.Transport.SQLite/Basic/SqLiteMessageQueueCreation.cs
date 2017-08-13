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
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// A class that will create the queue tables if needed. No support for updating existing tables is provided.
    /// </summary>
    public class SqLiteMessageQueueCreation : IQueueCreation
    {
        #region Member level variables

        private readonly SqLiteMessageQueueSchema _createSchema;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _queryTableExists;

        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
            _createCommand;

        private readonly ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult> _deleteCommand;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private int _disposeCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueCreation" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="queryTableExists">The query table exists.</param>
        /// <param name="options">The options.</param>
        /// <param name="createSchema">The create schema.</param>
        /// <param name="createCommand">The create command.</param>
        /// <param name="deleteCommand">The delete command.</param>
        /// <param name="creationScope">The creation scope.</param>
        public SqLiteMessageQueueCreation(IConnectionInformation connectionInfo,
            IQueryHandler<GetTableExistsQuery, bool> queryTableExists,
            ISqLiteMessageQueueTransportOptionsFactory options, 
            SqLiteMessageQueueSchema createSchema,
            ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> createCommand,
            ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult> deleteCommand,
            ICreationScope creationScope
            )
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => createSchema, createSchema);
            Guard.NotNull(() => queryTableExists, queryTableExists);
            Guard.NotNull(() => createCommand, createCommand);
            Guard.NotNull(() => deleteCommand, deleteCommand);
            Guard.NotNull(() => creationScope, creationScope);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _createSchema = createSchema;
            _queryTableExists = queryTableExists;
            _createCommand = createCommand;
            _deleteCommand = deleteCommand;
            ConnectionInfo = connectionInfo;
            Scope = creationScope;
        }

        #endregion

        #region Public Methods / Properties

        /// <summary>
        /// Gets or sets the options for the queue transport.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public SqLiteMessageQueueTransportOptions Options => _options.Value;

        /// <summary>
        /// Gets the connection information for the queue.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        public IConnectionInformation ConnectionInfo { get; }

        /// <summary>
        /// Gets a disposable creation scrope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.</remarks>
        public ICreationScope Scope { get; }

        /// <summary>
        /// Creates the queue if needed.
        /// </summary>
        /// <returns></returns>
        public QueueCreationResult CreateQueue()
        {
            return !QueueExists ? CreateQueueInternal() : new QueueCreationResult(QueueCreationStatus.AlreadyExists);
        }

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <remarks>Any data in the queue will be lost. Will cause exceptions in any producer/consumer that is connected</remarks>
        /// <returns></returns>
        public QueueRemoveResult RemoveQueue()
        {
            return QueueExists ? RemoveQueueInternal() : new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
        }

        /// <summary>
        /// Returns true if the queue exists in the transport
        /// </summary>
        /// <value>
        ///   <c>true</c> if [queue exists]; otherwise, <c>false</c>.
        /// </value>
        public bool QueueExists => _queryTableExists.Handle(new GetTableExistsQuery(ConnectionInfo.ConnectionString,
            ConnectionInfo.QueueName));

        #region IDisposable, IsDisposed

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {

            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <returns></returns>
        private QueueCreationResult CreateQueueInternal()
        {
            var valid = Options.ValidConfiguration();
            if (valid.Valid)
            {
                return
                    _createCommand.Handle(
                        new CreateQueueTablesAndSaveConfigurationCommand<ITable>(_createSchema.GetSchema()));
            }
            return new QueueCreationResult(QueueCreationStatus.ConfigurationError, valid.ErrorMessage);
        }

        /// <summary>
        /// Removes the queue if it exists
        /// </summary>
        /// <returns></returns>
        private QueueRemoveResult RemoveQueueInternal()
        {
            return _deleteCommand.Handle(new DeleteQueueTablesCommand());
        }

        #endregion
    }
}
