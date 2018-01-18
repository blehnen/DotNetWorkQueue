using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;

using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageContextDataTests
    {
        [Fact]
        public void Create_Null_Name_Fails()
        {
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                var test = new MessageContextData<Data>(null, null);
                Assert.Null(test);
            });
        }

        [Theory, AutoData]
        public void Create_Null_Data_Ok(string name)
        {
            var test = new MessageContextData<Data>(name, null);
            Assert.NotNull(test);
        }

        [Theory, AutoData]
        public void Create_Default(string name, Data d)
        {
            var test = new MessageContextData<Data>(name, d);
            Assert.Equal(name, test.Name);
            Assert.Equal(d, test.Default);
        }

        public class Data
        {

        }
    }
}
