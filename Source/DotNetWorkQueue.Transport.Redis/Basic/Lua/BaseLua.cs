// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Caches lua script on each configured redis server
    /// </summary>
    public abstract class BaseLua
    {
        private LoadedLuaScript _loadedLuaScript;
        /// <summary>
        /// The connection to the redis server(s)
        /// </summary>
        protected readonly IRedisConnection Connection;
        /// <summary>
        /// The names of the various redis queues
        /// </summary>
        protected readonly RedisNames RedisNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        protected BaseLua(IRedisConnection connection, RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            Connection = connection;
            RedisNames = redisNames;
        }
        
        /// <summary>
        /// Gets or sets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        public string Script { get; protected set; }

        /// <summary>
        /// Gets the loaded lua script.
        /// </summary>
        /// <value>
        /// The loaded lua script.
        /// </value>
        /// <exception cref="DotNetWorkQueueException">Script has not been loaded</exception>
        public LoadedLuaScript LoadedLuaScript
        {
            get
            {
                if (_loadedLuaScript == null) throw new InvalidOperationException("Script has not been loaded");
                return _loadedLuaScript;
            }
            private set => _loadedLuaScript = value;
        }

        /// <summary>
        /// Tries to execute the loaded script.  If the script is no longer cached, it will re-cache it and try again.
        /// </summary>
        /// <param name="parameters">The parameters. Pass null if there are none.</param>
        /// <returns></returns>
        public RedisResult TryExecute(object parameters)
        {
            if (Connection.IsDisposed)
                return RedisResult.Create(RedisValue.Null);

            var db = Connection.Connection.GetDatabase();
            try
            {
                return parameters != null ? db.ScriptEvaluate(LoadedLuaScript, parameters) : db.ScriptEvaluate(LoadedLuaScript);
            }
            catch (RedisException e)
            {
                if (e.Message.StartsWith("NOSCRIPT",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    LoadScript();
                    return parameters != null ? db.ScriptEvaluate(LoadedLuaScript, parameters) : db.ScriptEvaluate(LoadedLuaScript);
                }
                throw;
            }
        }

        /// <summary>
        /// Tries to execute the loaded script.  If the script is no longer cached, it will re-cache it and try again.
        /// </summary>
        /// <param name="parameters">The parameters. Pass null if there are none.</param>
        /// <returns></returns>
        public async  Task<RedisResult> TryExecuteAsync(object parameters)
        {
            if (Connection.IsDisposed)
                return RedisResult.Create(RedisValue.Null);

            var db = Connection.Connection.GetDatabase();
            try
            {
                if(parameters != null)
                    return await db.ScriptEvaluateAsync(LoadedLuaScript, parameters).ConfigureAwait(false);
                return await db.ScriptEvaluateAsync(LoadedLuaScript).ConfigureAwait(false);
            }
            catch (RedisException e)
            {
                //there does not appear to be an error code we can look at, so see if the message starts with 'NOSCRIPT'
                if (e.Message.StartsWith("NOSCRIPT",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    LoadScript();
                    if(parameters != null)
                        return await db.ScriptEvaluateAsync(LoadedLuaScript, parameters).ConfigureAwait(false);
                    return await db.ScriptEvaluateAsync(LoadedLuaScript).ConfigureAwait(false);
                }
                throw;
            }
        }

        /// <summary>
        /// Loads the script.
        /// </summary>
        public void LoadScript()
        {
            Guard.NotNullOrEmpty(() => Script, Script);

            var luaScript = LuaScript.Prepare(Script);
            var endpoints = Connection.Connection.GetEndPoints();
            Guard.IsValid(() => endpoints.Length, endpoints.Length, i => i > 0,
                "No endpoints where found; the count was 0");
            LoadedLuaScript loadedScript = null;
            foreach (var server in endpoints.Select(endpoint => Connection.Connection.GetServer(endpoint)))
            {
                loadedScript = luaScript.Load(server);
            }
            LoadedLuaScript = loadedScript; //set cached copy to last copy created.
        }
    }
}
