using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests
{
    /// <summary>
    /// Several queues being created at once.
    ///
    /// LiteDB maps a type through a process-wide BsonMapper, and mapping one for the first time is not
    /// thread safe - a second thread can see a half-built mapper and report a member that exists as
    /// missing. Creating tables is serialised because of it (GitHub #318).
    ///
    /// What this test can and cannot do is worth being plain about: the fault needs the *first* use of
    /// a type in the process, so once any other test in this assembly has touched LiteDB the mappers
    /// are warm and it will not reproduce here whatever the lock does. It is kept because creating
    /// queues concurrently is worth exercising on its own - a deadlock or a file-level collision in
    /// this path would fail it - and because the serialisation it covers has no other test.
    /// </summary>
    [TestClass]
    public class ConcurrentQueueCreation
    {
        [TestMethod]
        public void CreatingManyQueuesAtOnce_AllSucceed()
        {
            const int queues = 16;
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                var logProvider = LoggerShared.Create("concurrent-create", GetType().Name);
                using (var queueCreator = new QueueCreationContainer<LiteDbMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var created = new ConcurrentBag<LiteDbMessageQueueCreation>();
                    var failures = new ConcurrentBag<string>();
                    try
                    {
                        Parallel.For(0, queues, i =>
                        {
                            try
                            {
                                var connection = new QueueConnection(GenerateQueueName.Create(), connectionInfo.ConnectionString);
                                var creation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(connection);
                                created.Add(creation);
                                var result = creation.CreateQueue();
                                if (!result.Success)
                                    failures.Add($"{result.Status}: {result.ErrorMessage}");
                            }
                            catch (Exception error)
                            {
                                failures.Add(error.GetType().Name + ": " + error.Message);
                            }
                        });

                        Assert.IsEmpty(failures, string.Join(" | ", failures));
                    }
                    finally
                    {
                        foreach (var creation in created)
                        {
                            try { creation.RemoveQueue(); } catch { /* best effort */ }
                            creation.Dispose();
                        }
                    }
                }
            }
        }
    }
}
