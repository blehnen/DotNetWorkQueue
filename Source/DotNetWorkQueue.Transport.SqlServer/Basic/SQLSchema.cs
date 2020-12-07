using System;
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    internal class SqlSchema : ISqlSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSchema"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public SqlSchema(IConnectionInformation connection)
        {
            Guard.NotNull(() => connection, connection);
            Schema = connection.AdditionalConnectionSettings.GetSchema();
        }

        public string Schema
        {
            get;
        }
    }
}
