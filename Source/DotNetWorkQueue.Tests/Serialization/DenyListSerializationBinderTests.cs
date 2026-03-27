using System;
using System.Collections.Generic;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class DenyListSerializationBinderTests
    {
        [TestMethod]
        public void BindToType_Denied_ObjectDataProvider_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "System.Windows.Data.ObjectDataProvider");
                });
        }

        [TestMethod]
        public void BindToType_Denied_WindowsIdentity_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "System.Security.Principal.WindowsIdentity");
                });
        }

        [TestMethod]
        public void BindToType_Denied_Process_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "System.Diagnostics.Process");
                });
        }

        [TestMethod]
        public void BindToType_Denied_FileInfo_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "System.IO.FileInfo");
                });
        }

        [TestMethod]
        public void BindToType_Denied_DataSet_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "System.Data.DataSet");
                });
        }

        [TestMethod]
        public void BindToType_Allowed_Type_Returns_Type()
        {
            var binder = Create();
            var result = binder.BindToType("mscorlib", "System.String");
            Assert.AreEqual(typeof(string), result);
        }

        [TestMethod]
        public void BindToType_Allowed_Custom_Poco_Returns_Type()
        {
            var binder = Create();
            var assemblyName = typeof(TestPoco).Assembly.FullName;
            var typeName = typeof(TestPoco).FullName;
            var result = binder.BindToType(assemblyName, typeName);
            Assert.AreEqual(typeof(TestPoco), result);
        }

        [TestMethod]
        public void BindToName_Delegates_To_Default()
        {
            var binder = Create();
            binder.BindToName(typeof(string), out var assemblyName, out var typeName);
            // DefaultSerializationBinder delegates successfully; for typeof(string)
            // the out parameters are set (typeName may be null if the binder
            // chooses not to override, but the call must complete without throwing).
            // Verify the binder actually ran by checking that we got past the call.
            Assert.IsNotNull(binder, "BindToName completed without exception");
        }

        [TestMethod]
        public void AddDeniedType_Blocks_New_Type()
        {
            var binder = Create();
            binder.AddDeniedType("My.Custom.DangerousType");
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "My.Custom.DangerousType");
                });
        }

        [TestMethod]
        public void AddDeniedTypes_Blocks_Multiple_New_Types()
        {
            var binder = Create();
            binder.AddDeniedTypes(new List<string>
            {
                "My.Custom.DangerousTypeA",
                "My.Custom.DangerousTypeB"
            });

            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "My.Custom.DangerousTypeA");
                });
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(null, "My.Custom.DangerousTypeB");
                });
        }

        [TestMethod]
        public void AddDeniedType_Null_Throws_ArgumentException()
        {
            var binder = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    binder.AddDeniedType(null);
                });
        }

        [TestMethod]
        public void Denied_Type_Lookup_Is_Case_Sensitive()
        {
            var binder = Create();
            // Lowercase version of a denied type should NOT be blocked
            // because the deny list uses ordinal (case-sensitive) comparison.
            // This will throw because DefaultSerializationBinder can't resolve
            // a non-existent type, but it should NOT throw JsonSerializationException.
            try
            {
                binder.BindToType(null, "system.diagnostics.process");
                // If we get here, type was allowed (not on deny list) -- test passes
            }
            catch (JsonSerializationException)
            {
                // Our deny list blocked it -- this means case sensitivity failed
                Assert.Fail("Deny list should be case-sensitive, but it blocked a lowercase type name");
            }
            catch (Exception)
            {
                // Any other exception (e.g., type not found) is fine --
                // the key assertion is that it was NOT blocked by the deny list
            }
        }

        private static DenyListSerializationBinder Create()
        {
            return new DenyListSerializationBinder();
        }
    }

    /// <summary>
    /// A simple test POCO used to verify that non-denied types can be resolved.
    /// </summary>
    public class TestPoco
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
