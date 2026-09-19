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
using Microsoft.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Contains connection information for a SQL server queue
    /// </summary>
    public partial class SqlConnectionInformation : BaseConnectionInformation
    {
        //A queue name is short and the pattern is anchored with no nested quantifiers, so it cannot
        //backtrack badly - but an unbounded match is still worth not offering, and both targets
        //should give up at the same point rather than one of them running on.
        private const int MatchTimeoutMilliseconds = 1000;

#if NETSTANDARD2_0
        //the [GeneratedRegex] source generator needs .NET 7 or later, so the old target compiles
        //the pattern once into a static instead. Same pattern, built at first use rather than
        //at compile time.
        private static readonly TimeSpan MatchTimeout =
            TimeSpan.FromMilliseconds(MatchTimeoutMilliseconds);

        private static readonly Regex ValidQueueName =
            new Regex(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant,
                MatchTimeout);

        private static Regex ValidQueueNamePattern() => ValidQueueName;
#else
        [GeneratedRegex(@"^[a-zA-Z0-9_.]+$", RegexOptions.None, MatchTimeoutMilliseconds)]
        private static partial Regex ValidQueueNamePattern();
#endif

        private string _server;
        private string _catalog;

        #region Constructor
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
        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public override string Server => _server;

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>
        /// The name of the container for the queue
        /// </remarks>
        public override string Container => _catalog;

        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
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
        /// Lower than SQL Server's own 128 character identifier limit, because the queue name is not
        /// itself the longest identifier built from it. The longest is
        /// <c>IX_{name}History_Status_Completed</c> - twenty-seven characters more than the name - so at
        /// 102 characters creation fails with "The identifier that starts with ... is too long".
        ///
        /// Measured rather than derived: 101 creates, 102 does not.
        ///
        /// The real limit depends on the options, which the validator cannot see: with history off the
        /// longest identifier is an ErrorTracking one and names into the low 110s create successfully.
        /// The lower number is taken deliberately. A name that works only while history is off would
        /// break the moment somebody enabled it - a configuration change silently breaking an existing
        /// queue, which is worse than refusing a long name up front (GitHub #344).
        ///
        /// Unlike PostgreSQL in #339, going over this fails loudly and names the offending identifier,
        /// so nothing was ever silently wrong here. This turns a confusing failure at creation into a
        /// clear one where the name is supplied.
        /// </remarks>
        private const int MaxQueueNameLength = 101;

        /// <summary>Validates that the queue name contains only safe characters for use as a SQL Server table name identifier.</summary>
        private static void ValidateQueueName(string name)
        {
            if (string.IsNullOrEmpty(name)) return; // allow empty for backward compatibility
            Guard.IsValid(name, n => n.Length <= MaxQueueNameLength,
                $"Queue name exceeds maximum length of {MaxQueueNameLength} characters. Got {name.Length} characters. "
                + "SQL Server identifiers stop at 128 characters, and the queue name is a prefix for names longer "
                + "than itself - the longest is IX_<name>History_Status_Completed, twenty-seven characters more.");
            Guard.IsValid(name, n => ValidQueueNamePattern().IsMatch(n),
                "Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.");
        }

        /// <summary>
        /// Validates the connection string and determines the value of the server property
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void ValidateConnection(string value)
        {
            //validate that the passed in string parses as a SQL server connection string
            var builder = new SqlConnectionStringBuilder(value); //will fail here if not valid
            _server = builder.DataSource;
            _catalog = builder.InitialCatalog;
        }
    }
}
