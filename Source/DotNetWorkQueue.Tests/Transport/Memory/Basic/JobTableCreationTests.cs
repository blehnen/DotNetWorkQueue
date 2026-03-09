using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class JobTableCreationTests
    {
        [TestMethod]
        public void CreateJobTable_Test()
        {
            var create = new JobTableCreation();
            var result = create.CreateJobTable();
            Assert.IsTrue(create.JobTableExists); //returns true even if not called, as the structure is created at startup of the queue
            Assert.AreEqual(QueueCreationStatus.Success, result.Status); //a no-op, but returns success as we already created it
            Assert.IsTrue(create.JobTableExists);
        }
    }
}