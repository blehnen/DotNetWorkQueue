using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class RetryTransactionPolicyCreationTests
    {
        [TestMethod]
        public void Register_Adds_RetryCommandHandler_Definition()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryCommandHandler));
        }

        [TestMethod]
        public void Register_Adds_RetryCommandHandlerAsync_Definition()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryCommandHandlerAsync));
        }

        [TestMethod]
        public void Register_Adds_RetryQueryHandler_Definition()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryQueryHandler));
        }

        [TestMethod]
        public void Register_Adds_BeginTransaction_Definition()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.BeginTransaction));
        }

        [TestMethod]
        public void Register_Adds_Four_Definitions()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            // RetryCommandHandler and RetryCommandHandlerAsync map to the same key
            Assert.AreEqual(3, policies.TransportDefinition.Count);
        }

        [TestMethod]
        public void Register_RetryCommandHandler_Definition_Has_Correct_Name()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var definition = policies.TransportDefinition[TransportPolicyDefinitions.RetryCommandHandler];
            Assert.AreEqual(TransportPolicyDefinitions.RetryCommandHandler, definition.Name);
        }

        [TestMethod]
        public void Register_RetryQueryHandler_Definition_Has_Correct_Name()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var definition = policies.TransportDefinition[TransportPolicyDefinitions.RetryQueryHandler];
            Assert.AreEqual(TransportPolicyDefinitions.RetryQueryHandler, definition.Name);
        }

        [TestMethod]
        public void Register_BeginTransaction_Definition_Has_Correct_Name()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var definition = policies.TransportDefinition[TransportPolicyDefinitions.BeginTransaction];
            Assert.AreEqual(TransportPolicyDefinitions.BeginTransaction, definition.Name);
        }

        [TestMethod]
        public void Register_Pipeline_Can_Be_Retrieved_For_RetryCommandHandler()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryCommandHandler);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        public void Register_Pipeline_Can_Be_Retrieved_For_RetryQueryHandler()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryQueryHandler);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        public void Register_Pipeline_Can_Be_Retrieved_For_BeginTransaction()
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.BeginTransaction);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        public void Register_Does_Not_Throw()
        {
            var (container, _) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
        }

        [TestMethod]
        public void Register_With_Chaos_Enabled_Does_Not_Throw()
        {
            var (container, policies) = CreateMocks();
            policies.EnableChaos = true;
            RetryTransactionPolicyCreation.Register(container);
        }

        [TestMethod]
        public void Register_With_Chaos_Enabled_Pipeline_Can_Be_Retrieved()
        {
            var (container, policies) = CreateMocks();
            policies.EnableChaos = true;
            RetryTransactionPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryCommandHandler);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        [DataRow(5, true, DisplayName = "Base code 5 (SQLITE_BUSY) should retry")]
        [DataRow(6, true, DisplayName = "Base code 6 (SQLITE_LOCKED) should retry")]
        [DataRow(262, true, DisplayName = "Extended code 262 (SQLITE_LOCKED_SHAREDCACHE) should retry")]
        [DataRow(517, true, DisplayName = "Extended code 517 (SQLITE_BUSY_RECOVERY) should retry")]
        [DataRow(1, false, DisplayName = "Code 1 (SQLITE_ERROR) should not retry")]
        public void Retry_Handles_Base_And_Extended_SQLite_Error_Codes(int errorCode, bool shouldRetry)
        {
            var (container, policies) = CreateMocks();
            RetryTransactionPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryCommandHandler);

            var callCount = 0;
            try
            {
                pipeline.Execute(() =>
                {
                    callCount++;
                    throw new SQLiteException((SQLiteErrorCode)errorCode, "test");
                });
            }
            catch (SQLiteException)
            {
                // expected after retries exhausted or no retry
            }

            if (shouldRetry)
                Assert.IsTrue(callCount > 1, $"Error code {errorCode} should have triggered retries but was called only {callCount} time(s)");
            else
                Assert.AreEqual(1, callCount, $"Error code {errorCode} should not have triggered retries");
        }

        private static (IContainer container, IPolicies policies) CreateMocks()
        {
            var container = Substitute.For<IContainer>();
            var policies = CreateRealPolicies();
            container.GetInstance<IPolicies>().Returns(policies);
            container.GetInstance<ActivitySource>().Returns(new ActivitySource("test"));
            container.GetInstance<ILogger>().Returns(Substitute.For<ILogger>());
            return (container, policies);
        }

        private static IPolicies CreateRealPolicies()
        {
            var registry = new ResiliencePipelineRegistry<string>();
            var definitions = new PolicyDefinitions();
            return new DotNetWorkQueue.Policies.Policies(registry, definitions);
        }
    }
}
