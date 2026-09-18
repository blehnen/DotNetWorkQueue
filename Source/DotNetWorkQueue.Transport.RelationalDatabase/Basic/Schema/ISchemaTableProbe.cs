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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Answers whether a table exists, inside a transaction or outside one.
    /// </summary>
    /// <remarks>
    /// One concern behind one type. The two underlying query handlers ask the same question and differ
    /// only in whether there is a transaction to ask it in, so taking both separately spread that
    /// choice across every caller and pushed the updater's constructor past what is reasonable.
    ///
    /// The catalog query behind each is the transport's own, which matters: identifier casing differs
    /// between them - PostgreSQL folds unquoted names to lower case - and a probe written here rather
    /// than reused would answer wrongly on at least one of them.
    /// </remarks>
    public interface ISchemaTableProbe
    {
        /// <summary>
        /// Whether the table exists, on its own connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">The table to look for.</param>
        bool Exists(string connectionString, string tableName);

        /// <summary>
        /// Whether the table exists, as seen from inside a transaction.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction to look from.</param>
        /// <param name="tableName">The table to look for.</param>
        /// <remarks>
        /// Sees tables created earlier in the same transaction, which the other overload cannot. An
        /// upgrade that creates the version table and then reads it needs this one.
        /// </remarks>
        bool Exists(DbConnection connection, DbTransaction transaction, string tableName);
    }
}
