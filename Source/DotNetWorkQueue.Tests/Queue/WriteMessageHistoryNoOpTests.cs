using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WriteMessageHistoryNoOpTests
    {
        [TestMethod]
        public void RecordEnqueue_DoesNotThrow()
        {
            var test = Create();
            test.RecordEnqueue("1", "corr-1", "route", "MyType", null, null);
        }

        [TestMethod]
        public void RecordProcessingStart_DoesNotThrow()
        {
            var test = Create();
            test.RecordProcessingStart("1");
        }

        [TestMethod]
        public void RecordComplete_DoesNotThrow()
        {
            var test = Create();
            test.RecordComplete("1");
        }

        [TestMethod]
        public void RecordError_DoesNotThrow()
        {
            var test = Create();
            test.RecordError("1", "some exception");
        }

        [TestMethod]
        public void RecordRollback_DoesNotThrow()
        {
            var test = Create();
            test.RecordRollback("1");
        }

        [TestMethod]
        public void RecordDelete_DoesNotThrow()
        {
            var test = Create();
            test.RecordDelete("1");
        }

        [TestMethod]
        public void RecordExpire_DoesNotThrow()
        {
            var test = Create();
            test.RecordExpire("1");
        }

        private IWriteMessageHistory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WriteMessageHistoryNoOp>();
        }
    }
}
