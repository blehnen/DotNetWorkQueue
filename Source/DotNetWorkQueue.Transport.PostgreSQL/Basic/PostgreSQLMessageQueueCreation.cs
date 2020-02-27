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
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlMessageQueueCreation : IQueueCreation
    {
        #region Member level variables

        private readonly PostgreSqlMessageQueueSchema _createSchema;
        private readonly RelationalDatabase.IQueryHandler<GetTableExistsQuery, bool> _queryTableExists;

        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
            _createCommand;

        private readonly ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult> _deleteCommand;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private int _disposeCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueCreation" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="queryTableExists">The query table exists.</param>
        /// <param name="options">The options.</param>
        /// <param name="createSchema">The create schema.</param>
        /// <param name="createCommand">The create command.</param>
        /// <param name="deleteCommand">The delete command.</param>
        /// <param name="creationScope">The creation scope.</param>
        public PostgreSqlMessageQueueCreation(IConnectionInformation connectionInfo, RelationalDatabase.IQueryHandler<GetTableExistsQuery, bool> queryTableExists,
            IPostgreSqlMessageQueueTransportOptionsFactory options, 
            PostgreSqlMessageQueueSchema createSchema,
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

            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
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
        public PostgreSqlMessageQueueTransportOptions Options => _options.Value;

        /// <inheritdoc />
        public IConnectionInformation ConnectionInfo { get; }

        /// <inheritdoc />
        public ICreationScope Scope { get; }

        /// <inheritdoc />
        public QueueCreationResult CreateQueue()
        {
            return !QueueExists ? CreateQueueInternal() : new QueueCreationResult(QueueCreationStatus.AlreadyExists);
        }

        /// <inheritdoc />
        public QueueRemoveResult RemoveQueue()
        {
            return QueueExists ? RemoveQueueInternal() : new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
