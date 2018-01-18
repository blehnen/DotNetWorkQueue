using System.IO;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Basic
{
    public class DatabaseExistsTests
    {
        private const string BadConnection =
           "Thisisabadconnectionstring";

        private const string GoodConnectionInMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

        [Fact]
        public void DatabaseDoesNotExist()
        {
            var db = Create(BadConnection, false, "badfile.db3");
            Assert.False(db.Exists(BadConnection));
        }

        [Fact]
        public void DatabaseDoesExist()
        {
            var fileName = Path.GetTempFileName();
            var connectionString = $"Data Source={fileName};Version=3;";
            var db = Create(connectionString, false, fileName);
            try
            {
                Assert.True(db.Exists(connectionString));
            }
            finally
            {
                File.Delete(fileName);
            }
        }
        [Fact]
        public void DatabaseInMemory()
        {
            var db = Create(GoodConnectionInMemory, true, "");
            Assert.True(db.Exists(GoodConnectionInMemory));
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
