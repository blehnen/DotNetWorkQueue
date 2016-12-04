using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
using DotNetWorkQueue.Validation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void BuildCommand(SqlCommand selectCommand, ReceiveMessageQuery query)
        {
            selectCommand.Transaction = query.Transaction;
            if (query.MessageId != null && query.MessageId.HasValue)
            {
                selectCommand.CommandText =
                     _createDequeueStatement.GetDeQueueCommand(true, query.Routes);
                selectCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
                selectCommand.Parameters["@QueueID"].Value = query.MessageId.Id.Value;
            }
            else
            {
                selectCommand.CommandText =
                     _createDequeueStatement.GetDeQueueCommand(false, query.Routes);
            }
            if (_options.Value.EnableRoute && query.Routes != null && query.Routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in query.Routes)
                {
                    selectCommand.Parameters.Add("@Route" + routeCounter.ToString(), SqlDbType.VarChar);
                    selectCommand.Parameters["@Route" + routeCounter.ToString()].Value = route;
                    routeCounter++;
                }
            }
        }
    }
}
