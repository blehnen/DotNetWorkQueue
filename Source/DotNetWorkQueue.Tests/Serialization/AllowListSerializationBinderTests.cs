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

        [TestMethod]
        [DataRow("System.Private.CoreLib")]
        [DataRow("mscorlib")]
        public void A_Generic_Type_Allowed_By_Type_Matches_The_Name_Json_Net_Writes(string coreLibrary)
        {
            //Type.FullName qualifies the argument with a version; the payload names it simply, and a
            //message written on .NET Framework names mscorlib
            var binder = Create();
            binder.AddAllowedType(typeof(List<string>));

            var result = binder.BindToType(typeof(List<string>).Assembly.GetName().Name,
                $"System.Collections.Generic.List`1[[System.String, {coreLibrary}]]");

            Assert.AreEqual(typeof(List<string>), result);
        }

        [TestMethod]
        public void A_Generic_Type_Allowed_By_Type_Matches_Its_Full_Name()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(Dictionary<string, List<int>>));

            var result = binder.BindToType(typeof(Dictionary<string, List<int>>).Assembly.FullName,
                typeof(Dictionary<string, List<int>>).FullName);

            Assert.AreEqual(typeof(Dictionary<string, List<int>>), result);
        }

        [TestMethod]
        public void A_Generic_Type_Allowed_By_Type_Does_Not_Match_Other_Arguments()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(List<string>));
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(List<int>).Assembly.GetName().Name,
                        "System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]]");
                });
        }

        [TestMethod]
        public void A_Polymorphic_Generic_Member_Round_Trips_Once_Allowed()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(List<string>));
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = binder };
            var json = JsonConvert.SerializeObject(new Holder { Value = new List<string> { "a" } }, settings);

            var back = JsonConvert.DeserializeObject<Holder>(json, settings);

            Assert.AreSequenceEqual(new[] { "a" }, (List<string>)back.Value);
        }

        [TestMethod]
        [DataRow("System.String", "System.String")]
        [DataRow("A.B`1[[C.D, E, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]", "A.B`1[[C.D]]")]
        [DataRow("A.B`2[[C.D, E],[F.G`1[[H.I, J]], K]]", "A.B`2[[C.D],[F.G`1[[H.I]]]]")]
        [DataRow("A.B`1[[C.D[,], E]][]", "A.B`1[[C.D[,]]][]")]
        public void WithoutAssemblies_Removes_Only_The_Generic_Argument_Assemblies(string name, string expected)
        {
            Assert.AreEqual(expected, AllowListSerializationBinder.WithoutAssemblies(name));
        }

        private class Holder
        {
            public object Value { get; set; }
        }

        private static AllowListSerializationBinder Create()
        {
            return new AllowListSerializationBinder();
        }
    }
}
