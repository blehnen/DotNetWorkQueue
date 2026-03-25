using System;
using DotNetWorkQueue.Admin;
using DotNetWorkQueue.Configuration;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Admin
{
    [TestClass]
    public class AdminApiTests
    {
        [TestMethod]
        public void Create_Sets_Configuration()
        {
            var config = new AdminApiConfiguration();
            var api = new AdminApi(config);
            Assert.AreSame(config, api.Configuration);
        }

        [TestMethod]
        public void Connections_Initially_Empty()
        {
            var api = Create();
            Assert.AreEqual(0, api.Connections.Count);
        }

        [TestMethod]
        public void AddQueueConnection_Returns_Guid()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            var id = api.AddQueueConnection(container, connection);
            Assert.AreNotEqual(Guid.Empty, id);
        }

        [TestMethod]
        public void AddQueueConnection_Adds_To_Connections()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            var id = api.AddQueueConnection(container, connection);

            Assert.AreEqual(1, api.Connections.Count);
            Assert.IsTrue(api.Connections.ContainsKey(id));
            Assert.AreSame(connection, api.Connections[id].Item2);
            Assert.AreSame(container, api.Connections[id].Item1);
        }

        [TestMethod]
        public void AddQueueConnection_Multiple_Creates_Multiple_Entries()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection1 = new QueueConnection("test1", "connection1");
            var connection2 = new QueueConnection("test2", "connection2");

            var id1 = api.AddQueueConnection(container, connection1);
            var id2 = api.AddQueueConnection(container, connection2);

            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(2, api.Connections.Count);
        }

        [TestMethod]
        public void Count_Calls_AdminFunctions()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            var functions = Substitute.For<IAdminFunctions>();
            functions.Count(QueueStatusAdmin.Waiting).Returns(42L);
            container.CreateAdminFunctions(connection).Returns(functions);

            var id = api.AddQueueConnection(container, connection);
            var result = api.Count(id, QueueStatusAdmin.Waiting);

            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void Count_With_Null_Status_Calls_AdminFunctions()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            var functions = Substitute.For<IAdminFunctions>();
            functions.Count(null).Returns(100L);
            container.CreateAdminFunctions(connection).Returns(functions);

            var id = api.AddQueueConnection(container, connection);
            var result = api.Count(id, null);

            Assert.AreEqual(100L, result);
        }

        [TestMethod]
        public void Count_Caches_AdminFunctions()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            var functions = Substitute.For<IAdminFunctions>();
            functions.Count(null).Returns(10L);
            container.CreateAdminFunctions(connection).Returns(functions);

            var id = api.AddQueueConnection(container, connection);
            api.Count(id, null);
            api.Count(id, null);

            container.Received(1).CreateAdminFunctions(connection);
        }

        [TestMethod]
        public void Count_Unknown_Id_Throws()
        {
            var api = Create();
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                api.Count(Guid.NewGuid(), null));
        }

        [TestMethod]
        public void IsDisposed_Initially_False()
        {
            var api = Create();
            Assert.IsFalse(api.IsDisposed);
        }

        [TestMethod]
        public void Dispose_Sets_IsDisposed()
        {
            var api = Create();
            api.Dispose();
            Assert.IsTrue(api.IsDisposed);
        }

        [TestMethod]
        public void Dispose_Clears_Connections()
        {
            var api = Create();
            var container = Substitute.For<IQueueContainer>();
            var connection = new QueueConnection("test", "connection");
            api.AddQueueConnection(container, connection);
            api.Dispose();

            Assert.AreEqual(0, api.Connections.Count);
        }

        [TestMethod]
        public void Dispose_Multiple_Times_Does_Not_Throw()
        {
            var api = Create();
            api.Dispose();
            api.Dispose();
        }

        [TestMethod]
        public void AddQueueConnection_After_Dispose_Throws()
        {
            var api = Create();
            api.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                var container = Substitute.For<IQueueContainer>();
                var connection = new QueueConnection("test", "connection");
                api.AddQueueConnection(container, connection);
            });
        }

        [TestMethod]
        public void Count_After_Dispose_Throws()
        {
            var api = Create();
            api.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
                api.Count(Guid.NewGuid(), null));
        }

        private AdminApi Create()
        {
            return new AdminApi(new AdminApiConfiguration());
        }
    }
}
