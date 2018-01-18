using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Dequeues the next record for a Rpc
    /// </summary>
    internal class DequeueRpcLua: BaseLua
    {
        private string[] _routes;
        private int _nextRoute;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly object _routeInit = new object();

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DequeueRpcLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="configuration">The configuration.</param>
        public DequeueRpcLua(IRedisConnection connection, RedisNames redisNames, QueueConsumerConfiguration configuration)
            : base(connection, redisNames)
        {
            _configuration = configuration;
            Script = @"local count = redis.call('LREM', @pendingkey, 1, @uuid) 
                    if (count==0) then 
                        return nil;
                    end                   
                    local expireScore = redis.call('zscore', @expirekey, @uuid)
                    redis.call('zadd', @workingkey, @timestamp, @uuid) 
                    local message = redis.call('hget', @valueskey, @uuid) 
                    redis.call('hset', @StatusKey, @uuid, '1') 
                    local headers = redis.call('hget', @headerskey, @uuid)
                    return {@uuid, message, headers, expireScore}";
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public RedisValue[] Execute(string messageId, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            InitRoutes();

            var db = Connection.Connection.GetDatabase();
            if (_routes == null)
                return (RedisValue[])db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, unixTime, null));

            var counter = 0;
            while (counter < _routes.Length)
            {
                var route = _routes[GetNextRoute()];
                var result = db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageId, unixTime, route));
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
                counter++;
            }

            return null;
        }
        /// <summary>
        /// Dequeues the next record for a Rpc.
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public async Task<RedisValue[]> ExecuteAsync(string messageId, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            InitRoutes();

            var db = Connection.Connection.GetDatabase();
            if (_routes == null)
            {
                var result =
                    await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageId, unixTime, null))
                        .ConfigureAwait(false);
                return (RedisValue[]) result;
            }

            var counter = 0;
            while (counter < _routes.Length)
            {
                var route = _routes[GetNextRoute()];
                var result =
                   await db.ScriptEvaluateAsync(LoadedLuaScript, GetParameters(messageId, unixTime, route))
                       .ConfigureAwait(false);
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
                counter++;
            }

            return null;
        }
        private int GetNextRoute()
        {
            if (_routes.Length == 1)
                return 0;

            var number = Interlocked.Increment(ref _nextRoute);
            if (number < _routes.Length) return number;
            Interlocked.Exchange(ref _nextRoute, -1);
            return 0;
        }
        private void InitRoutes()
        {
            if (_routes != null || _configuration.Routes == null || _configuration.Routes.Count == 0) return;
            lock (_routeInit)
            {
                if (_routes != null) return;
                _routes = _configuration.Routes.ToArray();
                _nextRoute = -1;
            }
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageId">The messageId.</param>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(string messageId, long unixTime, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            return new
            {
                pendingkey = (RedisKey)pendingKey,
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                headerskey = (RedisKey)RedisNames.Headers,
                valueskey = (RedisKey)RedisNames.Values,
                expirekey = (RedisKey)RedisNames.Expiration,
                StatusKey = (RedisKey)RedisNames.Status,
                uuid = messageId
            };
        }
    }
}
