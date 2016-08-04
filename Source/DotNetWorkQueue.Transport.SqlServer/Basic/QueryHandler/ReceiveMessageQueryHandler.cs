// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Collections.Generic;
using System.Data;
using System.Text;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery, IReceivedMessageInternal>
    {
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IMessageFactory _messageFactory;
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;

        private const string RpcdequeueKey = "dequeueCommandRpc";
        private const string DequeueKey = "dequeueCommand";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="serialization">The serialization.</param>
        public ReceiveMessageQueryHandler(ISqlServerMessageQueueTransportOptionsFactory optionsFactory, 
            TableNameHelper tableNameHelper, 
            IReceivedMessageFactory receivedMessageFactory,
            SqlServerCommandStringCache commandCache, 
            IMessageFactory messageFactory, 
            IHeaders headers, 
            ICompositeSerialization serialization)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);

            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _receivedMessageFactory = receivedMessageFactory;
            _commandCache = commandCache;
            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        /// <exception cref="PoisonMessageException">An error has occurred trying to re-assemble a message de-queued from the SQL server</exception>
        /// <exception cref="SqlServerMessageQueueId"></exception>
        /// <exception cref="SqlServerMessageQueueCorrelationId"></exception>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                selectCommand.Transaction = query.Transaction;
                if (query.MessageId != null && query.MessageId.HasValue)
                {
                    selectCommand.CommandText =
                        GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName, true,
                            _tableNameHelper.StatusName);
                    selectCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    selectCommand.Parameters["@QueueID"].Value = query.MessageId.Id.Value;
                }
                else
                {
                    selectCommand.CommandText =
                        GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName, false, _tableNameHelper.StatusName);
                }
                using (var reader = selectCommand.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    //load up the message from the DB
                    long id = 0;
                    var correlationId = Guid.Empty;
                    byte[] headerPayload = null;
                    byte[] messagePayload = null;
                    try
                    {
                        id = (long)reader["queueid"];
                        correlationId = (Guid)reader["CorrelationID"];
                        headerPayload = (byte[])reader["Headers"];
                        messagePayload = (byte[])reader["body"];

                        var headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerPayload);
                        var messageGraph = (MessageInterceptorsGraph)headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                        var message = _serialization.Serializer.BytesToMessage<MessageBody>(messagePayload, messageGraph).Body;
                        var newMessage = _messageFactory.Create(message, headers);

                        return _receivedMessageFactory.Create(newMessage,
                            new SqlServerMessageQueueId(id),
                            new SqlServerMessageQueueCorrelationId(correlationId));
                    }
                    catch (Exception error)
                    {
                        //at this point, the record has been de-queued, but it can't be processed.
                        throw new PoisonMessageException(
                            "An error has occurred trying to re-assemble a message de-queued from the SQL server", error, new SqlServerMessageQueueId(id), new SqlServerMessageQueueCorrelationId(correlationId), messagePayload, headerPayload);

                    }
                }
            }
        }
        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="metaTableName">Name of the meta table.</param>
        /// <param name="queueTableName">Name of the queue table.</param>
        /// <param name="forRpc">if set to <c>true</c> [for RPC].</param>
        /// <param name="statusTableName">Name of the status table.</param>
        /// <returns></returns>
        private string GetDeQueueCommand(string metaTableName, string queueTableName, bool forRpc, string statusTableName)
        {
            if (forRpc && _commandCache.Contains(RpcdequeueKey))
            {
                return _commandCache.Get(RpcdequeueKey);
            }
            if (_commandCache.Contains(DequeueKey))
            {
                return _commandCache.Get(DequeueKey);
            }

            var sb = new StringBuilder();

            //NOTE - this could be optimized a little bit. We are always using a CTE, but that's not necessary if the queue is 
            //setup as a pure FIFO queue.

            sb.AppendLine("declare @Queue1 table ");
            sb.AppendLine("( ");
            sb.AppendLine("QueueID bigint, ");
            sb.AppendLine("CorrelationID uniqueidentifier ");
            sb.AppendLine("); ");
            sb.AppendLine("with cte as ( ");
            sb.AppendLine("select top(1)  ");
            sb.AppendLine(metaTableName + ".QueueID, CorrelationID ");

            if (_options.Value.EnableStatus)
            {
                sb.Append(", [status] ");
            }
            if (_options.Value.EnableHeartBeat)
            {
                sb.Append(", HeartBeat ");
            }

            sb.AppendLine($"from {metaTableName} with (updlock, readpast, rowlock) ");

            //calculate where clause...
            if (_options.Value.EnableStatus && _options.Value.EnableDelayedProcessing)
            {
                sb.AppendFormat(" WHERE [Status] = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                sb.AppendLine("and QueueProcessTime < getutcdate() ");
            }
            else if (_options.Value.EnableStatus)
            {
                sb.AppendFormat("WHERE [Status] = {0}  ", Convert.ToInt16(QueueStatuses.Waiting));
            }
            else if (_options.Value.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (QueueProcessTime < getutcdate()) ");
            }

            if (forRpc)
            {
                sb.AppendLine("AND SourceQueueID = @QueueID");
            }


            if (_options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend)
            {
                sb.AppendLine("AND ExpirationTime > getutcdate()");
            }

            //determine order by looking at the options
            var bNeedComma = false;
            sb.Append(" Order by ");
            if (_options.Value.EnableStatus)
            {
                sb.Append(" [status] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnablePriority)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" [priority] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [QueueProcessTime] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableMessageExpiration)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [ExpirationTime] asc ");
                bNeedComma = true;
            }

            if (bNeedComma)
            {
                sb.Append(", ");
            }
            sb.AppendLine(" [QueueID] asc ) ");

            //determine if performing update or delete...
            if (_options.Value.EnableStatus && !_options.Value.EnableHoldTransactionUntilMessageCommited)
            { //update

                sb.AppendFormat("update cte set status = {0} ", Convert.ToInt16(QueueStatuses.Processing));
                if (_options.Value.EnableHeartBeat)
                {
                    sb.AppendLine(", HeartBeat = GetUTCDate() ");
                }
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else if (_options.Value.EnableHoldTransactionUntilMessageCommited)
            {
                sb.AppendLine("update cte set queueid = QueueID ");
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else
            { //delete - note even if heartbeat is enabled, there is no point in setting it

                //a delete here if not using transactions will actually remove the record from the queue
                //it's up to the caller to handle error conditions in this case.
                sb.AppendLine("delete from cte ");
                sb.AppendLine("output deleted.QueueID, deleted.CorrelationID into @Queue1 ");
            }

            //grab the rest of the data - this is all standard
            sb.AppendLine("select q.queueid, qm.body, qm.Headers, q.CorrelationID from @Queue1 q ");
            sb.AppendLine($"INNER JOIN {queueTableName} qm with (nolock) "); //a dirty read on the data here should be ok, since we have exclusive access to the queue record on the meta data table
            sb.AppendLine("ON q.QueueID = qm.QueueID  ");

            //if we are holding transactions, we can't update the status table as part of this query - has to be done after de-queue instead
            if (_options.Value.EnableStatusTable && !_options.Value.EnableHoldTransactionUntilMessageCommited)
            {
                sb.AppendFormat("update {0} set status = {1} where {0}.QueueID = (select q.queueid from @Queue1 q)", statusTableName, Convert.ToInt16(QueueStatuses.Processing));
            }

            return _commandCache.Add(forRpc ? RpcdequeueKey : DequeueKey, sb.ToString());
        }
    }
}
