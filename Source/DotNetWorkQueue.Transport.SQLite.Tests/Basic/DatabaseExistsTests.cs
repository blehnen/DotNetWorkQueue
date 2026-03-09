using System.IO;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.SQLite.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class DatabaseExistsTests
    {
        private const string BadConnection =
           "Thisisabadconnectionstring";

        private const string GoodConnectionInMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

        [TestMethod]
        public void DatabaseDoesNotExist()
        {
            var db = Create(BadConnection, false, "badfile.db3");
            Assert.IsFalse(db.Exists(BadConnection));
        }

        [TestMethod]
        public void DatabaseDoesExist()
        {
            var fileName = Path.GetTempFileName();
            var connectionString = $"Data Source={fileName};Version=3;";
            var db = Create(connectionString, false, fileName);
            try
            {
                Assert.IsTrue(db.Exists(connectionString));
            }
            finally
            {
                File.Delete(fileName);
            }
        }
        [TestMethod]
        public void DatabaseInMemory()
        {
            var db = Create(GoodConnectionInMemory, true, "");
            Assert.IsTrue(db.Exists(GoodConnectionInMemory));
        }

        private DatabaseExists Create(string connectionString, bool inMemory, string fileName)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<IGetFileNameFromConnectionString>();
            test.GetFileName(connectionString).Returns(new ConnectionStringInfo(inMemory, fileName));
            return new DatabaseExists(test);
        }
    }
}
