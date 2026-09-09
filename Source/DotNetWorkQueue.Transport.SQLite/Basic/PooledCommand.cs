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
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Data.SQLite;
using System.Threading;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// An <see cref="IDbCommand"/> that keeps its underlying command - and so the statements SQLite
    /// compiled for it - when disposed, instead of throwing them away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// System.Data.SQLite compiles a command's statements on first execution and keeps them on the
    /// command object, so a command created per operation recompiles every time. That is the
    /// dominant cost of a dequeue, whose script is long and has several statements: measured on
    /// net10 against an empty queue, 27,389 ns and 22,144 B with a command created per dequeue
    /// against 4,458 ns and 552 B with one reused.
    /// </para>
    /// <para>
    /// Only the compiled statements are worth keeping. Callers build their own parameters through
    /// <see cref="CreateDbParameter"/>, so the parameter collection is emptied on release and rebuilt
    /// by the next caller; that measured 4,458 ns against 4,230 ns for keeping the parameters too,
    /// which is 99% of the win for none of the disruption to callers.
    /// </para>
    /// <para>
    /// Assigning <see cref="CommandText"/> the value it already holds is ignored rather than passed
    /// through, because the setter discards the compiled statements. Callers that set the text
    /// unconditionally - which is the normal shape - therefore keep the benefit without changing.
    /// </para>
    /// </remarks>
    internal sealed class PooledCommand : DbCommand
    {
        private readonly PooledConnectionEntry _owner;
        private readonly string _commandText;
        private readonly SQLiteCommand _command;
        private int _disposeCount;

        internal PooledCommand(PooledConnectionEntry owner, string commandText, SQLiteCommand command)
        {
            _owner = owner;
            _commandText = commandText;
            _command = command;
        }

        private SQLiteCommand Inner =>
            Volatile.Read(ref _disposeCount) == 0
                ? _command
                : throw new ObjectDisposedException(nameof(PooledCommand));

        /// <summary>
        /// The wrapped command, for the code that needs the provider type rather than the
        /// interface - <see cref="ReaderAsync"/>, which reaches the async execution methods that
        /// <see cref="IDbCommand"/> does not expose.
        /// </summary>
        internal SQLiteCommand Unwrap() => Inner;

        /// <inheritdoc />
        [SuppressMessage("Major Code Smell", "S4275:Getters and setters should access the expected fields",
            Justification = "The setter deliberately never assigns. A pooled command is filed under its text, so " +
                            "changing the text would leave it under a key that no longer describes it. The setter " +
                            "reads the field to accept a caller re-assigning the value it already holds - which is " +
                            "the normal shape of the callers - and refuses anything else.")]
        public override string CommandText
        {
            //the text this command was rented for, which is also how it is filed
            get => _commandText;

            //Assigning the same text would discard the compiled statements this type exists to keep,
            //so it is ignored. A different text would leave the command filed under a key that no
            //longer describes it, so it is refused rather than silently corrupting the cache.
            set
            {
                if (string.Equals(_commandText, value, StringComparison.Ordinal))
                    return;

                throw new NotSupportedException(
                    "The text of a pooled command cannot be changed; request a command for the text you want.");
            }
        }

        /// <inheritdoc />
        public override int CommandTimeout
        {
            get => Inner.CommandTimeout;
            set => Inner.CommandTimeout = value;
        }

        /// <inheritdoc />
        public override CommandType CommandType
        {
            get => Inner.CommandType;
            set => Inner.CommandType = value;
        }

        /// <inheritdoc />
        /// <remarks>
        /// DbCommand exposes the public Connection, Transaction and Parameters members and routes them
        /// through these protected ones, so they are declared here rather than as public properties.
        /// </remarks>
        protected override DbConnection DbConnection
        {
            get => Inner.Connection;
            set => throw new NotSupportedException(
                "The connection of a pooled command cannot be changed; request a command from the connection you want.");
        }

        /// <inheritdoc />
        protected override DbParameterCollection DbParameterCollection => Inner.Parameters;

        /// <inheritdoc />
        protected override DbTransaction DbTransaction
        {
            get => Inner.Transaction;
            set => Inner.Transaction = (SQLiteTransaction)value;
        }

        /// <inheritdoc />
        public override UpdateRowSource UpdatedRowSource
        {
            get => Inner.UpdatedRowSource;
            set => Inner.UpdatedRowSource = value;
        }

        /// <inheritdoc />
        /// <remarks>Required by DbCommand for designer support; nothing here reads it.</remarks>
        public override bool DesignTimeVisible { get; set; }

        /// <inheritdoc />
        public override void Cancel() => Inner.Cancel();

        /// <inheritdoc />
        protected override DbParameter CreateDbParameter() => Inner.CreateParameter();

        /// <inheritdoc />
        public override int ExecuteNonQuery() => Inner.ExecuteNonQuery();

        /// <inheritdoc />
        /// <remarks>DbCommand supplies the public ExecuteReader overloads, which route through this.</remarks>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            Inner.ExecuteReader(behavior);

        /// <inheritdoc />
        public override object ExecuteScalar() => Inner.ExecuteScalar();

        /// <inheritdoc />
        public override void Prepare()
        {
            //System.Data.SQLite compiles on execution; Prepare is a no-op there as well
        }

        /// <inheritdoc />
        /// <remarks>
        /// Returns the command to its pool rather than disposing it - keeping the compiled statements
        /// is the whole point of this type. The finalizer path does nothing, since the pool holds the
        /// only reference that matters.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                base.Dispose(false);
                return;
            }

            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            _owner.Release(_commandText);
        }
    }
}
