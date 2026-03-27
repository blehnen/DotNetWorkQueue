using System;
using System.Collections.Generic;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class AllowListSerializationBinderTests
    {
        [TestMethod]
        public void BindToType_Unregistered_Type_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(string).Assembly.FullName, "System.String");
                });
        }

        [TestMethod]
        public void BindToType_Registered_Type_Returns_Type()
        {
            var binder = Create();
            binder.AddAllowedType("System.String");
            var result = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), result);
        }

        [TestMethod]
        public void AddAllowedType_Type_Overload_Enables_Deserialization()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(string));
            var result = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), result);
        }

        [TestMethod]
        public void AddAllowedTypes_Enables_Multiple_Types()
        {
            var binder = Create();
            binder.AddAllowedTypes(new List<string>
            {
                "System.String",
                "System.Int32"
            });

            var stringResult = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), stringResult);

            var intResult = binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
            Assert.AreEqual(typeof(int), intResult);
        }

        [TestMethod]
        public void BindToType_Registered_Type_Still_Blocks_Others()
        {
            var binder = Create();
            binder.AddAllowedType("System.String");
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
                });
        }

        [TestMethod]
        public void BindToName_Delegates_To_Default()
        {
            var binder = Create();
            binder.BindToName(typeof(AllowListSerializationBinderTests), out var assemblyName, out var typeName);
            Assert.IsNotNull(assemblyName, "BindToName should delegate to DefaultSerializationBinder and populate assemblyName");
        }

        [TestMethod]
        public void AddAllowedType_Null_String_Throws_ArgumentException()
        {
            var binder = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    binder.AddAllowedType((string)null);
                });
        }

        [TestMethod]
        public void AddAllowedType_Null_Type_Throws_ArgumentNullException()
        {
            var binder = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    binder.AddAllowedType((Type)null);
                });
        }

        [TestMethod]
        public void Empty_Allow_List_Blocks_Everything()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(object).Assembly.FullName, "System.Object");
                });
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(string).Assembly.FullName, "System.String");
                });
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
                });
        }

        private static AllowListSerializationBinder Create()
        {
            return new AllowListSerializationBinder();
        }
    }
}
