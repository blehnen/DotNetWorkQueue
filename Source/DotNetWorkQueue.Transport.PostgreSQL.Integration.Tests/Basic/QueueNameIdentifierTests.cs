using System;
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

            var creations = new[] { Create(first), Create(second) };
            try
            {
                foreach (var creation in creations)
                {
                    var result = creation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    //Success is not enough on its own here: the old failure reported
                    //AttemptedToCreateAlreadyExists, which also carries Success, having created nothing
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status);
                    Assert.IsTrue(creation.QueueExists, "the queue reported as created is not there");
                }
            }
            finally
            {
                foreach (var creation in creations)
                {
                    try { creation.RemoveQueue(); } finally { creation.Dispose(); }
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

        private static PostgreSqlMessageQueueCreation Create(string queueName)
        {
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
            var container = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            return container.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
        }
    }
}
