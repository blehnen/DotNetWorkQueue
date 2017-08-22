using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    public class GetTableExistsTransactionQueryHandler: IQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICaseTableName _caseTableName;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="caseTableName">Name of the case table.</param>
        public GetTableExistsTransactionQueryHandler(CommandStringCache commandCache,
            IDbConnectionFactory dbConnectionFactory,
            IConnectionInformation connectionInformation,
            ICaseTableName caseTableName)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
            _connectionInformation = connectionInformation;
            _caseTableName = caseTableName;
        }

        public bool Handle(GetTableExistsTransactionQuery query)
        {
            using (var command = query.Connection.CreateCommand())
            {
                command.Transaction = query.Trans;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.GetTableExists);

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@Table";
                parameter.DbType = DbType.AnsiString;
                parameter.Value = _caseTableName.FormatTableName(query.TableName);
                command.Parameters.Add(parameter);

                var parameterDb = command.CreateParameter();
                parameterDb.ParameterName = "@Database";
                parameterDb.DbType = DbType.AnsiString;
                parameterDb.Value = _connectionInformation.Container;
                command.Parameters.Add(parameterDb);

                using (var reader = command.ExecuteReader())
                {
                    var result = reader.Read();
                    return result;
                }
            }
        }
    }
}
