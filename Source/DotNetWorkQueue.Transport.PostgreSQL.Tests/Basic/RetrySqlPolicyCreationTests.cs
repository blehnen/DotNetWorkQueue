using System.Collections.Concurrent;
using System.Diagnostics;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class RetrySqlPolicyCreationTests
    {
        [TestMethod]
        public void Register_Adds_RetryCommandHandler_Definition()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryCommandHandler));
        }

        [TestMethod]
        public void Register_Adds_RetryCommandHandlerAsync_Definition()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryCommandHandlerAsync));
        }

        [TestMethod]
        public void Register_Adds_RetryQueryHandler_Definition()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            Assert.IsTrue(policies.TransportDefinition.ContainsKey(TransportPolicyDefinitions.RetryQueryHandler));
        }

        [TestMethod]
        public void Register_Adds_Three_Definitions()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            // RetryCommandHandler and RetryCommandHandlerAsync map to the same key
            Assert.HasCount(2, policies.TransportDefinition);
        }

        [TestMethod]
        public void Register_RetryCommandHandler_Definition_Has_Correct_Name()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            var definition = policies.TransportDefinition[TransportPolicyDefinitions.RetryCommandHandler];
            Assert.AreEqual(TransportPolicyDefinitions.RetryCommandHandler, definition.Name);
        }

        [TestMethod]
        public void Register_RetryQueryHandler_Definition_Has_Correct_Name()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            var definition = policies.TransportDefinition[TransportPolicyDefinitions.RetryQueryHandler];
            Assert.AreEqual(TransportPolicyDefinitions.RetryQueryHandler, definition.Name);
        }

        [TestMethod]
        public void Register_Pipeline_Can_Be_Retrieved_For_RetryCommandHandler()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryCommandHandler);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        public void Register_Pipeline_Can_Be_Retrieved_For_RetryQueryHandler()
        {
            var (container, policies) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryQueryHandler);
            Assert.IsNotNull(pipeline);
        }

        [TestMethod]
        public void Register_Does_Not_Throw()
        {
            var (container, _) = CreateMocks();
            RetrySqlPolicyCreation.Register(container);
        }

        [TestMethod]
        public void Register_With_Chaos_Enabled_Does_Not_Throw()
        {
            var (container, policies) = CreateMocks();
            policies.EnableChaos = true;
            RetrySqlPolicyCreation.Register(container);
        }

        [TestMethod]
        public void Register_With_Chaos_Enabled_Pipeline_Can_Be_Retrieved()
        {
            var (container, policies) = CreateMocks();
            policies.EnableChaos = true;
            RetrySqlPolicyCreation.Register(container);
            var pipeline = policies.Registry.GetPipeline(TransportPolicyDefinitions.RetryCommandHandler);
            Assert.IsNotNull(pipeline);
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
