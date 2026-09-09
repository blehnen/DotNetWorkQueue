using System;
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    /// <summary>
    /// Container registration for the asynchronous poison-message path (#284).
    ///
    /// LiteDB gains nothing from this in itself - it has no asynchronous API until v6 (#283) - but
    /// the shared <c>ReceivePoisonMessage</c> every transport resolves now takes both handlers, so a
    /// missing registration here fails at run time in the consumer rather than at compile time.
    /// </summary>
    [TestClass]
    public class PoisonMessageAsyncRegistrationTests
    {
        private string _database;

        [TestInitialize]
        public void Setup()
        {
            //A path of its own per run: the container reaches the file, and a shared name would make
            //this test depend on whatever a previous run left behind.
            _database = Path.Combine(Path.GetTempPath(),
                $"dnwq-poison-registration-{Guid.NewGuid():N}.db");
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { File.Delete(_database); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        [TestMethod]
        public void MoveRecordToErrorQueue_ResolvesForTheAsynchronousPath()
        {
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<int>> handler = null;
            IReceivePoisonMessage poisonMessage = null;

            using (var qc = new QueueContainer<LiteDbMessageQueueInit>(
                registerService: _ => { },
                setOptions: container =>
                {
                    handler = container.GetInstance<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<int>>>();
                    poisonMessage = container.GetInstance<IReceivePoisonMessage>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("poison_registration", _database)); }
                catch { /* the database does not exist; the resolutions above are the point */ }
            }

            Assert.IsNotNull(handler, "The asynchronous move-to-error-queue handler did not resolve.");
            Assert.IsNotNull(poisonMessage, "IReceivePoisonMessage did not resolve.");
        }
    }
}
