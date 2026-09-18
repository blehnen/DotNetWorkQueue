using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// Queue names that PostgreSQL could not actually take (GitHub #375).
    ///
    /// Both cases used to end the same way: the CREATE failed, the failure was read as "this queue
    /// already exists", and that status carries Success - so the caller was told it had a queue when
    /// nothing had been created.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class QueueNameIdentifierTests
    {
        [TestMethod]
        public void TwoQueuesWhoseIdentifiersWouldTruncateAlike_CanBothBeCreated()
        {
            //PostgreSQL truncates identifiers at 63 bytes and index names are unique per schema, so
            //two queue names matching for their first 40 characters used to produce one index name -
            //the second CREATE failed with 42P07. The 51 character limit does not prevent this: it was
            //measured against two identifiers of the same queue colliding, and any two queues may
            //share a prefix.
            var shared = "u" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var prefix = (shared + new string('x', 40)).Substring(0, 40);

            var first = prefix + "aaa";
            var second = prefix + "bbb";
            Assert.AreEqual(first.Substring(0, 40), second.Substring(0, 40));

            //built one at a time and recorded as they are built, so that a failure part way through
            //still disposes what exists
            var queues = new List<QueueUnderTest>();
            try
            {
                foreach (var name in new[] { first, second })
                {
                    queues.Add(new QueueUnderTest(name));

                    var result = queues[queues.Count - 1].Creation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    //Success is not enough on its own here: the old failure reported
                    //AttemptedToCreateAlreadyExists, which also carries Success, having created nothing
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status);
                    Assert.IsTrue(queues[queues.Count - 1].Creation.QueueExists,
                        "the queue reported as created is not there");
                }
            }
            finally
            {
                //each one independently: a failure removing the first must not strand the second
                foreach (var queue in queues)
                {
                    queue.Dispose();
                }
            }
        }

        [TestMethod]
        public void AQueueNameContainingADot_IsRefusedWhereTheMistakeIs()
        {
            //a dot was accepted by the validator and then failed in the DDL, because the name goes in
            //unquoted and PostgreSQL reads it as a schema separator. It is now refused where the
            //mistake is, rather than several steps later as a queue that was said to exist already.
            var failure = Assert.ThrowsExactly<ArgumentException>(
                () => new SqlConnectionInformation(
                    new QueueConnection("dnwq.probe", ConnectionInfo.ConnectionString)));

            Assert.Contains("invalid characters", failure.Message);
            //and a name without one is still accepted
            var valid = new SqlConnectionInformation(
                new QueueConnection("dnwq_probe", ConnectionInfo.ConnectionString));
            Assert.AreEqual("dnwq_probe", valid.QueueName);
        }

        /// <summary>
        /// A queue and the container it came from, so that both are disposed and neither is left
        /// behind on the server if the test fails part way through.
        /// </summary>
        private sealed class QueueUnderTest : IDisposable
        {
            private readonly QueueCreationContainer<PostgreSqlMessageQueueInit> _container;

            public QueueUnderTest(string queueName)
            {
                _container = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
                try
                {
                    Creation = _container.GetQueueCreation<PostgreSqlMessageQueueCreation>(
                        new QueueConnection(queueName, ConnectionInfo.ConnectionString));
                }
                catch
                {
                    _container.Dispose();
                    throw;
                }
            }

            public PostgreSqlMessageQueueCreation Creation { get; }

            public void Dispose()
            {
                try
                {
                    //the queue may not have been created, and removing one that is not there is not a
                    //reason to leave the rest of this undisposed
                    Creation.RemoveQueue();
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    Creation.Dispose();
                    _container.Dispose();
                }
            }
        }
    }
}
