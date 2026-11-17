using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Tests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    /// <summary>
    /// Every handler this transport registers both sync and async must resolve, on each side, to a
    /// handler from the same assembly wrapped in the same decorators (GitHub #411).
    /// </summary>
    /// <remarks>
    /// A missing half still compiles and still resolves - a shared layer answers instead, or a
    /// decorator is just absent - so nothing else notices (#297, #301). The container is the real one,
    /// built without a server: the options are read from the server and the Lua scripts are loaded onto
    /// it, so both are replaced.
    /// </remarks>
    [TestClass]
    public class HandlerRegistrationParityTests
    {
        /// <summary>
        /// The sync service of every pair. Pinned so that deleting an async registration fails here,
        /// rather than turning the pair into a sync-only service.
        /// </summary>
        private static readonly string[] Pairs =
        {
            "ICommandHandler<MoveRecordToErrorQueueCommand<String>>",
            "ICommandHandler<RollbackMessageCommand<String>>",
            "ICommandHandler<SaveMetaDataCommand>",
            "ICommandHandlerWithOutput<DeleteMessageCommand<String>, Boolean>",
            "ICommandHandlerWithOutput<SendHeartBeatCommand<String>, Int64>",
            "IQueryHandler<GetMetaDataQuery, RedisMetaData>",
            "IQueryHandler<ReceiveMessageQuery, RedisMessage>"
        };

        [TestMethod]
        public void SyncAndAsyncHandlers_ComeFromTheSameAssembly_WithTheSameDecorators()
        {
            using var queueContainer = new QueueContainer<RedisQueueInit>(c =>
            {
                c.Register<IBaseTransportOptions>(() => new RedisBaseTransportOptions(), LifeStyles.Singleton);
                c.Register(() => new LuaScripts(Array.Empty<BaseLua>()), LifeStyles.Singleton);
            });
            using var container = queueContainer.CreateAdminContainer(new QueueConnection("parity", "127.0.0.1:1,connectTimeout=100,abortConnect=false"));

            HandlerRegistrationParity.AssertMatches(container, Pairs);
        }
    }
}
