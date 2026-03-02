using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.CommandHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.CommandHandler
{
    public class DashboardDeleteAllErrorMessagesCommandHandlerTests
    {
        [Fact]
        public void Create_Default()
        {
            Assert.NotNull(new DashboardDeleteAllErrorMessagesCommandHandler(Substitute.For<IDataStorage>()));
        }

        [Fact]
        public void Create_NullDataStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DashboardDeleteAllErrorMessagesCommandHandler(null));
        }
    }
}
