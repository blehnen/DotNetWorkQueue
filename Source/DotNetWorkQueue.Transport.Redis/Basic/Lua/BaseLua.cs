using System;
using System.Linq;
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
