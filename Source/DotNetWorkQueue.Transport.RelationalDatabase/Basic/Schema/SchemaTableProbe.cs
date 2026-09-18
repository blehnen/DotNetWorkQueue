// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <inheritdoc />
    public class SchemaTableProbe : ISchemaTableProbe
    {
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly IQueryHandler<GetTableExistsTransactionQuery, bool> _tableExistsInTransaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaTableProbe"/> class.
        /// </summary>
        /// <param name="tableExists">Answers on its own connection.</param>
        /// <param name="tableExistsInTransaction">Answers from inside a transaction.</param>
        public SchemaTableProbe(IQueryHandler<GetTableExistsQuery, bool> tableExists,
            IQueryHandler<GetTableExistsTransactionQuery, bool> tableExistsInTransaction)
        {
            Guard.NotNull(tableExists);
            Guard.NotNull(tableExistsInTransaction);

            _tableExists = tableExists;
            _tableExistsInTransaction = tableExistsInTransaction;
        }

        /// <inheritdoc />
        public bool Exists(string connectionString, string tableName) =>
            _tableExists.Handle(new GetTableExistsQuery(connectionString, tableName));

        /// <inheritdoc />
        public bool Exists(DbConnection connection, DbTransaction transaction, string tableName) =>
            _tableExistsInTransaction.Handle(new GetTableExistsTransactionQuery(connection, transaction, tableName));
    }
}
