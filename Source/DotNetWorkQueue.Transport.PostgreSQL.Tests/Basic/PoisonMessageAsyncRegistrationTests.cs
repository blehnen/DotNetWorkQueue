using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    /// <summary>
    /// Container registration for the asynchronous poison-message path (#284).
    ///
    /// The handler is only half of the change: an <see cref="ICommandHandlerAsync{TCommand}"/> that
    /// resolves without its retry decorator would move a message to the error queue on the first
    /// transient failure instead of retrying, and nothing else in the suite would notice - the
    /// synchronous twin beside it retries either way.
    /// </summary>
    [TestClass]
    public class PoisonMessageAsyncRegistrationTests
    {
        private const string FakeConnection =
            "Server=localhost;Application Name=Test;Database=db;User ID=sa;Password=password";

        [TestMethod]
        public void MoveRecordToErrorQueue_ResolvesWrappedInTheRetryDecorator()
        {
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>> handler = null;
            IReceivePoisonMessage poisonMessage = null;

            using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>(
                registerService: container =>
                {
                    //Without this the container reaches the database for its defaults before the
                    //callback below ever runs.
                    var stubFactory = Substitute.For<ITransportOptionsFactory>();
                    stubFactory.Create().Returns(new PostgreSqlMessageQueueTransportOptions());
                    container.Register<ITransportOptionsFactory>(() => stubFactory, LifeStyles.Singleton);
                },
                setOptions: container =>
                {
                    handler = container.GetInstance<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>();
                    poisonMessage = container.GetInstance<IReceivePoisonMessage>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection)); }
                catch { /* the fake connection fails further down; the resolutions above are the point */ }
            }

            Assert.IsNotNull(handler, "The asynchronous move-to-error-queue handler did not resolve.");
            Assert.IsInstanceOfType<RetryCommandHandlerDecoratorAsync<MoveRecordToErrorQueueCommand<long>>>(handler,
                "The asynchronous handler resolved without its retry decorator, so a transient failure " +
                "would move the message to the error queue instead of being retried.");
            Assert.IsNotNull(poisonMessage, "IReceivePoisonMessage did not resolve.");
        }
    }
}
