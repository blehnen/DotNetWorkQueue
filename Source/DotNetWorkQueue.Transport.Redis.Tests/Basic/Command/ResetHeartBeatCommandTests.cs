using System;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class ResetHeartBeatCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new ResetHeartBeatCommand<string>(new MessageToReset<string>(string.Empty, DateTime.Now, null));
            Assert.IsNotNull(test);
        }
    }
}
