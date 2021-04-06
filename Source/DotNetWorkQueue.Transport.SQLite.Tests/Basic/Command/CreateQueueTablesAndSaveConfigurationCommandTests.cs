using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic.Command
{
    public class CreateQueueTablesAndSaveConfigurationCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            var tables = new List<Table>();
            var test = new CreateQueueTablesAndSaveConfigurationCommand<Table>(tables);
            Assert.Equal(tables, test.Tables);
        }
    }
}
