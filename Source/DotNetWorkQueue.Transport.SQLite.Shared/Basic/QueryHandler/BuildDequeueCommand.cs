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

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "Query checked")]
        internal void BuildCommand(IDbCommand selectCommand, CommandString commandString,
            SqLiteMessageQueueTransportOptions options, List<string> routes)
        {
            selectCommand.CommandText = commandString.CommandText;

            var paramDate = selectCommand.CreateParameter();
            paramDate.ParameterName = "@CurrentDateTime";
            paramDate.DbType = DbType.Int64;
            paramDate.Value = _getTime.GetCurrentUtcDate().Ticks;
            selectCommand.Parameters.Add(paramDate);

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
