using System;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class AdditionalMetaDataTests
    {
        [TestMethod]
        public void Create_MetaData_Null_Name_Fails()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    var test = new AdditionalMetaData<Data>(null, new Data());
                    Assert.IsNull(test);
                });
        }

        [TestMethod]
        public void Get_Value_AsObject()
        {
            var input = new Data();
            IAdditionalMetaData data = new AdditionalMetaData<Data>("test", input);
            Assert.AreEqual(data.Value, input);
        }

        [TestMethod]
        public void Get_Value()
        {
            var input = new Data();
            var data = new AdditionalMetaData<Data>("test", input);
            Assert.AreEqual(data.Value, input);
        }

        [TestMethod]
        public void Get_Name()
        {
            var input = new Data();
            IAdditionalMetaData data = new AdditionalMetaData<Data>("test", input);
            Assert.AreEqual("test", data.Name);
        }

        private class Data
        {

        }
    }
}
