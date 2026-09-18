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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL
{
    /// <inheritdoc />
    public partial class SqlConnectionInformation : BaseConnectionInformation
    {
        //no dot: the name is written into DDL unquoted, so PostgreSQL reads one as a schema separator
        //and CREATE TABLE fails with a syntax error. Dots were allowed here and have never been
        //creatable (GitHub #375).
        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex ValidQueueNamePattern();

        private string _server;

        #region Constructor
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueConnection">Queue and connection information.</param>
        public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)
        {
            ValidateQueueName(queueConnection.Queue);
            ValidateConnection(queueConnection.Connection);
        }
        #endregion

        #region Public Properties
        /// <inheritdoc />
        public override string Server => _server;

        /// <inheritdoc />
        public override string Container => Server;
        #endregion

        #region IClone
        /// <inheritdoc />
        public override IConnectionInformation Clone()
        {
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new SqlConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
        }
        #endregion

        /// <summary>
        /// The longest queue name that can actually be created.
        /// </summary>
        /// <remarks>
        /// Lower than PostgreSQL's own 63 byte identifier limit, because the queue name is not itself the
        /// longest identifier built from it. Creation generates <c>PK_{name}MetaDataErrors</c> - seventeen
        /// characters more than the name - alongside <c>PK_{name}MetaData</c>, and
        /// <c>IX_{name}History_Status_Completed</c> alongside <c>IX_{name}History_QueueID</c>. From 52
        /// characters up, each pair truncates to the same 63 bytes and the second one raises
        /// <c>42710 duplicate_object</c>.
        ///
        /// That error is caught and reported as <see cref="QueueCreationStatus.AttemptedToCreateAlreadyExists"/>,
        /// which carries <c>Success == true</c>, so the caller was told the queue was ready while the whole
        /// batch had rolled back and nothing existed. Rejecting the name is the only way to fail where the
        /// mistake is (GitHub #339).
        ///
        /// Measured rather than derived: 51 creates, 52 does not.
        /// </remarks>
        private const int MaxQueueNameLength = 51;

        /// <summary>Validates that the queue name contains only safe characters for use as a PostgreSQL identifier.</summary>
        private static void ValidateQueueName(string name)
        {
            if (string.IsNullOrEmpty(name)) return; // allow empty for backward compatibility
            Guard.IsValid(name, n => n.Length <= MaxQueueNameLength,
                $"Queue name exceeds maximum length of {MaxQueueNameLength} characters. Got {name.Length} characters. "
                + "PostgreSQL truncates every identifier at 63 bytes, and the queue name is a prefix for table names "
                + "longer than itself - the longest is <name>MetaDataErrors, 14 characters more - so a longer queue "
                + "name produces two tables that truncate to the same identifier. Index and constraint names are "
                + "shortened rather than capped, so they no longer constrain this limit.");
            Guard.IsValid(name, n => ValidQueueNamePattern().IsMatch(n),
                "Queue name contains invalid characters. Only alphanumeric characters and underscores are allowed. "
                + "A dot was permitted here until it was found that a queue whose name contains one cannot be created: "
                + "the name goes into the DDL unquoted, so PostgreSQL reads the dot as a schema separator and the "
                + "CREATE fails with a syntax error, which was then reported as the queue already existing.");
        }

        /// <summary>
        /// Validates the connection string and determines the value of the server property
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void ValidateConnection(string value)
        {
            //validate that the passed in string parses as a connection string
            var builder = new NpgsqlConnectionStringBuilder(value); //will fail here if not valid
            _server = builder.Database;
        }
    }
}
