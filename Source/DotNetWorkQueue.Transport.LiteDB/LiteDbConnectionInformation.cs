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

namespace DotNetWorkQueue.Transport.LiteDb
{
    /// <inheritdoc />
    public partial class LiteDbConnectionInformation : BaseConnectionInformation
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

        private readonly string _server;

        #region Constructor
        /// <inheritdoc />
        public LiteDbConnectionInformation(QueueConnection queueConnection) : base(queueConnection)
        {
            ValidateQueueName(queueConnection.Queue);
            _server = queueConnection.Connection;
        }
        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override string Server => _server;

        /// <inheritdoc />
        public override string Container => Server;
        #endregion

        /// <summary>Validates that the queue name contains only safe characters for use as a LiteDB collection name.</summary>
        private static void ValidateQueueName(string name)
        {
            Guard.NotNullOrEmpty(name);
            Guard.IsValid(name, n => n.Length <= 256,
                $"Queue name exceeds maximum length of 256 characters. Got {name.Length} characters.");
            Guard.IsValid(name, n => ValidQueueNamePattern().IsMatch(n),
                "Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.");
        }

        #region IClone
        /// <inheritdoc />
        public override IConnectionInformation Clone()
        {
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new LiteDbConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
        }
        #endregion
    }
}
