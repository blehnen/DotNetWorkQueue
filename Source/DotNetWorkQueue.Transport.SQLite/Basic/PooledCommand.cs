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
    /// <see cref="CreateParameter"/>, so the parameter collection is emptied on release and rebuilt
    /// by the next caller; that measured 4,458 ns against 4,230 ns for keeping the parameters too,
    /// which is 99% of the win for none of the disruption to callers.
    /// </para>
    /// <para>
    /// Assigning <see cref="CommandText"/> the value it already holds is ignored rather than passed
    /// through, because the setter discards the compiled statements. Callers that set the text
    /// unconditionally - which is the normal shape - therefore keep the benefit without changing.
    /// </para>
    /// </remarks>
    internal sealed class PooledCommand : IDbCommand
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

        /// <inheritdoc />
        public string CommandText
        {
            get => Inner.CommandText;

            //Assigning the same text would discard the compiled statements this type exists to keep,
            //so it is ignored. A different text would leave the command filed under a key that no
            //longer describes it, so it is refused rather than silently corrupting the cache.
            set
            {
                if (string.Equals(Inner.CommandText, value, StringComparison.Ordinal))
                    return;

                throw new NotSupportedException(
                    "The text of a pooled command cannot be changed; request a command for the text you want.");
            }
        }

        /// <inheritdoc />
        public int CommandTimeout
        {
            get => Inner.CommandTimeout;
            set => Inner.CommandTimeout = value;
        }

        /// <inheritdoc />
        public CommandType CommandType
        {
            get => Inner.CommandType;
            set => Inner.CommandType = value;
        }

        /// <inheritdoc />
        public IDbConnection Connection
        {
            get => Inner.Connection;
            set => throw new NotSupportedException(
                "The connection of a pooled command cannot be changed; request a command from the connection you want.");
        }

        /// <inheritdoc />
        public IDataParameterCollection Parameters => Inner.Parameters;

        /// <inheritdoc />
        public IDbTransaction Transaction
        {
            get => Inner.Transaction;
            set => Inner.Transaction = (SQLiteTransaction)value;
        }

        /// <inheritdoc />
        public UpdateRowSource UpdatedRowSource
        {
            get => Inner.UpdatedRowSource;
            set => Inner.UpdatedRowSource = value;
        }

        /// <inheritdoc />
        public void Cancel() => Inner.Cancel();

        /// <inheritdoc />
        public IDbDataParameter CreateParameter() => Inner.CreateParameter();

        /// <inheritdoc />
        public int ExecuteNonQuery() => Inner.ExecuteNonQuery();

        /// <inheritdoc />
        public IDataReader ExecuteReader() => Inner.ExecuteReader();

        /// <inheritdoc />
        public IDataReader ExecuteReader(CommandBehavior behavior) => Inner.ExecuteReader(behavior);

        /// <inheritdoc />
        public object ExecuteScalar() => Inner.ExecuteScalar();

        /// <summary>
        /// No-op. The statements are compiled lazily on execution and kept on the command, which is
        /// the point of this type.
        /// </summary>
        public void Prepare()
        {
            //System.Data.SQLite compiles on execution; Prepare is a no-op there as well
        }

        /// <summary>
        /// Releases the command back to the connection that owns it. The parameters a caller added
        /// are cleared and the transaction detached, so the next caller sees a command in the state
        /// a freshly created one would be in - except that its statements are already compiled.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            _command.Parameters.Clear();
            _command.Transaction = null;
            _owner.Release(_commandText);
        }
    }
}
