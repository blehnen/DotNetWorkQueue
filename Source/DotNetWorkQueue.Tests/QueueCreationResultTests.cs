using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests
{
    [TestClass]
    public class QueueCreationResultTests
    {
        [TestMethod]
        public void Create_Ok()
        {
            var test = new QueueCreationResult(QueueCreationStatus.None, null);
            Assert.AreEqual(QueueCreationStatus.None, test.Status);
        }
        [TestMethod]
        public void GetSet_Status()
        {
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, null);
            Assert.AreEqual(QueueCreationStatus.AlreadyExists, test.Status);
        }
        [TestMethod]
        public void GetSet_ErrorMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<string>();
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, message);
            Assert.AreEqual(message, test.ErrorMessage);
        }
        [TestMethod]
        public void GetSet_ErrorMessage_True()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<string>();
            var test = new QueueCreationResult(QueueCreationStatus.Success, message);
            Assert.IsTrue(test.Success);
        }
        [TestMethod]
        public void GetSet_ErrorMessage_False()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<string>();
            var test = new QueueCreationResult(QueueCreationStatus.ConfigurationError, message);
            Assert.IsFalse(test.Success);
        }
    }
}
