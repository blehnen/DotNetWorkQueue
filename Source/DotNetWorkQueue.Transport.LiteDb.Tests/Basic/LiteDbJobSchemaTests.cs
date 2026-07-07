using System.Linq;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class LiteDbJobSchemaTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new LiteDbJobSchema());
        }

        [TestMethod]
        public void GetSchema_Returns_Non_Empty_List()
        {
            var schema = new LiteDbJobSchema();
            var tables = schema.GetSchema();
            Assert.IsNotEmpty(tables);
        }

        [TestMethod]
        public void GetSchema_Returns_JobsTable()
        {
            var schema = new LiteDbJobSchema();
            var tables = schema.GetSchema();
            Assert.Contains(t => t is JobsTable, tables);
        }

        [TestMethod]
        public void GetSchema_Returns_Exactly_One_Table()
        {
            var schema = new LiteDbJobSchema();
            var tables = schema.GetSchema();
            Assert.HasCount(1, tables);
        }
    }
}
