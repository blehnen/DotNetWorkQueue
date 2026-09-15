using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// The queue name length limit, against a real database.
    ///
    /// The limit lives in <c>SqlConnectionInformation</c> as a number, and a number in a validator is
    /// only as good as the behaviour it claims to describe. This asserts the claim: a name at the limit
    /// creates a queue, and the next character up is refused before it can reach the database.
    ///
    /// Without the first half, the limit could be lowered to anything and still look correct. Without
    /// the second, it could drift back up to a value that fails in a way the caller cannot see - which
    /// is what it did, reporting <see cref="QueueCreationStatus.AttemptedToCreateAlreadyExists"/> with
    /// <c>Success == true</c> after creating nothing (GitHub #339).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class LongestQueueNameTests
    {
        private const int Longest = 51;

        [TestMethod]
        public void AQueueNameAtTheLimit_Creates()
        {
            var queueName = NameOfLength(Longest);
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var container = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            {
                var creation = container.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {
                    creation.Options.EnableHistory = true;   //History carries the second colliding pair
                    var result = creation.CreateQueue();

                    //Status rather than Success: AttemptedToCreateAlreadyExists also reports success, and
                    //that is exactly the answer a truncation collision produced
                    Assert.AreEqual(QueueCreationStatus.Success, result.Status,
                        $"a {Longest} character name did not create a queue: {result.ErrorMessage}");
                }
                finally
                {
                    creation.RemoveQueue();
                    creation.Dispose();
                }
            }
        }

        [TestMethod]
        public void AQueueNameOverTheLimit_IsRefusedBeforeItReachesTheDatabase()
        {
            var tooLong = NameOfLength(Longest + 1);

            //the validator runs when the transport builds its connection information, which is where a
            //caller first hands the name to the library
            Assert.ThrowsExactly<ArgumentException>(
                () => new SqlConnectionInformation(new QueueConnection(tooLong, ConnectionInfo.ConnectionString)),
                "a name one character over the limit was accepted");
        }

        private static string NameOfLength(int length)
        {
            var name = GenerateQueueName.Create() + new string('n', length);
            return name.Substring(0, length);
        }
    }
}
