using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    public class PostgreSqlCommandStringCacheTests
    {
        [Fact]
        public void Create_Key_Missing()
        {
            var test = Create();
            Assert.False(test.Contains("test"));
        }

        [Fact]
        public void GetSet_Key()
        {
            var test = Create();
            test.Add("test", "command");
            Assert.True(test.Contains("test"));
            Assert.Equal("command", test.Get("test").CommandText);
        }

        [Fact]
        public async Task Threaded_Query()
        {
            var test = Create();

            var task1 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task2 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task3 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task4 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task5 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));

            task1.Start();
            task2.Start();
            task3.Start();
            task4.Start();
            task5.Start();

            await Task.WhenAll(task1, task2, task3, task4, task5);
        }

        private PostgreSqlCommandStringCache Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<PostgreSqlCommandStringCache>();
        }
    }
}
