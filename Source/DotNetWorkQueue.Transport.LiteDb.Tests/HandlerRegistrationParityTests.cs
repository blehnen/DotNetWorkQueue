using System;
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Tests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    /// <summary>
    /// Every handler this transport registers both sync and async must resolve, on each side, to a
    /// handler from the same assembly wrapped in the same decorators (GitHub #411).
    /// </summary>
    /// <remarks>
    /// A missing half still compiles and still resolves - a shared layer answers instead, or a
    /// decorator is just absent - so nothing else notices (#297, #301). The container is the real one,
    /// built without a server: liteDB opens its file while the container is built, so it gets a
    /// throwaway one.
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
            "ICommandHandler<MoveRecordToErrorQueueCommand<Int32>>",
            "ICommandHandler<RollbackMessageCommand<Int32>>",
            "ICommandHandler<SetErrorCountCommand<Int32>>",
            "ICommandHandlerWithOutput<DeleteMessageCommand<Int32>, Int64>",
            "ICommandHandlerWithOutput<SendHeartBeatCommand<Int32>, Nullable<DateTime>>",
            "ICommandHandlerWithOutput<SendMessageCommand, Int32>",
            "ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>",
            "IQueryHandler<GetErrorRetryCountQuery<Int32>, Int32>"
        };

        [TestMethod]
        public void SyncAndAsyncHandlers_ComeFromTheSameAssembly_WithTheSameDecorators()
        {
            var file = Path.Combine(Path.GetTempPath(), $"parity-{Guid.NewGuid():N}.ldb");
            try
            {
                using (var queueContainer = new QueueContainer<LiteDbMessageQueueInit>(c => { }))
                using (var container = queueContainer.CreateAdminContainer(new QueueConnection("parity", $"Filename={file};Connection=shared;")))
                {
                    HandlerRegistrationParity.AssertMatches(container, Pairs);
                }
            }
            finally
            {
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
    }
}
