using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardErrorMessagesQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Errors_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(1);
            var now = DateTimeOffset.UtcNow;

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorMessages, 0, reader).Returns(1L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorMessages, 1, reader).Returns(100L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorMessages, 2, reader).Returns("NullReferenceException");
            readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardErrorMessages, 3, reader).Returns(now);

            var result = handler.Handle(new GetDashboardErrorMessagesQuery(0, 25));

            Assert.ContainsSingle(result);
            Assert.AreEqual(1L, result[0].Id);
            Assert.AreEqual("100", result[0].QueueId);
            Assert.AreEqual("NullReferenceException", result[0].LastException);
        }

        [TestMethod]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardErrorMessagesQuery(0, 25));

            Assert.IsEmpty(result);
        }

        private static (IQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            if (rowCount > 0)
                reader.Read().Returns(true, false);
            else
                reader.Read().Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardErrorMessagesQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
