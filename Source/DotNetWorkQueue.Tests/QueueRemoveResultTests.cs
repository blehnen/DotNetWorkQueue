using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests
{
    [TestClass]
    public class QueueRemoveResultTests
    {
        [TestMethod]
        public void Create_SetStatus()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.AreEqual(QueueRemoveStatus.DoesNotExist, test.Status);
        }

        [TestMethod]
        public void Create_Success()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.IsFalse(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.Success);
            Assert.IsTrue(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.None);
            Assert.IsFalse(test.Success);
        }
    }
}
