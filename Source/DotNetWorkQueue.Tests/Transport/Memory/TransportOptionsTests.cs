using DotNetWorkQueue.Transport.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    [TestClass]
    public class TransportOptionsTests
    {
        [TestMethod]
        public void ValidConfiguration_Test()
        {
            var options = new TransportOptions();
            var valid = options.ValidConfiguration();
            Assert.IsTrue(valid.Valid);
            options.SetReadOnly();
            valid = options.ValidConfiguration();
            Assert.IsTrue(valid.Valid);
        }

        [TestMethod]
        public void SetReadOnly_Test()
        {
            var options = new TransportOptions();
            Assert.IsFalse(options.IsReadOnly);
            options.SetReadOnly();
            Assert.IsTrue(options.IsReadOnly);
        }
    }
}