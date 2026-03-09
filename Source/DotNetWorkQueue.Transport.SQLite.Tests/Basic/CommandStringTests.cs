using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class CommandStringTests
    {
        [TestMethod]
        public void Create_Command()
        {
            var commands = new List<string>();
            var test = new CommandString("testing", commands);
            Assert.AreEqual("testing", test.CommandText);
            Assert.AreEqual(commands, test.AdditionalCommands);
        }
    }
}
