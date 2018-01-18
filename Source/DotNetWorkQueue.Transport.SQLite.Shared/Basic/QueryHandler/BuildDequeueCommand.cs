using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.QueryHandler
{
    internal class BuildDequeueCommand
    {
        private readonly IGetTime _getTime;
        public BuildDequeueCommand(IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _getTime = getTimeFactory.Create();
        }
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal void BuildCommand(IDbCommand selectCommand, IMessageId messageId, CommandString commandString,
            SqLiteMessageQueueTransportOptions options, List<string> routes )
        {
            selectCommand.CommandText = commandString.CommandText;
            if (messageId != null && messageId.HasValue)
            {
                var param = selectCommand.CreateParameter();
                param.ParameterName = "@QueueID";
                param.DbType = DbType.Int64;
                param.Value = messageId.Id.Value;
                selectCommand.Parameters.Add(param);

                param = selectCommand.CreateParameter();
                param.ParameterName = "@CurrentDateTime";
                param.DbType = DbType.Int64;
                param.Value = _getTime.GetCurrentUtcDate().Ticks;
                selectCommand.Parameters.Add(param);
            }
            else
            {
                var param = selectCommand.CreateParameter();
                param.ParameterName = "@CurrentDateTime";
                param.DbType = DbType.Int64;
                param.Value = _getTime.GetCurrentUtcDate().Ticks;
                selectCommand.Parameters.Add(param);
            }

            if (options.EnableRoute && routes != null && routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in routes)
                {
                    var param = selectCommand.CreateParameter();
                    param.ParameterName = "@Route" + routeCounter;
                    param.DbType = DbType.AnsiString;
                    param.Value = route;
                    selectCommand.Parameters.Add(param);
                    routeCounter++;
                }
            }
        }
    }
}
