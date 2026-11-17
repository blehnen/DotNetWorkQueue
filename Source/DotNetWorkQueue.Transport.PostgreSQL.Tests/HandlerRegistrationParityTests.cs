using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Tests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests
{
    /// <summary>
    /// Every handler this transport registers both sync and async must resolve, on each side, to a
    /// handler from the same assembly wrapped in the same decorators (GitHub #411).
    /// </summary>
    /// <remarks>
    /// A missing half still compiles and still resolves - a shared layer answers instead, or a
    /// decorator is just absent - so nothing else notices (#297, #301). The container is the real one,
    /// built without a server: the queue options are read from the database, so they are supplied
    /// instead.
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
            "ICommandHandler<DeleteMetaDataCommand>",
            "ICommandHandler<DeleteStatusTableStatusCommand<Int64>>",
            "ICommandHandler<MoveRecordToErrorQueueCommand<Int64>>",
            "ICommandHandler<RollbackMessageCommand<Int64>>",
            "ICommandHandler<SetErrorCountCommand<Int64>>",
            "ICommandHandler<SetStatusTableStatusCommand<Int64>>",
            "ICommandHandler<SetStatusTableStatusTransactionCommand>",
            "ICommandHandlerWithOutput<DeleteMessageCommand<Int64>, Int64>",
            "ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, Int64>",
            "ICommandHandlerWithOutput<SendHeartBeatCommand<Int64>, Nullable<DateTime>>",
            "ICommandHandlerWithOutput<SendMessageCommand, Int64>",
            "ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>",
            "IQueryHandler<GetDashboardConfigurationQuery, Byte[]>",
            "IQueryHandler<GetDashboardErrorMessageCountQuery, Int64>",
            "IQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>",
            "IQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>",
            "IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>",
            "IQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody>",
            "IQueryHandler<GetDashboardMessageCountQuery, Int64>",
            "IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>",
            "IQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>",
            "IQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>",
            "IQueryHandler<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>",
            "IQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts>",
            "IQueryHandler<GetErrorRetryCountQuery<Int64>, Int32>"
        };

        [TestMethod]
        public void SyncAndAsyncHandlers_ComeFromTheSameAssembly_WithTheSameDecorators()
        {
            using var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(c => c.Register<ITransportOptionsFactory>(() => new DefaultOptions(), LifeStyles.Singleton));
            using var container = queueContainer.CreateAdminContainer(new QueueConnection("parity", "Host=127.0.0.1;Port=1;Database=parity;Timeout=1;"));

            HandlerRegistrationParity.AssertMatches(container, Pairs);
        }

        private sealed class DefaultOptions : ITransportOptionsFactory
        {
            public ITransportOptions Create() => new PostgreSqlMessageQueueTransportOptions();
        }
    }
}
