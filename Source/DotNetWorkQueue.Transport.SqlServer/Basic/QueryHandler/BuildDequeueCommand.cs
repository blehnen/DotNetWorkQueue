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
 
using DotNetWorkQueue.Validation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    internal class BuildDequeueCommand
    {
        private readonly CreateDequeueStatement _createDequeueStatement;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        public BuildDequeueCommand(ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            CreateDequeueStatement createDequeueStatement)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => createDequeueStatement, createDequeueStatement);

            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _createDequeueStatement = createDequeueStatement;
        }
        public void BuildCommand(SqlCommand selectCommand, ReceiveMessageQuery<SqlConnection, SqlTransaction> query)
        {
            BuildCommandInternal(selectCommand, query.Connection, query.Transaction, query.MessageId, query.Routes);
        }
        public void BuildCommand(SqlCommand selectCommand, ReceiveMessageQueryAsync<SqlConnection, SqlTransaction> query)
        {
            BuildCommandInternal(selectCommand, query.Connection, query.Transaction, query.MessageId, query.Routes);
        }

        private void BuildCommandInternal(SqlCommand selectCommand, 
            SqlConnection connection, SqlTransaction transaction, IMessageId messageId, List<string> routes)
        {
            selectCommand.Transaction = transaction;
            if (messageId != null && messageId.HasValue)
            {
                selectCommand.CommandText =
                    _createDequeueStatement.GetDeQueueCommand(true, routes);
                selectCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
                selectCommand.Parameters["@QueueID"].Value = messageId.Id.Value;
            }
            else
            {
                selectCommand.CommandText =
                    _createDequeueStatement.GetDeQueueCommand(false, routes);
            }
            if (_options.Value.EnableRoute && routes != null && routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in routes)
                {
                    selectCommand.Parameters.Add("@Route" + routeCounter.ToString(), SqlDbType.VarChar);
                    selectCommand.Parameters["@Route" + routeCounter.ToString()].Value = route;
                    routeCounter++;
                }
            }
        }
    }
}
