// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Caches command strings
    /// </summary>
    public abstract class CommandStringCache
    {
        protected readonly Dictionary<CommandStringTypes, string> CommandCache;
        private readonly ConcurrentDictionary<string, CommandString> _commandCacheRunTime;
        protected readonly TableNameHelper TableNameHelper;
        private bool _commandsBuilt;
        private readonly object _commandBuilder = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        protected CommandStringCache(TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            TableNameHelper = tableNameHelper;
            CommandCache = new Dictionary<CommandStringTypes, string>();
            _commandCacheRunTime = new ConcurrentDictionary<string, CommandString>();
        }

        /// <summary>
        /// Gets the command for the indicated command type
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <returns></returns>
        public string GetCommand(CommandStringTypes type)
        {
            if (_commandsBuilt && CommandCache.Count != 0) return CommandCache[type];
            lock (_commandBuilder)
            {
                if (CommandCache.Count != 0) return CommandCache[type];
                BuildCommands();
                _commandsBuilt = true;
            }
            return CommandCache[type];
        }

        /// <summary>
        /// Gets the command for the indicated command type
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public string GetCommand(CommandStringTypes type, params object[] input)
        {
            if (_commandsBuilt && CommandCache.Count != 0) return string.Format(CommandCache[type], input);
            lock (_commandBuilder)
            {
                if (CommandCache.Count != 0) return string.Format(CommandCache[type], input);
                BuildCommands();
                _commandsBuilt = true;
            }
            return CommandCache[type];
        }

        /// <summary>
        /// Adds a new cached command string
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string Add(string key, string value)
        {
            _commandCacheRunTime.TryAdd(key, new CommandString(value));
            return value;
        }

        /// <summary>
        /// Adds a new cached command string
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public CommandString Add(string key, CommandString value)
        {
            _commandCacheRunTime.TryAdd(key, value);
            return value;
        }

        /// <summary>
        /// Gets the specified command string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public CommandString Get(string key)
        {
            return _commandCacheRunTime.TryGetValue(key, out CommandString value) ? value : null;
        }

        /// <summary>
        /// Determines whether [contains] [the specified key].
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return _commandCacheRunTime.ContainsKey(key);
        }

        /// <summary>
        /// Builds the commands.
        /// </summary>
        protected abstract void BuildCommands();
    }

    /// <summary>
    /// Command types
    /// </summary>
    public enum CommandStringTypes
    {
        /// <summary>
        /// delete from meta data
        /// </summary>
        DeleteFromMetaData,
        /// <summary>
        /// delete from queue
        /// </summary>
        DeleteFromQueue,
        /// <summary>
        /// delete from error tracking
        /// </summary>
        DeleteFromErrorTracking,
        /// <summary>
        /// delete from status table
        /// </summary>
        DeleteFromStatus,
        /// <summary>
        /// save configuration
        /// </summary>
        SaveConfiguration,
        /// <summary>
        /// update status record
        /// </summary>
        UpdateStatusRecord,
        /// <summary>
        /// reset heartbeat
        /// </summary>
        ResetHeartbeat,
        /// <summary>
        /// send heart beat
        /// </summary>
        SendHeartBeat,
        /// <summary>
        /// insert message body
        /// </summary>
        InsertMessageBody,
        /// <summary>
        /// update error count
        /// </summary>
        UpdateErrorCount,
        /// <summary>
        /// insert error count
        /// </summary>
        InsertErrorCount,
        /// <summary>
        /// get heart beat expired message ids
        /// </summary>
        GetHeartBeatExpiredMessageIds,
        /// <summary>
        /// get column names from table
        /// </summary>
        GetColumnNamesFromTable,
        /// <summary>
        /// get error record exists
        /// </summary>
        GetErrorRecordExists,
        /// <summary>
        /// get error retry count
        /// </summary>
        GetErrorRetryCount,
        /// <summary>
        /// get configuration
        /// </summary>
        GetConfiguration,
        /// <summary>
        /// get table exists
        /// </summary>
        GetTableExists,
        /// <summary>
        /// get UTC date
        /// </summary>
        GetUtcDate,
        /// <summary>
        /// Gets the number of pending items from the queue
        /// </summary>
        GetPendingCount,
        /// <summary>
        /// Gets the number of pending items from the queue, not included items that are still delayed
        /// </summary>
        GetPendingExcludeDelayCount,
        /// <summary>
        /// Gets the number of working items from the queue
        /// </summary>
        GetWorkingCount,
        /// <summary>
        /// Gets the number of items that have stopped processing due to an error
        /// </summary>
        GetErrorCount,
        /// <summary>
        /// Gets the number of records that are pending, but are scheduled for a future time
        /// </summary>
        GetPendingDelayCount,
        /// <summary>
        /// Gets the last known event time for a job
        /// </summary>
        GetJobLastKnownEvent,
        /// <summary>
        /// Sets the last known event time for a job
        /// </summary>
        SetJobLastKnownEvent,
        /// <summary>
        /// Determines if a job (via job name) already is queued
        /// </summary>
        DoesJobExist,
        /// <summary>
        /// Gets job identifier (via job name)
        /// </summary>
        GetJobId,
        /// <summary>
        /// The get job last schedule time from the last time the job was queued
        /// </summary>
        GetJobLastScheduleTime,
        /// <summary>
        /// Deletes a record from the meta data error table
        /// </summary>
        DeleteFromMetaDataErrors,
        /// <summary>
        /// Finds expired records to delete, that are in a waiting status
        /// </summary>
        FindExpiredRecordsWithStatusToDelete,
        /// <summary>
        /// Finds expired records to delete
        /// </summary>
        FindExpiredRecordsToDelete,
        /// <summary>
        /// Deletes a table
        /// </summary>
        DeleteTable
    }

    /// <summary>
    /// Contains the primary command and any secondary commands that should be executed after the primary command completes.
    /// </summary>
    public class CommandString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandString"/> class.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        /// <param name="commands">The commands.</param>
        public CommandString(string commandString, List<string> commands)
        {
            CommandText = commandString;
            AdditionalCommands = commands;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandString"/> class.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        public CommandString(string commandString)
        {
            CommandText = commandString;
            AdditionalCommands = new List<string>(0);
        }
        /// <summary>
        /// Gets the command text.
        /// </summary>
        /// <value>
        /// The command text.
        /// </value>
        public string CommandText { get; }
        /// <summary>
        /// Gets the additional commands.
        /// </summary>
        /// <value>
        /// The additional commands.
        /// </value>
        public List<string> AdditionalCommands { get; }
    }
}
