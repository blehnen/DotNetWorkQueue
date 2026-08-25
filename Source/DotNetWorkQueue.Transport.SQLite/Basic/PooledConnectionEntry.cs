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
using System.Data;
using System.Data.SQLite;
using System.Threading;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// A pooled connection together with the commands compiled on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQLite compiles a command's statements on first execution and keeps them on the command
    /// object, so the compiled form belongs to a connection and has to live and die with one. That
    /// is why the pool holds this rather than a bare <see cref="SQLiteConnection"/>.
    /// </para>
    /// <para>
    /// A rented connection is used by one operation at a time, so no locking is needed here. The
    /// in-use set covers the one case that is not sequential: a caller holding a command open - a
    /// reader, typically - while asking the same connection for another. Those are different
    /// statements today, but asking twice for the same one hands the second caller its own
    /// uncached command rather than a command already in use.
    /// </para>
    /// </remarks>
    internal sealed class PooledConnectionEntry : IDisposable
    {
        /// <summary>
        /// A queue issues a handful of distinct statements. The cap exists because the dequeue
        /// script embeds the caller's user clause, which a caller could in principle vary per call;
        /// past it, commands are created and disposed as they were before.
        /// </summary>
        private const int MaxCommandsPerConnection = 16;

        private readonly Dictionary<string, SQLiteCommand> _commands =
            new Dictionary<string, SQLiteCommand>(StringComparer.Ordinal);

        private readonly HashSet<string> _inUse = new HashSet<string>(StringComparer.Ordinal);

        private int _disposeCount;

        internal PooledConnectionEntry(SQLiteConnection connection)
        {
            Connection = connection;
        }

        internal SQLiteConnection Connection { get; }

        /// <summary>How many distinct commands this connection is holding compiled statements for.</summary>
        internal int CachedCommandCount => _commands.Count;

        /// <summary>
        /// A command for <paramref name="commandText"/>, reusing the statements compiled for it on
        /// this connection where that is possible.
        /// </summary>
        internal IDbCommand CreateCommand(string commandText)
        {
            if (Volatile.Read(ref _disposeCount) != 0 || string.IsNullOrEmpty(commandText) || _inUse.Contains(commandText))
                return Uncached(commandText);

            if (!_commands.TryGetValue(commandText, out var command))
            {
                if (_commands.Count >= MaxCommandsPerConnection)
                    return Uncached(commandText);

                command = Connection.CreateCommand();
                command.CommandText = commandText;
                _commands.Add(commandText, command);
            }

            _inUse.Add(commandText);
            return new PooledCommand(this, commandText, command);
        }

        /// <summary>Marks a command as available again; called when the caller disposes it.</summary>
        internal void Release(string commandText)
        {
            _inUse.Remove(commandText);
        }

        private SQLiteCommand Uncached(string commandText)
        {
            var command = Connection.CreateCommand();
            command.CommandText = commandText;
            return command;
        }

        /// <summary>
        /// Disposes the compiled commands and then the connection. Order matters: a command
        /// outliving its connection is what leaves a database file locked.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            foreach (var command in _commands.Values)
                Safely(command.Dispose);

            _commands.Clear();
            _inUse.Clear();

            Safely(Connection.Dispose);
        }

        /// <summary>
        /// Disposal of something already broken can itself throw, and there is nothing useful to do
        /// about it - it is being discarded either way.
        /// </summary>
        private static void Safely(Action action)
        {
            try
            {
                action();
            }
            catch (SQLiteException)
            {
                //discarding a broken command or connection
            }
            catch (InvalidOperationException)
            {
                //discarding a broken command or connection
            }
        }
    }
}
