using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Basic
{
    public class CommandStringTests
    {
        [Fact]
        public void Create_Command()
        {
            var commands = new List<string>();
            var test = new CommandString("testing", commands);
            Assert.Equal("testing", test.CommandText);
            Assert.Equal(commands, test.AdditionalCommands);
        }
    }
}
