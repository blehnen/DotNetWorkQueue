using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Basic
{
    /// <summary>
    /// The queue name length limit, against a real server.
    ///
    /// The limit lives in <c>SqlConnectionInformation</c> as a number, and a number in a validator is
    /// only as good as the behaviour it claims to describe. This asserts the claim from both sides: a
    /// name at the limit creates a queue, and one character more is refused before it reaches the server.
    ///
    /// History is on deliberately. The longest identifier the schema builds is
    /// <c>IX_{name}History_Status_Completed</c>, so the limit is the one that applies with history
    /// enabled - with it off, longer names create successfully. Taking the lower number is what stops a
    /// queue working today and breaking when somebody enables history later (GitHub #344).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class LongestQueueNameTests
    {
        private const int Longest = 101;

        [TestMethod]
        public void AQueueNameAtTheLimit_Creates()
        {
            var queueName = NameOfLength(Longest);
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

            using (var container = new QueueCreationContainer<SqlServerMessageQueueInit>())
            {
                var creation = container.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    //the option that produces the longest identifier of all
                    creation.Options.EnableHistory = true;
                    var result = creation.CreateQueue();

                    //Status rather than Success: AttemptedToCreateAlreadyExists also reports success
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
        public void AQueueNameOverTheLimit_IsRefusedBeforeItReachesTheServer()
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
