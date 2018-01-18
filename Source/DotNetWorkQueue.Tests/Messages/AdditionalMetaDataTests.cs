using System;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class AdditionalMetaDataTests
    {
        [Fact]
        public void Create_MetaData_Null_Name_Fails()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    var test = new AdditionalMetaData<Data>(null, new Data());
                    Assert.Null(test);
                });
        }

        [Fact]
        public void Get_Value_AsObject()
        {
            var input = new Data();
            IAdditionalMetaData data = new AdditionalMetaData<Data>("test", input);
            Assert.Equal(data.Value, input);
        }

        [Fact]
        public void Get_Value()
        {
            var input = new Data();
            var data = new AdditionalMetaData<Data>("test", input);
            Assert.Equal(data.Value, input);
        }

        [Fact]
        public void Get_Name()
        {
            var input = new Data();
            IAdditionalMetaData data = new AdditionalMetaData<Data>("test", input);
            Assert.Equal("test", data.Name);
        }

        private class Data
        {
            
        }
    }
}
