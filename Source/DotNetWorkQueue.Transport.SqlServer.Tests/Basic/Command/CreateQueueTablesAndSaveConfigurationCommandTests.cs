using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.Command
{
    [TestClass]
    public class CreateQueueTablesAndSaveConfigurationCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var tables = new List<Table>();
            var test = new CreateQueueTablesAndSaveConfigurationCommand<Table>(tables);
            Assert.AreEqual(tables, test.Tables);
        }
    }
}
