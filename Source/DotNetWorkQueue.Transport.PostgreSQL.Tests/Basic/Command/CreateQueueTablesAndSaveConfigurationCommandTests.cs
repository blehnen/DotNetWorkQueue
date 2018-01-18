using System.Collections.Generic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.Command
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
