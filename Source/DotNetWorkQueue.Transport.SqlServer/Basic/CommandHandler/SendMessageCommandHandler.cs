// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, long>
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly TransportConfigurationSend _configurationSend;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configurationSend">The configuration send.</param>
        public SendMessageCommandHandler(TableNameHelper tableNameHelper, 
            ICompositeSerialization serializer,
            ISqlServerMessageQueueTransportOptionsFactory optionsFactory, 
            IHeaders headers,
            SqlServerCommandStringCache commandCache, 
            TransportConfigurationSend configurationSend)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configurationSend, configurationSend);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _commandCache = commandCache;
            _configurationSend = configurationSend;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="commandSend">The command.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to insert record - the ID of the new record returned by SQL server was 0</exception>
        public long Handle(SendMessageCommand commandSend)
        {
            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend;
            }

            using (var connection = new SqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = trans;
                        command.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.InsertMessageBody);
                        var serialization =
                            _serializer.Serializer.MessageToBytes(new MessageBody {Body = commandSend.MessageToSend.Body});

                        command.Parameters.Add("@body", SqlDbType.VarBinary, -1);
                        command.Parameters["@body"].Value = serialization.Output;

                        commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph,
                            serialization.Graph);

                        command.Parameters.Add("@headers", SqlDbType.VarBinary, -1);
                        command.Parameters["@headers"].Value =
                            _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                        var id = Convert.ToInt64(command.ExecuteScalar());
                        if (id > 0)
                        {
                            var expiration = TimeSpan.Zero;
                            if (_messageExpirationEnabled.Value)
                            {
                                expiration = MessageExpiration.GetExpiration(commandSend, _headers);
                            }

                            CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration, connection, id,
                                commandSend.MessageToSend, commandSend.MessageData, trans);
                            if (_options.Value.EnableStatusTable)
                            {
                                CreateStatusRecord(connection, id, commandSend.MessageToSend, commandSend.MessageData, trans);
                            }
                        }
                        else
                        {
                            throw new DotNetWorkQueueException(
                                "Failed to insert record - the ID of the new record returned by SQL server was 0");
                        }
                        trans.Commit();
                        return id;
                    }
                }
            }
        }

        /// <summary>
        /// Creates the status record.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <param name="trans">The transaction.</param>
        private void CreateStatusRecord(SqlConnection connection, long id, IMessage message,
            IAdditionalMessageData data, SqlTransaction trans)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, id, _options.Value);
                command.Transaction = trans;
                command.ExecuteNonQuery();
            }
        }

        #region Insert Meta data record
        /// <summary>
        /// Creates the meta data record.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <param name="trans">The transaction.</param>
        private void CreateMetaDataRecord(TimeSpan? delay, TimeSpan expiration, SqlConnection connection, long id, IMessage message, IAdditionalMessageData data,
            SqlTransaction trans)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildMetaCommand(command, _tableNameHelper, _headers,
                   data, message, id, _options.Value, delay, expiration);
                command.Transaction = trans;
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}
